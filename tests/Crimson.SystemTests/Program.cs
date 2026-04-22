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
Run("Cpp CMake cross init writes generic toolchain scaffold", CppCMakeCrossInitWritesToolchainScaffold);
Run("Shared SmartHome C++ example builds and runs", SharedSmartHomeCppExampleBuildsAndRuns);
Run("Rust Cargo init writes reusable build integration", RustCargoInitWritesBuildIntegration);
Run("Rust Cargo starter app builds and runs", RustCargoStarterAppBuildsAndRuns);
Run("Rust Cargo no_std init writes library scaffold", RustCargoNoStdInitWritesLibraryScaffold);
Run("Init target name resolves to project file path", InitTargetNameResolvesToProjectFilePath);
Run("CLI init requires explicit profile", CliInitRequiresExplicitProfile);
Run("CLI init profiles list built in profiles", CliInitProfilesListBuiltInProfiles);
Run("Shared SmartHome Rust example runs from repo root without crimson on PATH", SharedSmartHomeRustExampleRunsFromRepoRootWithoutCrimsonOnPath);
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
namespace Billing {
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

    var generated = Path.Combine(root, "src", "Generated", "Billing", "CustomerService.g.cs");
    var user = Path.Combine(root, "src", "User", "Billing", "CustomerService.cs");

    if (!File.Exists(generated))
    {
        throw new InvalidOperationException("Generated class file was not materialized.");
    }

    if (!File.Exists(user))
    {
        throw new InvalidOperationException("User class file was not materialized.");
    }

    var projectJson = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(projectFile));
    if (!projectJson.TryGetProperty("groups", out var groups))
    {
        throw new InvalidOperationException("Project file was not initialized correctly.");
    }

    var output = groups.GetProperty("csharp").GetProperty("output").GetString();
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
namespace Billing {
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

    var userFile = Path.Combine(root, "src", "User", "Billing", "CustomerService.cs");
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

    var props = Path.Combine(root, ".crimson", "msbuild", "Crimson.csharp.props");
    var targets = Path.Combine(root, ".crimson", "msbuild", "Crimson.csharp.targets");

    if (!File.Exists(props))
    {
        throw new InvalidOperationException("MSBuild props file was not created.");
    }

    if (!File.Exists(targets))
    {
        throw new InvalidOperationException("MSBuild targets file was not created.");
    }

    if (!File.Exists(Path.Combine(root, "README.md")))
    {
        throw new InvalidOperationException("Scaffold README was not created.");
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

    AssertContains("Porch Light: 42%", result.Output);
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
    AssertContains(".merge/", contents);
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

    var cmakeModule = Path.Combine(root, ".crimson", "cmake", "Crimson.cpp.cmake");
    if (!File.Exists(cmakeModule))
    {
        throw new InvalidOperationException("CMake integration file was not created.");
    }

    if (!File.Exists(Path.Combine(root, "CMakeLists.txt")))
    {
        throw new InvalidOperationException("CMakeLists.txt was not created.");
    }

    if (!File.Exists(Path.Combine(root, "README.md")))
    {
        throw new InvalidOperationException("Scaffold README was not created.");
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

    AssertContains("Porch Light: 42%", run.Output);
}

void CppCMakeCrossInitWritesToolchainScaffold()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "cpp-cmake-cross", starter: false);

    var toolchain = Path.Combine(root, "cmake", "toolchains", "generic.cmake");
    if (!File.Exists(toolchain))
    {
        throw new InvalidOperationException("Generic cross toolchain scaffold was not created.");
    }

    var presets = Path.Combine(root, "CMakePresets.json");
    if (!File.Exists(presets))
    {
        throw new InvalidOperationException("CMakePresets.json was not created for cross profile.");
    }

    AssertContains("cross-debug", File.ReadAllText(presets));
    AssertContains("CMAKE_TOOLCHAIN_FILE", File.ReadAllText(presets));
}

void SharedSmartHomeCppExampleBuildsAndRuns()
{
    if (!CommandExists("cmake"))
    {
        Console.WriteLine("SKIP Shared SmartHome C++ example builds and runs (cmake not installed)");
        return;
    }

    var repoRoot = ResolveRepoRoot();
    var tempRoot = Path.Combine(Path.GetTempPath(), $"crimson-cpp-example-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);

    var sourceRoot = Path.Combine(repoRoot, "examples", "SmartHomeDemo");
    var exampleRoot = Path.Combine(tempRoot, "SmartHomeDemo");
    CopyDirectory(sourceRoot, exampleRoot);

    var cliProjectPath = Path.Combine(repoRoot, "src", "Crimson.Cli", "Crimson.Cli.csproj");
    var configure = new ExecResult(
        "cmake",
        [
            "--preset", "gcc-debug",
            "-DCrimsonCommand=dotnet",
            $"-DCrimsonCommandArguments=run --project {cliProjectPath} --",
        ],
        exampleRoot).Run();

    if (configure.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected example cmake configure to succeed.{Environment.NewLine}{configure.Output}");
    }

    var build = new ExecResult("cmake", ["--build", "--preset", "gcc-debug"], exampleRoot).Run();
    if (build.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected example cmake build to succeed.{Environment.NewLine}{build.Output}");
    }

    var binaryPath = Path.Combine(exampleRoot, "build", "gcc-debug", "SmartHomeCppApp");
    var run = new ExecResult(binaryPath, Array.Empty<string>(), exampleRoot).Run();
    if (run.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected built cpp example to run successfully.{Environment.NewLine}{run.Output}");
    }

    AssertContains("Home: Willow Lane", run.Output);
    AssertContains("Automation chain from porch.eufy:", run.Output);
    AssertContains("Scene after apply: Evening Arrival", run.Output);
}

void RustCargoInitWritesBuildIntegration()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "rust-cargo", starter: false);

    var cargoHelper = Path.Combine(root, ".crimson", "cargo", "Crimson.rust.rs");
    if (!File.Exists(cargoHelper))
    {
        throw new InvalidOperationException("Cargo integration helper was not created.");
    }

    if (!File.Exists(Path.Combine(root, "Cargo.toml")))
    {
        throw new InvalidOperationException("Cargo.toml was not created.");
    }

    var gitIgnore = File.ReadAllText(Path.Combine(root, ".gitignore"));
    AssertContains("target/", gitIgnore);
}

void RustCargoStarterAppBuildsAndRuns()
{
    if (!CommandExists("cargo"))
    {
        Console.WriteLine("SKIP Rust Cargo starter app builds and runs (cargo not installed)");
        return;
    }

    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "rust-cargo", starter: true);

    var repoRoot = ResolveRepoRoot();
    var cliProjectPath = Path.Combine(repoRoot, "src", "Crimson.Cli", "Crimson.Cli.csproj");
    var run = new ExecResult(
        "cargo",
        ["run"],
        root,
        [
            new KeyValuePair<string, string?>("CRIMSON_COMMAND", "dotnet"),
            new KeyValuePair<string, string?>("CRIMSON_COMMAND_ARGUMENTS", $"run --project {cliProjectPath} --"),
        ]).Run();

    if (run.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected cargo run to succeed.{Environment.NewLine}{run.Output}");
    }

    AssertContains("Porch Light: 42%", run.Output);
}

void RustCargoNoStdInitWritesLibraryScaffold()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-system-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Billing.crimsonproj");
    workspace.InitProject(projectFile, "rust-cargo-no-std", starter: false);

    var library = Path.Combine(root, "src", "lib.rs");
    if (!File.Exists(library))
    {
        throw new InvalidOperationException("Rust no_std library scaffold was not created.");
    }

    AssertContains("#![no_std]", File.ReadAllText(library));
    AssertContains("extern crate alloc;", File.ReadAllText(library));
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
    projectJson["groups"]!["csharp"]!["output"] = "codegen";
    File.WriteAllText(projectFile, projectJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    Directory.CreateDirectory(Path.Combine(root, "contracts"));
    File.WriteAllText(Path.Combine(root, "contracts", "customer.idl"), """
namespace Billing {
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
  <Import Project="../.crimson/msbuild/Crimson.csharp.props" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CrimsonProjectFile>../Billing.crimsonproj</CrimsonProjectFile>
    <CrimsonCommand>dotnet</CrimsonCommand>
    <CrimsonCommandArguments>run --project &quot;{{cliProjectPath}}&quot; --</CrimsonCommandArguments>
  </PropertyGroup>

  <Import Project="../.crimson/msbuild/Crimson.csharp.targets" />
</Project>
""");

    File.WriteAllText(Path.Combine(appDirectory, "Program.cs"), """
using Billing;

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
namespace Billing {
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

    var userFile = Path.Combine(root, "src", "User", "Billing", "CustomerService.cs");
    var customized = """
using System;
using System.Collections.Generic;

namespace Billing;

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

void SharedSmartHomeRustExampleRunsFromRepoRootWithoutCrimsonOnPath()
{
    if (!CommandExists("cargo"))
    {
        Console.WriteLine("SKIP Shared SmartHome Rust example runs from repo root without crimson on PATH (cargo not installed)");
        return;
    }

    var repoRoot = ResolveRepoRoot();
    var result = new ExecResult(
        "cargo",
        ["run", "--manifest-path", Path.Combine(repoRoot, "examples", "SmartHomeDemo", "Cargo.toml")],
        repoRoot).Run();

    if (result.ExitCode != 0)
    {
        throw new InvalidOperationException($"Expected rust example app to run successfully.{Environment.NewLine}{result.Output}");
    }

    AssertContains("Home: Willow Lane", result.Output);
    AssertContains("Automation chain from porch.eufy:", result.Output);
    AssertContains("Scene after apply: Evening Arrival", result.Output);
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
    AssertContains("cpp-cmake-cross", result.Output);
    AssertContains("rust-cargo", result.Output);
    AssertContains("rust-cargo-no-std", result.Output);
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

static void CopyDirectory(string sourceRoot, string destinationRoot)
{
    foreach (var directory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
    {
        Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
    }

    foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
    {
        var destination = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(file, destination, overwrite: true);
    }
}

static bool CommandExists(string command)
{
    var path = Environment.GetEnvironmentVariable("PATH");
    if (string.IsNullOrWhiteSpace(path))
    {
        return false;
    }

    foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
    {
        var candidate = Path.Combine(directory, command);
        if (File.Exists(candidate))
        {
            return true;
        }
    }

    return false;
}

sealed class ExecResult(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
{
    private readonly IReadOnlyList<KeyValuePair<string, string?>> _environmentVariables = Array.Empty<KeyValuePair<string, string?>>();

    public ExecResult(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyList<KeyValuePair<string, string?>> environmentVariables)
        : this(fileName, arguments, workingDirectory)
    {
        _environmentVariables = environmentVariables;
    }

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

        foreach (var environmentVariable in _environmentVariables)
        {
            startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
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
