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
            var profile = RequireOptionValue(args, "--profile", "Expected --profile <id>. Run 'crimson init-profiles' to list available profiles.");
            workspace.InitProject(projectFile, profile, starter);
            Console.WriteLine($"Initialized {projectFile}");
            return 0;
        }

        case "init-profiles":
        {
            PrintInitProfiles(workspace);
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

static string RequireOptionValue(string[] arguments, string optionName, string message)
{
    for (var index = 0; index < arguments.Length; index++)
    {
        if (string.Equals(arguments[index], optionName, StringComparison.Ordinal))
        {
            if (index + 1 >= arguments.Length)
            {
                throw new InvalidOperationException(message);
            }

            return arguments[index + 1];
        }

        var prefix = optionName + "=";
        if (arguments[index].StartsWith(prefix, StringComparison.Ordinal))
        {
            return arguments[index][prefix.Length..];
        }
    }

    throw new InvalidOperationException(message);
}

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
    Console.WriteLine("  crimson init <project-name|path> --profile <id> [--starter]");
    Console.WriteLine("  crimson init-profiles");
    Console.WriteLine("  crimson parse <file.idl>");
    Console.WriteLine("  crimson validate <project.crimsonproj>");
    Console.WriteLine("  crimson generate <project.crimsonproj>");
    Console.WriteLine("  crimson merge <project.crimsonproj>");
    Console.WriteLine("  crimson build <project.crimsonproj>");
    Console.WriteLine("  crimson help");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  init           Create a new Crimson project from an init profile.");
    Console.WriteLine("  init-profiles  List available init profiles.");
    Console.WriteLine("  parse     Parse a single .idl file and emit the typed JSON AST.");
    Console.WriteLine("  validate  Parse and validate a Crimson project without generating output.");
    Console.WriteLine("  generate  Generate staged output into .merge/<group>/current.");
    Console.WriteLine("  merge     Merge staged output into the live project tree.");
    Console.WriteLine("  build     Generate and merge in one step.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  crimson init SmartHomeDemo --profile csharp --starter");
    Console.WriteLine("  crimson init SensorNode --profile cpp-cmake-gcc --starter");
    Console.WriteLine("  crimson init ControlNode --profile cpp-cmake-cross");
    Console.WriteLine("  crimson init DeviceBridge --profile rust-cargo --starter");
    Console.WriteLine("  crimson init-profiles");
    Console.WriteLine("  crimson validate examples/SmartHomeDemo/SmartHome.crimsonproj");
    Console.WriteLine("  crimson build examples/SmartHomeDemo/SmartHome.crimsonproj");
}

static void PrintInitProfiles(CrimsonWorkspace workspace)
{
    Console.WriteLine("Available init profiles:");
    foreach (var profile in workspace.GetInitProfiles())
    {
        Console.WriteLine($"  {profile.ProfileId,-18} {profile.DisplayName}");
        Console.WriteLine($"    {profile.Description}");
    }
}
