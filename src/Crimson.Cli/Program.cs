using Crimson.Core;
using Crimson.Core.Model;
using Crimson.Core.Merge;
using Crimson.Core.Projects;

var workspace = new CrimsonWorkspace();

try
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 0;
    }

    switch (args[0])
    {
        case "help":
        case "--help":
        case "-h":
        {
            PrintUsage();
            return 0;
        }

        case "parse":
        {
            var file = RequireArgument(args, 1, "Expected a .idl file path.");
            var model = workspace.ParseFile(file);
            Console.WriteLine(workspace.EmitAstJson(model));
            return 0;
        }

        case "init":
        {
            var target = RequireArgument(args, 1, "Expected a project name or .crimsonproj path.");
            var projectFile = CrimsonProjectFile.ResolveInitProjectFilePath(target, Environment.CurrentDirectory);
            var starter = args.Contains("--starter", StringComparer.Ordinal);
            workspace.InitProject(projectFile, starter);
            Console.WriteLine($"Initialized {projectFile}");
            return 0;
        }

        case "generate":
        {
            workspace.Generate(RequireArgument(args, 1, "Expected a .crimsonproj path."));
            return 0;
        }

        case "validate":
        {
            workspace.ValidateProject(RequireArgument(args, 1, "Expected a .crimsonproj path."));
            Console.WriteLine("Validation succeeded");
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
            var exitCode = HandleMergeResult(result);
            if (exitCode == 0)
            {
                Console.WriteLine("Build succeeded");
            }

            return exitCode;
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
        Console.Error.WriteLine(FormatDiagnostic(diagnostic));
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

static string FormatDiagnostic(Diagnostic diagnostic)
{
    if (diagnostic.Source is null)
    {
        return $"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}";
    }

    return $"{diagnostic.Severity} {diagnostic.Code} {diagnostic.Source.FilePath}:{diagnostic.Source.Start.Line}:{diagnostic.Source.Start.Column}: {diagnostic.Message}";
}

static void PrintUsage()
{
    Console.WriteLine("Crimson CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  crimson init <project-name|path> [--starter]");
    Console.WriteLine("  crimson parse <file.idl>");
    Console.WriteLine("  crimson validate <project.crimsonproj>");
    Console.WriteLine("  crimson generate <project.crimsonproj>");
    Console.WriteLine("  crimson merge <project.crimsonproj>");
    Console.WriteLine("  crimson build <project.crimsonproj>");
    Console.WriteLine("  crimson help");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  init      Create a new Crimson project directory, project file, state folder, and .gitignore.");
    Console.WriteLine("  parse     Parse a single .idl file and emit the typed JSON AST.");
    Console.WriteLine("  validate  Parse and validate a Crimson project without generating output.");
    Console.WriteLine("  generate  Generate staged raw output into .crimson/raw-current.");
    Console.WriteLine("  merge     Merge staged output into the live project tree.");
    Console.WriteLine("  build     Generate and merge in one step.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  crimson init BillingDemo --starter");
    Console.WriteLine("  crimson validate BillingDemo/BillingDemo.crimsonproj");
    Console.WriteLine("  crimson build examples/BillingDemo/Billing.crimsonproj");
}
