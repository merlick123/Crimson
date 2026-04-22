using System.Text;
using System.Text.Json;
using Crimson.Core.Generation;
using Crimson.Core.Utility;

namespace Crimson.Core.Host;

public sealed class CMakeHostIntegration : IHostIntegration
{
    public string HostName => "cmake";

    public IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration)
    {
        var buildDirectory = ResolveBuildDirectory(configuration);
        return [buildDirectory.EndsWith("/", StringComparison.Ordinal) ? buildDirectory : buildDirectory + "/"];
    }

    public void ValidateHost(string projectFilePath, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        var cppTargets = targets
            .Where(static target => string.Equals(target.TargetName, "cpp", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (cppTargets.Length == 0)
        {
            throw new InvalidOperationException($"Host integration '{HostName}' requires a 'cpp' target in '{projectFilePath}'.");
        }

        if (cppTargets.Length > 1)
        {
            throw new InvalidOperationException($"Host integration '{HostName}' currently supports a single 'cpp' target in '{projectFilePath}'.");
        }
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        var cppTarget = targets.Single(static target => string.Equals(target.TargetName, "cpp", StringComparison.OrdinalIgnoreCase));
        var cmakeRoot = Path.Combine(projectDirectory, ".crimson", "cmake");
        Directory.CreateDirectory(cmakeRoot);
        File.WriteAllText(
            Path.Combine(cmakeRoot, "Crimson.Cpp.cmake"),
            RenderModule(projectFilePath, projectDirectory, cppTarget));
    }

    private static string RenderModule(string projectFilePath, string projectDirectory, ResolvedHostTarget target)
    {
        var projectFileRelativePath = EscapeCMake(PathHelpers.NormalizeRelativePath(Path.GetRelativePath(projectDirectory, projectFilePath)));
        var sourceGlobs = target.Outputs
            .Where(static output => output.ContentType == TargetOutputContentType.SourceFiles)
            .Select(static output => PathHelpers.NormalizeRelativePath(Path.Combine(output.RelativeOutputPath, "*.cpp")))
            .ToArray();
        var headerRoots = target.Outputs
            .Where(static output => output.ContentType == TargetOutputContentType.HeaderFiles)
            .Select(static output => PathHelpers.NormalizeRelativePath(output.RelativeOutputPath))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("set(CrimsonCommand \"crimson\" CACHE STRING \"Command used to invoke Crimson\")");
        builder.AppendLine("set(CrimsonCommandArguments \"\" CACHE STRING \"Extra arguments passed before the Crimson subcommand\")");
        builder.AppendLine();
        builder.AppendLine("function(_crimson_cpp_run_build)");
        builder.AppendLine("  get_filename_component(_crimson_project_root \"${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../..\" ABSOLUTE)");
        builder.AppendLine($"  set(_crimson_project_file \"${{_crimson_project_root}}/{projectFileRelativePath}\")");
        builder.AppendLine("  separate_arguments(_crimson_command_arguments NATIVE_COMMAND \"${CrimsonCommandArguments}\")");
        builder.AppendLine("  execute_process(");
        builder.AppendLine("    COMMAND \"${CrimsonCommand}\" ${_crimson_command_arguments} build \"${_crimson_project_file}\"");
        builder.AppendLine("    WORKING_DIRECTORY \"${_crimson_project_root}\"");
        builder.AppendLine("    RESULT_VARIABLE _crimson_result");
        builder.AppendLine("    OUTPUT_VARIABLE _crimson_stdout");
        builder.AppendLine("    ERROR_VARIABLE _crimson_stderr)");
        builder.AppendLine("  if(NOT _crimson_result EQUAL 0)");
        builder.AppendLine("    message(FATAL_ERROR \"Crimson build failed:\\n${_crimson_stdout}${_crimson_stderr}\")");
        builder.AppendLine("  endif()");
        builder.AppendLine("endfunction()");
        builder.AppendLine();
        builder.AppendLine("function(crimson_configure_cpp_target target_name)");
        builder.AppendLine("  if(NOT TARGET \"${target_name}\")");
        builder.AppendLine("    message(FATAL_ERROR \"Target '${target_name}' does not exist.\")");
        builder.AppendLine("  endif()");
        builder.AppendLine();
        builder.AppendLine("  get_filename_component(_crimson_project_root \"${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../..\" ABSOLUTE)");
        builder.AppendLine($"  set(_crimson_project_file \"${{_crimson_project_root}}/{projectFileRelativePath}\")");
        builder.AppendLine("  set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS \"${_crimson_project_file}\")");
        builder.AppendLine("  file(GLOB_RECURSE _crimson_contract_files CONFIGURE_DEPENDS \"${_crimson_project_root}/contracts/*.idl\")");
        builder.AppendLine("  separate_arguments(_crimson_command_arguments NATIVE_COMMAND \"${CrimsonCommandArguments}\")");
        builder.AppendLine();
        builder.AppendLine("  _crimson_cpp_run_build()");
        builder.AppendLine();
        builder.AppendLine("  set(_crimson_sources)");

        foreach (var sourceGlob in sourceGlobs)
        {
            var escapedGlob = EscapeCMake(PathHelpers.NormalizeRelativePath(Path.Combine(target.OutputRoot, sourceGlob)));
            builder.AppendLine($"  file(GLOB_RECURSE _crimson_globbed_sources CONFIGURE_DEPENDS \"${{_crimson_project_root}}/{escapedGlob}\")");
            builder.AppendLine("  list(APPEND _crimson_sources ${_crimson_globbed_sources})");
        }

        builder.AppendLine();
        builder.AppendLine("  if(_crimson_sources)");
        builder.AppendLine("    target_sources(\"${target_name}\" PRIVATE ${_crimson_sources})");
        builder.AppendLine("  endif()");

        foreach (var headerRoot in headerRoots)
        {
            var escapedHeaderRoot = EscapeCMake(PathHelpers.NormalizeRelativePath(Path.Combine(target.OutputRoot, headerRoot)));
            builder.AppendLine($"  target_include_directories(\"${{target_name}}\" PRIVATE \"${{_crimson_project_root}}/{escapedHeaderRoot}\")");
        }

        builder.AppendLine();
        builder.AppendLine("  add_custom_target(\"${target_name}_crimson_codegen\"");
        builder.AppendLine("    COMMAND \"${CrimsonCommand}\" ${_crimson_command_arguments} build \"${_crimson_project_file}\"");
        builder.AppendLine("    WORKING_DIRECTORY \"${_crimson_project_root}\"");
        builder.AppendLine("    DEPENDS ${_crimson_contract_files} \"${_crimson_project_file}\"");
        builder.AppendLine("    VERBATIM)");
        builder.AppendLine("  add_dependencies(\"${target_name}\" \"${target_name}_crimson_codegen\")");
        builder.AppendLine("endfunction()");
        return builder.ToString();
    }

    private static string ResolveBuildDirectory(JsonElement configuration)
    {
        if (configuration.ValueKind == JsonValueKind.Object &&
            configuration.TryGetProperty("buildDirectory", out var buildDirectoryElement) &&
            buildDirectoryElement.GetString() is { Length: > 0 } buildDirectory)
        {
            return PathHelpers.NormalizeRelativePath(buildDirectory);
        }

        return "build";
    }

    private static string EscapeCMake(string value) =>
        value.Replace("\\", "/", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
