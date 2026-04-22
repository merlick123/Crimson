using System.Text.Json;
using Crimson.Core.Model;
using Crimson.Core.Utility;

namespace Crimson.Core.Generation.Cpp;

public enum CppInterfaceHandleStyle
{
    SharedPtr,
    RawPtr,
}

public sealed record CppTargetOptions(string OutputRoot, CppInterfaceHandleStyle InterfaceHandleStyle)
{
    public static CppTargetOptions Default => new("cpp", CppInterfaceHandleStyle.SharedPtr);
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
        var result = _emitter.Emit(compilation, ResolveOptions(configuration));
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
        var interfaceHandleStyle = JsonConfigurationHelpers.GetOptionalString(configuration, "interfaceHandleStyle") is { Length: > 0 } handleStyleText
            ? ParseInterfaceHandleStyle(handleStyleText)
            : CppTargetOptions.Default.InterfaceHandleStyle;

        return new CppTargetOptions(
            JsonConfigurationHelpers.ResolveOutputRoot(configuration, CppTargetOptions.Default.OutputRoot),
            interfaceHandleStyle);
    }

    private static CppInterfaceHandleStyle ParseInterfaceHandleStyle(string value) =>
        value.ToLowerInvariant() switch
        {
            "shared_ptr" or "sharedptr" or "shared" => CppInterfaceHandleStyle.SharedPtr,
            "raw_ptr" or "rawptr" or "raw" or "observer" => CppInterfaceHandleStyle.RawPtr,
            _ => throw new InvalidOperationException($"Unknown cpp interfaceHandleStyle '{value}'. Supported values: shared_ptr, raw_ptr."),
        };
}
