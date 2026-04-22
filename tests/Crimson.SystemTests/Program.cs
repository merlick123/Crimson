using System.Text.Json;
using System.Text.Json.Nodes;
using Crimson.Core;

var failures = new List<string>();

Run("Init build pipeline creates project output", BuildPipelineCreatesOutput);
Run("Build heals missing user files", BuildHealsMissingUserFiles);
Run("Init writes reusable MSBuild integration", InitWritesMsBuildIntegration);
Run("MSBuild integration respects configured output root", MsBuildIntegrationRespectsConfiguredOutputRoot);
Run("Example app runs from repo root without crimson on PATH", ExampleAppRunsFromRepoRootWithoutCrimsonOnPath);
Run("Init creates gitignore entries", InitCreatesGitIgnoreEntries);
Run("Init target name resolves to project file path", InitTargetNameResolvesToProjectFilePath);
return failures.Count == 0 ? 0 : 1;

void Run(string name, Action body)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add(name);
        Console.Error.WriteLine($"FAIL {name}");
        Console.Error.WriteLine(exception);
    }
}

void BuildPipelineCreatesOutput()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    Directory.CreateDirectory(Path.Combine(root, "contracts"));
    File.WriteAllText(Path.Combine(root, "contracts", "customer.idl"), """
namespace Billing.Contracts {
    interface CustomerService {
        string name;
        string get_customer(string customer_id);
    }
}
""");

    var result = workspace.Build(projectFile);
    if (result.Conflicts.Count != 0)
    {
        throw new InvalidOperationException("Build reported merge conflicts unexpectedly.");
    }

    var generated = Path.Combine(root, "src", "Generated", "Billing", "Contracts", "CustomerService.g.cs");
    var user = Path.Combine(root, "src", "User", "Billing", "Contracts", "CustomerService.cs");

    if (!File.Exists(generated))
    {
        throw new InvalidOperationException("Generated class file was not materialized.");
    }

    if (!File.Exists(user))
    {
        throw new InvalidOperationException("User class file was not materialized.");
    }

    var projectJson = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(projectFile));
    if (!projectJson.TryGetProperty("sources", out _))
    {
        throw new InvalidOperationException("Project file was not initialized correctly.");
    }

    var output = projectJson.GetProperty("targets").GetProperty("csharp").GetProperty("output").GetString();
    if (!string.Equals(output, "src", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Project file did not include the default csharp output root.");
    }
}

void BuildHealsMissingUserFiles()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    Directory.CreateDirectory(Path.Combine(root, "contracts"));
    File.WriteAllText(Path.Combine(root, "contracts", "customer.idl"), """
namespace Billing.Contracts {
    interface CustomerService {
        string name;
        string get_customer(string customer_id);
    }
}
""");

    var first = workspace.Build(projectFile);
    if (first.Conflicts.Count != 0)
    {
        throw new InvalidOperationException("Initial build reported merge conflicts unexpectedly.");
    }

    var userFile = Path.Combine(root, "src", "User", "Billing", "Contracts", "CustomerService.cs");
    if (!File.Exists(userFile))
    {
        throw new InvalidOperationException("User file was not created on initial build.");
    }

    File.Delete(userFile);

    var second = workspace.Build(projectFile);
    if (second.Conflicts.Count != 0)
    {
        throw new InvalidOperationException("Healing build reported merge conflicts unexpectedly.");
    }

    if (!File.Exists(userFile))
    {
        throw new InvalidOperationException("Missing user file was not restored on rebuild.");
    }
}

void InitWritesMsBuildIntegration()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    var props = Path.Combine(root, ".crimson", "msbuild", "Crimson.CSharp.props");
    var targets = Path.Combine(root, ".crimson", "msbuild", "Crimson.CSharp.targets");

    if (!File.Exists(props))
    {
        throw new InvalidOperationException("MSBuild props file was not created.");
    }

    if (!File.Exists(targets))
    {
        throw new InvalidOperationException("MSBuild targets file was not created.");
    }
}

void InitCreatesGitIgnoreEntries()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    var gitIgnore = Path.Combine(root, ".gitignore");
    if (!File.Exists(gitIgnore))
    {
        throw new InvalidOperationException(".gitignore was not created.");
    }

    var contents = File.ReadAllText(gitIgnore);
    AssertContains(".merge/previous/", contents);
    AssertContains(".merge/current/", contents);
    AssertContains(".merge/backup/", contents);
}

void MsBuildIntegrationRespectsConfiguredOutputRoot()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    var projectJson = JsonNode.Parse(File.ReadAllText(projectFile))?.AsObject()
        ?? throw new InvalidOperationException("Project file JSON was not parsed.");
    projectJson["targets"]!["csharp"]!["output"] = "codegen";
    File.WriteAllText(projectFile, projectJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    Directory.CreateDirectory(Path.Combine(root, "contracts"));
    File.WriteAllText(Path.Combine(root, "contracts", "customer.idl"), """
namespace Billing.Contracts {
    interface CustomerService {
        string name;
    }
}
""");

    workspace.Build(projectFile);

    var cliPath = ResolveCliPath();
    var appDirectory = Path.Combine(root, "app");
    Directory.CreateDirectory(appDirectory);
    File.WriteAllText(Path.Combine(appDirectory, "App.csproj"), $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../.crimson/msbuild/Crimson.CSharp.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CrimsonProjectFile>../Billing.crimsonproj</CrimsonProjectFile>
    <CrimsonCommand>{{cliPath}}</CrimsonCommand>
  </PropertyGroup>

  <Import Project="../.crimson/msbuild/Crimson.CSharp.targets" />
</Project>
""");

    File.WriteAllText(Path.Combine(appDirectory, "Program.cs"), """
using Billing.Contracts;

var service = new CustomerService();
Console.WriteLine(service.Name);
""");

    var result = new ExecResult("dotnet", ["build", Path.Combine(appDirectory, "App.csproj"), "-v", "minimal"], appDirectory).Run();
    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected app build to succeed.{Environment.NewLine}{result.Output}");
    }
}

void ExampleAppRunsFromRepoRootWithoutCrimsonOnPath()
{
    var repoRoot = ResolveRepoRoot();
    var result = new ExecResult(
        "dotnet",
        ["run", "--project", Path.Combine(repoRoot, "examples", "SmartHomeDemo", "app", "SmartHomeDemo.App.csproj")],
        repoRoot).Run();

    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected example app to run successfully.{Environment.NewLine}{result.Output}");
    }

    AssertContains("Home: Willow Lane", result.Output);
}

void InitTargetNameResolvesToProjectFilePath()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var resolved = Crimson.Core.Projects.CrimsonProjectFile.ResolveInitProjectFilePath("BillingDemo", root);
    var expected = Path.Combine(root, "BillingDemo", "BillingDemo.crimsonproj");

    if (!string.Equals(resolved, expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected '{expected}' but found '{resolved}'.");
    }
}

void AssertContains(string expectedSubstring, string actual)
{
    if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected to find '{expectedSubstring}' in output.");
    }
}

static string ResolveCliPath()
{
    var candidate = Path.GetFullPath(Path.Combine(ResolveRepoRoot(), "src/Crimson.Cli/bin/Debug/net10.0/crimson"));
    if (!File.Exists(candidate))
    {
        throw new InvalidOperationException($"Expected CLI executable at '{candidate}'.");
    }

    return candidate;
}

static string ResolveRepoRoot() =>
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

sealed class ExecResult(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
{
    public int ExitCode { get; private set; }
    public string Output { get; private set; } = string.Empty;

    public ExecResult Run()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        ExitCode = process.ExitCode;
        Output = string.Concat(stdout, stderr);
        return this;
    }
}
