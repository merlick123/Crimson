using System.Text.Json;
using Crimson.Core.Model;

namespace Crimson.Core.Generation;

public sealed record GeneratedFile(string RelativePath, string Content);

public enum TargetMergeMode
{
    ThreeWay,
    PreferGenerated,
}

public enum TargetOutputContentType
{
    Other,
    SourceFiles,
    HeaderFiles,
}

public enum TargetOutputOwnership
{
    Generated,
    UserOwned,
}

public sealed record TargetOutputDescriptor(
    string Name,
    string RelativeOutputPath,
    TargetMergeMode MergeMode,
    TargetOutputContentType ContentType,
    TargetOutputOwnership Ownership);

public sealed record EmittedTargetOutput(
    string Name,
    IReadOnlyList<GeneratedFile> Files);

public interface ITargetEmitter
{
    string TargetName { get; }

    string DefaultOutputRoot { get; }

    IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration);

    void ValidateTarget(CompilationSetModel compilation, JsonElement configuration);

    IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration);
}
