using System.Text.Json;
using Crimson.Core;

var failures = new List<string>();

Run("Init build pipeline creates project output", BuildPipelineCreatesOutput);
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

    var generated = Path.Combine(root, "out", "csharp", "project", "Generated", "Billing", "Contracts", "CustomerService.g.cs");
    var user = Path.Combine(root, "out", "csharp", "project", "User", "Billing", "Contracts", "CustomerService.cs");

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
}
