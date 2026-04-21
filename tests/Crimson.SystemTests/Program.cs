using System.Text.Json;
using Crimson.Core;

var failures = new List<string>();

Run("Init build pipeline creates project output", BuildPipelineCreatesOutput);
Run("Build heals missing user files", BuildHealsMissingUserFiles);
Run("Init writes reusable MSBuild integration", InitWritesMsBuildIntegration);
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
