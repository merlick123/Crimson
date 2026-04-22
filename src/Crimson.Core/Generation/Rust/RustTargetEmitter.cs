using System.Text.Json;
using Crimson.Core.Model;
using Crimson.Core.Utility;

namespace Crimson.Core.Generation.Rust;

public enum RustSupportProvider
{
    Generated,
    External,
}

public enum RustSupportProfile
{
    Std,
    NoStd,
}

public sealed record RustSupportOptions(RustSupportProvider Provider, RustSupportProfile Profile, string ModulePath)
{
    public static RustSupportOptions StdDefault => new(RustSupportProvider.Generated, RustSupportProfile.Std, "crate::generated::crimson_support");

    public static RustSupportOptions NoStdDefault => new(RustSupportProvider.Generated, RustSupportProfile.NoStd, "crate::generated::crimson_support");
}

public sealed record RustTargetOptions(string OutputRoot, RustSupportOptions Support)
{
    public static RustTargetOptions Default => new("src", RustSupportOptions.StdDefault);
}

public sealed class RustTargetEmitter : ITargetEmitter
{
    private static readonly TargetOutputDescriptor[] OutputDescriptors =
    [
        new("Generated", "generated", TargetMergeMode.PreferGenerated, TargetOutputContentType.SourceFiles, TargetOutputOwnership.Generated),
        new("User", "user", TargetMergeMode.ThreeWay, TargetOutputContentType.SourceFiles, TargetOutputOwnership.UserOwned),
    ];

    private readonly RustEmitter _emitter = new();

    public string TargetName => "rust";

    public string DefaultOutputRoot => RustTargetOptions.Default.OutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
        OutputDescriptors;

    public void ValidateTarget(CompilationSetModel compilation, JsonElement configuration)
    {
        _emitter.ValidateTargetSupport(compilation, ResolveOptions(configuration));
    }

    public IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration)
    {
        var result = _emitter.Emit(compilation, ResolveOptions(configuration));
        return
        [
            new EmittedTargetOutput("Generated", result.GeneratedFiles),
            new EmittedTargetOutput("User", result.UserFiles),
        ];
    }

    private static RustTargetOptions ResolveOptions(JsonElement configuration)
    {
        var support = RustSupportOptions.StdDefault;
        if (JsonConfigurationHelpers.GetOptionalObject(configuration, "support") is { } supportElement)
        {
            var provider = JsonConfigurationHelpers.GetOptionalString(supportElement, "provider") is { Length: > 0 } providerText
                ? ParseProvider(providerText)
                : RustSupportOptions.StdDefault.Provider;
            var profile = JsonConfigurationHelpers.GetOptionalString(supportElement, "profile") is { Length: > 0 } profileText
                ? ParseProfile(profileText)
                : RustSupportOptions.StdDefault.Profile;
            var modulePath = JsonConfigurationHelpers.GetOptionalString(supportElement, "modulePath") is { Length: > 0 } configuredModulePath
                ? configuredModulePath
                : provider == RustSupportProvider.External
                    ? "crate::support"
                    : "crate::generated::crimson_support";

            support = new RustSupportOptions(provider, profile, modulePath);
        }

        return new RustTargetOptions(
            JsonConfigurationHelpers.ResolveOutputRoot(configuration, RustTargetOptions.Default.OutputRoot),
            support);
    }

    private static RustSupportProvider ParseProvider(string value) =>
        value.ToLowerInvariant() switch
        {
            "generated" => RustSupportProvider.Generated,
            "external" => RustSupportProvider.External,
            _ => throw new InvalidOperationException($"Unknown rust support provider '{value}'. Supported values: generated, external."),
        };

    private static RustSupportProfile ParseProfile(string value) =>
        value.ToLowerInvariant() switch
        {
            "std" => RustSupportProfile.Std,
            "no_std" or "nostd" => RustSupportProfile.NoStd,
            _ => throw new InvalidOperationException($"Unknown rust support profile '{value}'. Supported values: std, no_std."),
        };
}
