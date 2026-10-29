using System.Security.Cryptography;
using System.Text;

namespace SetupScript;

/// <summary>
/// A hash of everything the setup scripts are built from: the assemblies' sources, their
/// projects and the templates. The scripts record it, so a test can tell when they are older
/// than the sources. Line endings don't count, so a checkout on Windows hashes as one on Linux.
/// </summary>
public static class SourceHash
{
    private static readonly string[] Projects = ["SqlClrHtml", "SqlClrSendMail"];

    public static string Compute(string repositoryRoot)
    {
        var files = Projects
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(repositoryRoot, project), "*.*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Concat(Directory.EnumerateFiles(Path.Combine(repositoryRoot, "Setup"), "*.template.sql"))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/'))
            .Where(file => !file.Contains("/bin/") && !file.Contains("/obj/"))
            .Order(StringComparer.Ordinal);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            var text = File.ReadAllText(Path.Combine(repositoryRoot, file)).Replace("\r\n", "\n");
            hash.AppendData(Encoding.UTF8.GetBytes($"{file}\n{text}\n"));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
