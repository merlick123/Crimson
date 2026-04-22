using System.Text.Json;
using Crimson.Core.Model;
using Crimson.Core.Utility;

namespace Crimson.Core.Generation.CSharp;

public sealed record CSharpTargetOptions(string OutputRoot)
{
    public static CSharpTargetOptions Default => new("src");
}

public sealed class CSharpTargetEmitter : ITargetEmitter
{
    private static readonly TargetOutputDescriptor[] OutputDescriptors =
    [
        new("Generated", "Generated", TargetMergeMode.PreferGenerated, TargetOutputContentType.SourceFiles, TargetOutputOwnership.Generated),
        new("User", "User", TargetMergeMode.ThreeWay, TargetOutputContentType.SourceFiles, TargetOutputOwnership.UserOwned),
    ];

    private readonly CSharpEmitter _emitter = new();

    public string TargetName => "csharp";

    public string ResolveOutputRoot(JsonElement configuration) =>
        ResolveOptions(configuration).OutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
        OutputDescriptors;

    public void ValidateTarget(CompilationSetModel compilation, JsonElement configuration)
    {
    }

    public IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration)
    {
        var result = _emitter.Emit(compilation);
        return
        [
            new EmittedTargetOutput("Generated", result.GeneratedFiles),
            new EmittedTargetOutput("User", result.UserFiles),
        ];
    }

    private static CSharpTargetOptions ResolveOptions(JsonElement configuration) =>
        new(JsonConfigurationHelpers.ResolveOutputRoot(configuration, CSharpTargetOptions.Default.OutputRoot));
}
