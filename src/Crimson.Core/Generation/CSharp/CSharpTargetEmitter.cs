using System.Text.Json;
using Crimson.Core.Model;

namespace Crimson.Core.Generation.CSharp;

public sealed record CSharpTargetOptions(string OutputRoot)
{
    public static CSharpTargetOptions Default => new("src");
}

public sealed class CSharpTargetEmitter : ITargetEmitter
{
    private static readonly TargetOutputDescriptor[] OutputDescriptors =
    [
        new("Generated", "Generated", TargetMergeMode.PreferGenerated),
        new("User", "User", TargetMergeMode.ThreeWay),
    ];

    private readonly CSharpEmitter _emitter = new();

    public string TargetName => "csharp";

    public object? GetDefaultProjectOptions() => new
    {
        output = CSharpTargetOptions.Default.OutputRoot,
    };

    public string ResolveOutputRoot(JsonElement configuration) =>
        ResolveOptions(configuration).OutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
        OutputDescriptors;

    public void PrepareProject(string projectDirectory, JsonElement configuration) =>
        CSharpBuildIntegration.Write(projectDirectory, ResolveOptions(configuration));

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

    private static CSharpTargetOptions ResolveOptions(JsonElement configuration)
    {
        var outputRoot = configuration.ValueKind == JsonValueKind.Object && configuration.TryGetProperty("output", out var outputElement)
            ? outputElement.GetString()
            : null;

        return new CSharpTargetOptions(outputRoot ?? CSharpTargetOptions.Default.OutputRoot);
    }
}
