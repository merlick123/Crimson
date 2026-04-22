using System.Text.Json;
using System.Text.Json.Nodes;
using Crimson.Core;

var failures = new List<string>();

Run("Init build pipeline creates project output", BuildPipelineCreatesOutput);
Run("Build heals missing user files", BuildHealsMissingUserFiles);
Run("Init writes reusable MSBuild integration", InitWritesMsBuildIntegration);
Run("CSharp starter app scaffold builds and runs", CSharpStarterAppScaffoldBuildsAndRuns);
Run("MSBuild integration respects configured output root", MsBuildIntegrationRespectsConfiguredOutputRoot);
Run("Build without existing merge baseline preserves user files", BuildWithoutExistingMergeBaselinePreservesUserFiles);
Run("Example app runs from repo root without crimson on PATH", ExampleAppRunsFromRepoRootWithoutCrimsonOnPath);
Run("Init creates gitignore entries", InitCreatesGitIgnoreEntries);
Run("Cpp init writes reusable CMake integration", CppInitWritesCMakeIntegration);
Run("Cpp CMake GCC starter app builds and runs", CppCMakeGccStarterAppBuildsAndRuns);
Run("Init target name resolves to project file path", InitTargetNameResolvesToProjectFilePath);
Run("CLI init requires explicit profile", CliInitRequiresExplicitProfile);
Run("CLI init profiles list built in profiles", CliInitProfilesListBuiltInProfiles);
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
    workspace.InitProject(projectFile, "csharp", starter: false);

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
    workspace.InitProject(projectFile, "csharp", starter: false);

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
    workspace.InitProject(projectFile, "csharp", starter: false);

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

void CSharpStarterAppScaffoldBuildsAndRuns()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: true);

    var repoRoot = ResolveRepoRoot();
    var cliProjectPath = Path.Combine(repoRoot, "src", "Crimson.Cli", "Crimson.Cli.csproj");
    var appProjectPath = Path.Combine(root, "app", "Billing.App.csproj");
    var appProjectContents = File.ReadAllText(appProjectPath);
    appProjectContents = appProjectContents.Replace(
        "</PropertyGroup>",
        $$"""
    <CrimsonCommand>dotnet</CrimsonCommand>
    <CrimsonCommandArguments>run --project &quot;{{cliProjectPath}}&quot; --</CrimsonCommandArguments>
  </PropertyGroup>
""",
        StringComparison.Ordinal);
    File.WriteAllText(appProjectPath, appProjectContents);

    var result = new ExecResult("dotnet", ["run", "--project", appProjectPath, "-v", "minimal"], root).Run();
    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected scaffolded app to run successfully.{Environment.NewLine}{result.Output}");
    }

    AssertContains("Crimson", result.Output);
}

void InitCreatesGitIgnoreEntries()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: false);

    var gitIgnore = Path.Combine(root, ".gitignore");
    if (!File.Exists(gitIgnore))
    {
        throw new InvalidOperationException(".gitignore was not created.");
    }

    var contents = File.ReadAllText(gitIgnore);
    AssertContains(".merge/previous/", contents);
    AssertContains(".merge/current/", contents);
    AssertContains(".merge/backup/", contents);
    AssertContains("app/bin/", contents);
    AssertContains("app/obj/", contents);
}

void CppInitWritesCMakeIntegration()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "cpp-cmake", starter: false);

    var cmakeModule = Path.Combine(root, ".crimson", "cmake", "Crimson.Cpp.cmake");
    if (!File.Exists(cmakeModule))
    {
        throw new InvalidOperationException("CMake integration file was not created.");
    }

    if (!File.Exists(Path.Combine(root, "CMakeLists.txt")))
    {
        throw new InvalidOperationException("CMakeLists.txt was not created.");
    }

    var gitIgnore = File.ReadAllText(Path.Combine(root, ".gitignore"));
    AssertContains("build/", gitIgnore);
}

void CppCMakeGccStarterAppBuildsAndRuns()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "cpp-cmake-gcc", starter: true);

    var repoRoot = ResolveRepoRoot();
    var cliProjectPath = Path.Combine(repoRoot, "src", "Crimson.Cli", "Crimson.Cli.csproj");
    var crimsonCommandArguments = $"run --project {cliProjectPath} --";

    var configure = new ExecResult(
        "cmake",
        [
            "--preset", "gcc-debug",
            "-DCrimsonCommand=dotnet",
            $"-DCrimsonCommandArguments={crimsonCommandArguments}",
        ],
        root).Run();

    if (configure.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected cmake configure to succeed.{Environment.NewLine}{configure.Output}");
    }

    var build = new ExecResult("cmake", ["--build", "--preset", "gcc-debug"], root).Run();
    if (build.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected cmake build to succeed.{Environment.NewLine}{build.Output}");
    }

    var binaryPath = Path.Combine(root, "build", "gcc-debug", "BillingApp");
    var run = new ExecResult(binaryPath, Array.Empty<string>(), root).Run();
    if (run.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected built cpp app to run successfully.{Environment.NewLine}{run.Output}");
    }

    AssertContains("Crimson", run.Output);
}

void MsBuildIntegrationRespectsConfiguredOutputRoot()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: false);

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

    var repoRoot = ResolveRepoRoot();
    var cliProjectPath = Path.Combine(repoRoot, "src", "Crimson.Cli", "Crimson.Cli.csproj");
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
    <CrimsonCommand>dotnet</CrimsonCommand>
    <CrimsonCommandArguments>run --project &quot;{{cliProjectPath}}&quot; --</CrimsonCommandArguments>
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

void BuildWithoutExistingMergeBaselinePreservesUserFiles()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: false);

    Directory.CreateDirectory(Path.Combine(root, "contracts"));
    File.WriteAllText(Path.Combine(root, "contracts", "customer.idl"), """
namespace Billing.Contracts {
    interface CustomerService {
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
    var customized = """
using System;
using System.Collections.Generic;

namespace Billing.Contracts;

public partial class CustomerService
{
    public CustomerService()
    {
    }

    public override string GetCustomer(string customerId)
    {
        return "custom";
    }
}
""";
    File.WriteAllText(userFile, customized);
    Directory.Delete(Path.Combine(root, ".merge"), recursive: true);

    var second = workspace.Build(projectFile);
    if (second.Conflicts.Count != 0)
    {
        throw new InvalidOperationException("Build without baseline reported merge conflicts unexpectedly.");
    }

    AssertContains("return \"custom\";", File.ReadAllText(userFile));
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

void CliInitRequiresExplicitProfile()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var result = new ExecResult(
        "dotnet",
        ["run", "--project", Path.Combine(ResolveRepoRoot(), "src", "Crimson.Cli", "Crimson.Cli.csproj"), "--", "init", Path.Combine(root, "Demo.crimsonproj")],
        ResolveRepoRoot()).Run();

    if (result.ExitCode == 0)
    {
        throw new InvalidOperationException("Expected init without --profile to fail.");
    }

    AssertContains("Expected --profile <id>", result.Output);
}

void CliInitProfilesListBuiltInProfiles()
{
    var result = new ExecResult(
        "dotnet",
        ["run", "--project", Path.Combine(ResolveRepoRoot(), "src", "Crimson.Cli", "Crimson.Cli.csproj"), "--", "init-profiles"],
        ResolveRepoRoot()).Run();

    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected init-profiles to succeed.{Environment.NewLine}{result.Output}");
    }

    AssertContains("csharp", result.Output);
    AssertContains("C# / .NET", result.Output);
    AssertContains("cpp-cmake", result.Output);
    AssertContains("cpp-cmake-gcc", result.Output);
}

void AssertContains(string expectedSubstring, string actual)
{
    if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected to find '{expectedSubstring}' in output.");
    }
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
