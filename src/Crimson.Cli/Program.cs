using Crimson.Core;
using Crimson.Core.Model;
using Crimson.Core.Merge;

var workspace = new CrimsonWorkspace();

try
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 1;
    }

    switch (args[0])
    {
        case "parse":
        {
            var file = RequireArgument(args, 1, "Expected a .idl file path.");
            var model = workspace.ParseFile(file);
            Console.WriteLine(workspace.EmitAstJson(model));
            return 0;
        }

        case "init":
        {
            var projectFile = RequireArgument(args, 1, "Expected a .crimsonproj path.");
            var starter = args.Contains("--starter", StringComparer.Ordinal);
            workspace.InitProject(projectFile, starter);
            Console.WriteLine($"Initialized {projectFile}");
            Console.WriteLine("Recommended .gitignore entries:");
            Console.WriteLine(".crimson/raw-previous/Generated/");
            Console.WriteLine(".crimson/raw-current/");
            Console.WriteLine(".crimson/merge-backup/");
            return 0;
        }

        case "generate":
        {
            workspace.Generate(RequireArgument(args, 1, "Expected a .crimsonproj path."));
            return 0;
        }

        case "merge":
        {
            var result = workspace.Merge(RequireArgument(args, 1, "Expected a .crimsonproj path."));
            return HandleMergeResult(result);
        }

        case "build":
        {
            var result = workspace.Build(RequireArgument(args, 1, "Expected a .crimsonproj path."));
            return HandleMergeResult(result);
        }

        default:
            PrintUsage();
            return 1;
    }
}
catch (DiagnosticException exception)
{
    foreach (var diagnostic in exception.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}");
    }

    return 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

static int HandleMergeResult(MergeResult result)
{
    foreach (var file in result.UpdatedFiles)
    {
        Console.WriteLine($"updated {file}");
    }

    foreach (var file in result.DeletedFiles)
    {
        Console.WriteLine($"deleted {file}");
    }

    foreach (var conflict in result.Conflicts)
    {
        Console.Error.WriteLine($"conflict {conflict.RelativePath}: {conflict.Reason}");
    }

    return result.Conflicts.Count == 0 ? 0 : 2;
}

static string RequireArgument(string[] arguments, int index, string message) =>
    arguments.Length > index ? arguments[index] : throw new InvalidOperationException(message);

static void PrintUsage()
{
    Console.WriteLine("crimson parse <file.idl>");
    Console.WriteLine("crimson init <project.crimsonproj> [--starter]");
    Console.WriteLine("crimson generate <project.crimsonproj>");
    Console.WriteLine("crimson merge <project.crimsonproj>");
    Console.WriteLine("crimson build <project.crimsonproj>");
}
