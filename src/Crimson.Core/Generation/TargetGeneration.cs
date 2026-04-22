using System.Text.Json;
using Crimson.Core.Model;

namespace Crimson.Core.Generation;

public sealed record GeneratedFile(string RelativePath, string Content);

public enum TargetMergeMode
{
    ThreeWay,
    PreferGenerated,
}

public sealed record TargetOutputDescriptor(
    string Name,
    string RelativeOutputPath,
    TargetMergeMode MergeMode);

public sealed record EmittedTargetOutput(
    string Name,
    IReadOnlyList<GeneratedFile> Files);

public interface ITargetEmitter
{
    string TargetName { get; }

    object? GetDefaultProjectOptions();

    string ResolveOutputRoot(JsonElement configuration);

    IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration);

    void PrepareProject(string projectDirectory, JsonElement configuration);

    void ValidateTarget(CompilationSetModel compilation, JsonElement configuration);

    IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration);
}
