using System.Text.Json;
using Crimson.Core.Model;

namespace Crimson.Core.Generation.Cpp;

public sealed record CppTargetOptions(string OutputRoot)
{
    public static CppTargetOptions Default => new("cpp");
}

public sealed class CppTargetEmitter : ITargetEmitter
{
    private static readonly TargetOutputDescriptor[] OutputDescriptors =
    [
        new("GeneratedHeaders", Path.Combine("generated", "include"), TargetMergeMode.PreferGenerated, TargetOutputContentType.HeaderFiles, TargetOutputOwnership.Generated),
        new("GeneratedSources", Path.Combine("generated", "src"), TargetMergeMode.PreferGenerated, TargetOutputContentType.SourceFiles, TargetOutputOwnership.Generated),
        new("UserHeaders", Path.Combine("user", "include"), TargetMergeMode.ThreeWay, TargetOutputContentType.HeaderFiles, TargetOutputOwnership.UserOwned),
        new("UserSources", Path.Combine("user", "src"), TargetMergeMode.ThreeWay, TargetOutputContentType.SourceFiles, TargetOutputOwnership.UserOwned),
    ];

    private readonly CppEmitter _emitter = new();

    public string TargetName => "cpp";

    public string ResolveOutputRoot(JsonElement configuration) =>
        ResolveOptions(configuration).OutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
        OutputDescriptors;

    public void ValidateTarget(CompilationSetModel compilation, JsonElement configuration)
    {
        _emitter.ValidateTargetSupport(compilation);
    }

    public IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration)
    {
        var result = _emitter.Emit(compilation);
        return
        [
            new EmittedTargetOutput("GeneratedHeaders", result.GeneratedHeaders),
            new EmittedTargetOutput("GeneratedSources", result.GeneratedSources),
            new EmittedTargetOutput("UserHeaders", result.UserHeaders),
            new EmittedTargetOutput("UserSources", result.UserSources),
        ];
    }

    private static CppTargetOptions ResolveOptions(JsonElement configuration)
    {
        var outputRoot = configuration.ValueKind == JsonValueKind.Object && configuration.TryGetProperty("output", out var outputElement)
            ? outputElement.GetString()
            : null;

        return new CppTargetOptions(outputRoot ?? CppTargetOptions.Default.OutputRoot);
    }
}
