using System.Reflection;
using System.Security.Cryptography;

using SetupScript;

// Each setup script carries its assembly as a binary literal, with the hash SQL Server trusts it by
var metadata = typeof(SourceHash).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
    .ToDictionary(a => a.Key, a => a.Value!);
var root = metadata["RepositoryRoot"];
var configuration = metadata["Configuration"];
var sources = SourceHash.Compute(root);

Write("SetupHtml", "SqlClrHtml", "SimpleTalk.SQLCLR.Html");
Write("SetupSendMail", "SqlClrSendMail", "SimpleTalk.SQLCLR.SendMail");

void Write(string script, string project, string assemblyName)
{
    var dll = Path.Combine(root, project, "bin", configuration, "net48", assemblyName + ".dll");
    var bits = File.ReadAllBytes(dll);
    // The name SQL Server shows in sys.assemblies.clr_name, which trusted assemblies are described by
    var description = AssemblyName.GetAssemblyName(dll).FullName.ToLowerInvariant() + ", processorarchitecture=msil";

    var setup = Path.Combine(root, "Setup");
    var text = File.ReadAllText(Path.Combine(setup, script + ".template.sql"))
        .Replace("$(SourceHash)", sources)
        .Replace("$(AssemblyHash)", "0x" + Convert.ToHexString(SHA512.HashData(bits)))
        .Replace("$(AssemblyDescription)", description)
        .Replace("$(AssemblyBits)", "0x" + Convert.ToHexString(bits));

    File.WriteAllText(Path.Combine(setup, script + ".sql"), text);
    Console.WriteLine($"Wrote {script}.sql with {description}");
}
