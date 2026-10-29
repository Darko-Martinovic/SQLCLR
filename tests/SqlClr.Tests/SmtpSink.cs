using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

using MimeKit;

namespace SqlClr.Tests;

/// <summary>What one SMTP session delivered.</summary>
public sealed record Delivery(string? UserName, string? Password, string MailFrom, IReadOnlyList<string> Recipients, MimeMessage Message);

/// <summary>
/// A stand-in SMTP server on 127.0.0.1 that accepts every message, signs in with AUTH LOGIN
/// and keeps what it got. It offers no STARTTLS.
/// </summary>
public sealed class SmtpSink : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _accepting;

    public SmtpSink()
    {
        _listener.Start();
        _accepting = AcceptAsync();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public ConcurrentQueue<Delivery> Deliveries { get; } = new();

    public Delivery Single() => Assert.Single(Deliveries);

    public void Clear() => Deliveries.Clear();

    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using var _ = client;
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.ASCII);
        var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

        string? user = null, password = null, from = null;
        var recipients = new List<string>();
        await writer.WriteLineAsync("220 sink ready");

        while (await reader.ReadLineAsync() is { } line)
        {
            var command = line.ToUpperInvariant();
            if (command.StartsWith("EHLO"))
            {
                await writer.WriteLineAsync("250-sink");
                await writer.WriteLineAsync("250-AUTH LOGIN");
                await writer.WriteLineAsync("250 OK");
            }
            else if (command.StartsWith("HELO") || command.StartsWith("NOOP") || command.StartsWith("RSET"))
            {
                await writer.WriteLineAsync("250 OK");
            }
            else if (command.StartsWith("AUTH LOGIN"))
            {
                var initial = line.Length > "AUTH LOGIN".Length ? line["AUTH LOGIN".Length..].Trim() : "";
                if (initial.Length == 0)
                {
                    await writer.WriteLineAsync("334 VXNlcm5hbWU6");
                    initial = await reader.ReadLineAsync() ?? "";
                }
                user = Decode(initial);
                await writer.WriteLineAsync("334 UGFzc3dvcmQ6");
                password = Decode(await reader.ReadLineAsync() ?? "");
                await writer.WriteLineAsync("235 2.7.0 Authentication successful");
            }
            else if (command.StartsWith("MAIL FROM:"))
            {
                from = Address(line);
                recipients.Clear();
                await writer.WriteLineAsync("250 OK");
            }
            else if (command.StartsWith("RCPT TO:"))
            {
                recipients.Add(Address(line));
                await writer.WriteLineAsync("250 OK");
            }
            else if (command == "DATA")
            {
                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                var data = new StringBuilder();
                while (await reader.ReadLineAsync() is { } dataLine && dataLine != ".")
                    data.Append(dataLine.StartsWith("..") ? dataLine[1..] : dataLine).Append("\r\n");

                var message = await MimeMessage.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes(data.ToString())));
                Deliveries.Enqueue(new Delivery(user, password, from ?? "", recipients.ToList(), message));
                await writer.WriteLineAsync("250 OK queued");
            }
            else if (command == "QUIT")
            {
                await writer.WriteLineAsync("221 Bye");
                return;
            }
            else
            {
                await writer.WriteLineAsync("502 Command not implemented");
            }
        }
    }

    private static string Decode(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim()));

    private static string Address(string line)
    {
        var start = line.IndexOf('<');
        var end = line.IndexOf('>');
        return start >= 0 && end > start ? line[(start + 1)..end] : line[(line.IndexOf(':') + 1)..].Trim();
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        try
        {
            await _accepting;
        }
        catch (ObjectDisposedException)
        {
        }
        _stop.Dispose();
    }
}
