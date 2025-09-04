// Search recursively for *.*proj files, and get a unique JSON array of used TFMs
#:sdk Microsoft.NET.Sdk
#:package NuGetizer@1.3.*
#:package Spectre.Console@0.50.*
#:property PublishAot=false
#:property ImplicitUsings=true

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Devlooped;
using Spectre.Console;

try
{
    var outputPath = args.Length > 0 ? args[0] : null;
    var repoRoot = Directory.GetCurrentDirectory();
    var projectFiles = Directory.EnumerateFiles(repoRoot, "*.*proj", SearchOption.AllDirectories).ToList();
    var versions = new HashSet<int>();

    foreach (var proj in projectFiles)
    {
        var frameworks = GetFrameworks(proj);
        foreach (var fw in frameworks)
        {
            var trimmed = fw.Trim();
            if (trimmed.Length == 0)
                continue;

            if (RegexParsers.NetRegex().Match(trimmed) is { Success: true } netmatch &&
                int.TryParse(netmatch.Groups[1].Value, out var netv))
            {
                versions.Add(netv);
                continue;
            }

            if (RegexParsers.NetCoreAppRegex().Match(trimmed) is { Success: true } corematch &&
                int.TryParse(corematch.Groups[1].Value, out var corev))
                versions.Add(corev);
        }
    }

    if (versions.Count == 0)
    {
        AnsiConsole.WriteLine("No .NET versions found in the repository.");
        return 0;
    }

    var sorted = versions.Order().Select(v => $"{v}.x").ToList();
    AnsiConsole.MarkupLine($"Found .NET versions: {string.Join(',', sorted)}");

    if (!string.IsNullOrWhiteSpace(outputPath))
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(outputPath!, JsonSerializer.Serialize(sorted, new JsonSerializerOptions()
        {
            WriteIndented = true
        }), Encoding.UTF8);

        AnsiConsole.MarkupLine($"[grey]Versions written to {outputPath}[/]");
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static IEnumerable<string> GetFrameworks(string projectFile)
{
    var single = RunBuild(projectFile, "TargetFramework").Trim();
    if (!string.IsNullOrWhiteSpace(single))
        return single.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var multi = RunBuild(projectFile, "TargetFrameworks").Trim();
    if (!string.IsNullOrWhiteSpace(multi))
        return multi.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return [];
}

static string RunBuild(string projectFile, string property)
{
    var psi = new ProcessStartInfo
    {
        Arguments = $"msbuild \"{projectFile}\" -getProperty:{property}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var proc = Dotnet.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    if (proc.ExitCode != 0)
    {
        Debug.WriteLine($"dotnet msbuild failed for {projectFile} ({property}): {stderr}");
        return string.Empty;
    }
    return stdout;
}

static partial class RegexParsers
{
    [GeneratedRegex("^net(\\d+)\\.\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex NetRegex();

    [GeneratedRegex("^netcoreapp(\\d+)\\.\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    public static partial Regex NetCoreAppRegex();
}