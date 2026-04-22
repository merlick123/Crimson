using System.Text.Json;
using System.Text.Json.Nodes;
using Crimson.Core;
using Crimson.Core.Generation;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Model;

var tests = new (string Name, Action Body)[]
{
    ("Parse interface with docs and members", ParseInterfaceWithDocs),
    ("Emit CSharp files for interface", EmitCSharpFiles),
    ("Validate project catches unresolved types", ValidateProjectCatchesUnresolvedTypes),
    ("Constants require explicit values", ConstantsRequireExplicitValues),
    ("Split namespaces across files validate and generate", SplitNamespacesAcrossFilesValidateAndGenerate),
    ("Interface composition cycle fails with CRIMSON111", InterfaceCompositionCycleFails),
    ("Multi-level composition emits inherited members", MultiLevelCompositionEmitsInheritedMembers),
    ("Multi-path composition deduplicates same origin members", MultiPathCompositionDeduplicatesSameOriginMembers),
    ("Inherited member collision from different origins errors", InheritedMemberCollisionErrors),
    ("Interface typed parameters returns and containers lower to IName", InterfaceTypedParametersReturnsAndContainersLowerToIName),
    ("Nullable types emit nullable CSharp", NullableTypesEmitNullableCSharp),
    ("Abstract interfaces emit only interface projections", AbstractInterfacesEmitOnlyInterfaceProjection),
    ("Nested type resolution through a base contract works", NestedTypeResolutionThroughBaseContractWorks),
    ("Global dot name resolution works", GlobalDotNameResolutionWorks),
    ("Relative name resolution prefers nearer scopes", RelativeNameResolutionPrefersNearerScopes),
    ("Ambiguous type references fail cleanly", AmbiguousTypeReferencesFailCleanly),
    ("Init uses registered default target emitters", InitUsesRegisteredDefaultTargetEmitters),
    ("Workspace builds arbitrary target emitters", WorkspaceBuildsArbitraryTargetEmitters),
    ("Workspace builds multiple configured targets", WorkspaceBuildsMultipleConfiguredTargets),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{test.Name}: {exception.Message}");
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

return failures.Count == 0 ? 0 : 1;

static void ParseInterfaceWithDocs()
{
    var tempFile = CreateTempIdl("""
namespace Company.Contracts {
    /// Customer API.
    interface CustomerService {
        /// The id.
        readonly int64 id;

        /// Gets a customer.
        /// @param customer_id The customer id.
        /// @return The customer result.
        string get_customer(string customer_id);
    }
}
""");

    var workspace = new CrimsonWorkspace();
    var model = workspace.ParseFile(tempFile);

    Assert.Equal(1, model.Declarations.Count);
    var namespaceDeclaration = Assert.IsType<NamespaceDeclaration>(model.Declarations[0]);
    var contract = Assert.IsType<InterfaceDeclaration>(namespaceDeclaration.Members[0]);
    Assert.Equal("CustomerService", contract.Name);
    Assert.Equal("Customer API.", contract.Documentation?.Summary);
    Assert.Equal(2, contract.Members.Count);

    var method = Assert.IsType<MethodMemberDeclaration>(contract.Members[1]);
    Assert.Equal("The customer id.", method.Documentation?.Parameters["customer_id"]);

    var json = JsonSerializer.Serialize(model, JsonDefaults.Options);
    Assert.Contains("\"kind\": \"interface\"", json);
}

static void EmitCSharpFiles()
{
    var emitter = new CSharpEmitter();
    var compilation = new CompilationSetModel([
        new CompilationUnitModel("test.idl", [
            new NamespaceDeclaration(
                "Company.Contracts",
                [],
                [],
                [],
                null,
                [
                    new InterfaceDeclaration(
                        "CustomerService",
                        ["Company", "Contracts"],
                        [],
                        [],
                        new DocumentationComment("Customer API.", new Dictionary<string, string>(), null, []),
                        false,
                        [],
                        [
                            new ValueMemberDeclaration("name", [], null, false, false, new PrimitiveTypeReference("string", false, null), null, null),
                            new MethodMemberDeclaration("get_customer", [], new DocumentationComment("Gets a customer.", new Dictionary<string, string> { ["customer_id"] = "The customer id." }, "The result.", []), new PrimitiveTypeReference("string", false, null), [
                                new MethodParameter("customer_id", new PrimitiveTypeReference("string", false, null), null, [], null, null)
                            ], null),
                        ],
                        [],
                        null)
                ],
                null)
        ])
    ]);

    var result = emitter.Emit(compilation);
    Assert.True(result.GeneratedFiles.Any(x => x.RelativePath.EndsWith("ICustomerService.g.cs", StringComparison.Ordinal)));
    var generatedClass = result.GeneratedFiles.Single(x => string.Equals(Path.GetFileName(x.RelativePath), "CustomerService.g.cs", StringComparison.Ordinal));
    Assert.True(result.UserFiles.Any(x => x.RelativePath.EndsWith("CustomerService.cs", StringComparison.Ordinal)));
    Assert.Contains("partial void OnNameGetting(ref string value);", generatedClass.Content);
    Assert.Contains("partial void OnNameSetting(ref string value);", generatedClass.Content);
    Assert.Contains("var currentValue = _name;", generatedClass.Content);
    Assert.DoesNotContain("protected virtual string OnNameGetting", generatedClass.Content);
}

static void ValidateProjectCatchesUnresolvedTypes()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Broken.crimsonproj");
    workspace.InitProject(projectFile, starter: false);
    File.WriteAllText(Path.Combine(root, "contracts", "broken.idl"), """
namespace Demo.Contracts {
    interface BrokenService {
        MissingType value;
    }
}
""");

    try
    {
        workspace.ValidateProject(projectFile);
        throw new InvalidOperationException("Expected validation to fail.");
    }
    catch (DiagnosticException exception)
    {
        Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON108"), "Expected unresolved type diagnostic.");
    }
}

static void ConstantsRequireExplicitValues()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/constants.idl", """
namespace Demo {
    const int32 Answer;

    interface Config {
        const string Name;
    }
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON114"), "Expected missing constant value diagnostic.");
}

static void SplitNamespacesAcrossFilesValidateAndGenerate()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "SmartHome.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    Directory.CreateDirectory(Path.Combine(root, "contracts", "core"));
    Directory.CreateDirectory(Path.Combine(root, "contracts", "vendors"));

    File.WriteAllText(Path.Combine(root, "contracts", "core", "capabilities.idl"), """
namespace SmartHome {
    abstract interface Device {
        string describe();
    }
}
""");

    File.WriteAllText(Path.Combine(root, "contracts", "vendors", "vendor.idl"), """
namespace SmartHome {
    interface DemoCamera : Device;
}
""");

    workspace.ValidateProject(projectFile);
    workspace.Generate(projectFile);

    var generatedInterface = Path.Combine(root, ".merge", "current", "targets", "csharp", "Generated", "SmartHome", "IDemoCamera.g.cs");
    if (!File.Exists(generatedInterface))
    {
        throw new InvalidOperationException("Expected generated interface output for split namespace declarations.");
    }
}

static void InterfaceCompositionCycleFails()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/cycle.idl", """
namespace Demo {
    interface Alpha : Beta;
    interface Beta : Alpha;
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON111"), "Expected interface composition cycle diagnostic.");
}

static void MultiLevelCompositionEmitsInheritedMembers()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/device.idl", """
namespace Demo {
    abstract interface Device {
        readonly string device_id;
    }

    abstract interface Camera : Device {
        string capture_snapshot();
    }

    interface Doorbell : Camera {
        void ring();
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedClass = ReadGenerated(project.Root, "Generated", "Demo", "Doorbell.g.cs");
    var userStub = ReadGenerated(project.Root, "User", "Demo", "Doorbell.cs");

    Assert.Contains("public string DeviceId", generatedClass);
    Assert.Contains("public virtual string CaptureSnapshot()", userStub);
    Assert.Contains("public virtual void Ring()", userStub);
}

static void MultiPathCompositionDeduplicatesSameOriginMembers()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/eufy.idl", """
namespace Demo {
    abstract interface Device {
        string describe_state();
    }

    abstract interface Camera : Device {
        string capture_snapshot();
    }

    abstract interface Speaker : Device {
        void play_announcement(string message);
    }

    interface Eufy : Camera, Speaker;
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var userStub = ReadGenerated(project.Root, "User", "Demo", "Eufy.cs");
    Assert.Equal(1, CountOccurrences(userStub, "public virtual string DescribeState()"));
    Assert.Contains("public virtual string CaptureSnapshot()", userStub);
    Assert.Contains("public virtual void PlayAnnouncement(string message)", userStub);
}

static void InheritedMemberCollisionErrors()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/conflict.idl", """
namespace Demo {
    abstract interface Camera {
        string status();
    }

    abstract interface Speaker {
        string status();
    }

    interface Eufy : Camera, Speaker;
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON113"), "Expected inherited member collision diagnostic.");
}

static void InterfaceTypedParametersReturnsAndContainersLowerToIName()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/registry.idl", """
namespace Demo {
    abstract interface Device {
        string describe();
    }

    interface Registry {
        Device get_device(Device device, list<Device> devices, set<Device> device_set, map<string, Device> device_map);
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedInterface = ReadGenerated(project.Root, "Generated", "Demo", "IRegistry.g.cs");
    Assert.Contains("IDevice GetDevice(IDevice device, List<IDevice> devices, HashSet<IDevice> deviceSet, Dictionary<string, IDevice> deviceMap);", generatedInterface);
}

static void NullableTypesEmitNullableCSharp()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/nulls.idl", """
namespace Demo {
    abstract interface Device {
        string? nickname;
    }

    enum Mode {
        On,
        Off,
    }

    interface Registry {
        string? display_name;
        list<string?>? aliases;
        Device? primary_device;
        map<string, Device?>? device_map;
        Mode? active_mode;
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedInterface = ReadGenerated(project.Root, "Generated", "Demo", "IRegistry.g.cs");
    Assert.Contains("string? DisplayName { get; set; }", generatedInterface);
    Assert.Contains("List<string?>? Aliases { get; set; }", generatedInterface);
    Assert.Contains("IDevice? PrimaryDevice { get; set; }", generatedInterface);
    Assert.Contains("Dictionary<string, IDevice?>? DeviceMap { get; set; }", generatedInterface);
    Assert.Contains("Mode? ActiveMode { get; set; }", generatedInterface);
}

static void AbstractInterfacesEmitOnlyInterfaceProjection()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/device.idl", """
namespace Demo {
    abstract interface Device {
        string describe();
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    Assert.True(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "csharp", "Generated", "Demo", "IDevice.g.cs")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "csharp", "Generated", "Demo", "Device.g.cs")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "csharp", "User", "Demo", "Device.cs")));
}

static void NestedTypeResolutionThroughBaseContractWorks()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/factory.idl", """
namespace Demo {
    abstract interface Factory {
        interface Request {
            string id;
        }
    }

    interface DoorbellFactory : Factory {
        Request create_request(Request request);
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedInterface = ReadGenerated(project.Root, "Generated", "Demo", "IDoorbellFactory.g.cs");
    Assert.Contains("IRequest CreateRequest(IRequest request);", generatedInterface);
}

static void GlobalDotNameResolutionWorks()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/global.idl", """
namespace Common {
    abstract interface Device {
        string describe();
    }
}

namespace Local.Common {
    abstract interface Device {
        string local_describe();
    }
}

namespace Local.Controllers {
    interface Registry {
        .Common.Device get_global_device();
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedInterface = ReadGenerated(project.Root, "Generated", "Local", "Controllers", "IRegistry.g.cs");
    Assert.Contains("global::Common.IDevice GetGlobalDevice();", generatedInterface);
}

static void RelativeNameResolutionPrefersNearerScopes()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/relative.idl", """
namespace Common {
    abstract interface Device {
        string describe();
    }
}

namespace Local.Common {
    abstract interface Device {
        string local_describe();
    }
}

namespace Local.Controllers {
    interface Registry {
        Common.Device get_local_device();
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedInterface = ReadGenerated(project.Root, "Generated", "Local", "Controllers", "IRegistry.g.cs");
    Assert.Contains("global::Local.Common.IDevice GetLocalDevice();", generatedInterface);
}

static void AmbiguousTypeReferencesFailCleanly()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/one.idl", """
namespace Demo {
    interface Device;
}
""");
    WriteContract(project.Root, "contracts/two.idl", """
namespace Demo {
    interface Device;
}
""");
    WriteContract(project.Root, "contracts/registry.idl", """
namespace Demo {
    interface Registry {
        Device get_device();
    }
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON100"), "Expected duplicate declaration diagnostic for ambiguous type reference.");
}

static void InitUsesRegisteredDefaultTargetEmitters()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace([new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src", includeByDefault: true)]);
    var projectFile = Path.Combine(root, "Notes.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    var projectJson = JsonNode.Parse(File.ReadAllText(projectFile))?.AsObject()
        ?? throw new InvalidOperationException("Expected project file JSON.");
    Assert.Equal("notes-src", projectJson["targets"]?["notes"]?["output"]?.GetValue<string>());
    Assert.True(File.Exists(Path.Combine(root, ".crimson", "notes", "notes.marker")));
}

static void WorkspaceBuildsArbitraryTargetEmitters()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace([new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src", includeByDefault: true)]);
    var projectFile = Path.Combine(root, "Notes.crimsonproj");
    workspace.InitProject(projectFile, starter: false);
    WriteContract(root, "contracts/notes.idl", """
namespace Demo {
    interface Notebook {
        string title;
    }
}
""");

    var result = workspace.Build(projectFile);
    Assert.Equal(0, result.Conflicts.Count);
    Assert.True(File.Exists(Path.Combine(root, "notes-src", "Runtime", "summary.txt")));
    Assert.True(File.Exists(Path.Combine(root, "notes-src", "Hooks", "Notebook.hooks.txt")));
    Assert.True(File.Exists(Path.Combine(root, ".merge", "current", "targets", "notes", "Runtime", "summary.txt")));
    Assert.True(File.Exists(Path.Combine(root, ".merge", "current", "targets", "notes", "Hooks", "Notebook.hooks.txt")));
}

static void WorkspaceBuildsMultipleConfiguredTargets()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace([
        new CSharpTargetEmitter(),
        new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src", includeByDefault: false),
    ]);
    var projectFile = Path.Combine(root, "Test.crimsonproj");
    workspace.InitProject(projectFile, starter: false);

    var projectJson = JsonNode.Parse(File.ReadAllText(projectFile))?.AsObject()
        ?? throw new InvalidOperationException("Expected project file JSON.");
    projectJson["targets"]!["notes"] = JsonNode.Parse("""{ "output": "notes-src" }""");
    File.WriteAllText(projectFile, projectJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    WriteContract(root, "contracts/device.idl", """
namespace Demo {
    interface Device {
        string name;
    }
}
""");

    var result = workspace.Build(projectFile);
    Assert.Equal(0, result.Conflicts.Count);
    Assert.True(File.Exists(Path.Combine(root, "src", "Generated", "Demo", "Device.g.cs")));
    Assert.True(File.Exists(Path.Combine(root, "notes-src", "Runtime", "summary.txt")));
}

static string CreateTempIdl(string content)
{
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.idl");
    File.WriteAllText(path, content);
    return path;
}

static TestProject CreateTempProject()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Test.crimsonproj");
    workspace.InitProject(projectFile, starter: false);
    return new TestProject(root, projectFile, workspace);
}

static void WriteContract(string root, string relativePath, string content)
{
    var fullPath = Path.Combine(root, relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, content);
}

static string ReadGenerated(string root, params string[] segments)
{
    var path = Path.Combine([root, ".merge", "current", "targets", "csharp", .. segments]);
    return File.ReadAllText(path);
}

static int CountOccurrences(string text, string value)
{
    var count = 0;
    var start = 0;
    while ((start = text.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
    {
        count++;
        start += value.Length;
    }

    return count;
}

static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}' but found '{actual}'.");
        }
    }

    public static void Contains(string expectedSubstring, string actual)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected to find '{expectedSubstring}' in output.");
        }
    }

    public static void DoesNotContain(string expectedSubstring, string actual)
    {
        if (actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Did not expect to find '{expectedSubstring}' in output.");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be false.");
        }
    }

    public static T Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected exception of type {typeof(T).Name}.");
    }

    public static T IsType<T>(object value)
    {
        if (value is not T typed)
        {
            throw new InvalidOperationException($"Expected type {typeof(T).Name} but found {value.GetType().Name}.");
        }

        return typed;
    }
}

sealed record TestProject(string Root, string ProjectFile, CrimsonWorkspace Workspace);

sealed class FakeTargetEmitter(string targetName, string defaultOutputRoot, bool includeByDefault) : ITargetEmitter
{
    public string TargetName => targetName;

    public object? GetDefaultProjectOptions() => includeByDefault
        ? new { output = defaultOutputRoot }
        : null;

    public string ResolveOutputRoot(JsonElement configuration) =>
        configuration.TryGetProperty("output", out var output)
            ? output.GetString() ?? defaultOutputRoot
            : defaultOutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
    [
        new TargetOutputDescriptor("Runtime", "Runtime", TargetMergeMode.PreferGenerated),
        new TargetOutputDescriptor("Hooks", "Hooks", TargetMergeMode.ThreeWay),
    ];

    public void PrepareProject(string projectDirectory, JsonElement configuration)
    {
        var markerDirectory = Path.Combine(projectDirectory, ".crimson", targetName);
        Directory.CreateDirectory(markerDirectory);
        File.WriteAllText(Path.Combine(markerDirectory, $"{targetName}.marker"), ResolveOutputRoot(configuration));
    }

    public void ValidateTarget(CompilationSetModel compilation, JsonElement configuration)
    {
    }

    public IReadOnlyList<EmittedTargetOutput> Emit(CompilationSetModel compilation, JsonElement configuration)
    {
        var interfaces = compilation.Files
            .SelectMany(file => EnumerateDeclarations(file.Declarations))
            .OfType<InterfaceDeclaration>()
            .Select(static declaration => declaration.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        return
        [
            new EmittedTargetOutput("Runtime", [
                new GeneratedFile("summary.txt", string.Join(",", interfaces)),
            ]),
            new EmittedTargetOutput("Hooks", interfaces.Select(static name => new GeneratedFile($"{name}.hooks.txt", $"hook:{name}")).ToArray()),
        ];
    }

    private static IEnumerable<Declaration> EnumerateDeclarations(IEnumerable<Declaration> declarations)
    {
        foreach (var declaration in declarations)
        {
            yield return declaration;

            switch (declaration)
            {
                case NamespaceDeclaration namespaceDeclaration:
                    foreach (var nested in EnumerateDeclarations(namespaceDeclaration.Members))
                    {
                        yield return nested;
                    }

                    break;

                case InterfaceDeclaration interfaceDeclaration:
                    foreach (var nested in EnumerateDeclarations(interfaceDeclaration.NestedDeclarations))
                    {
                        yield return nested;
                    }

                    break;
            }
        }
    }
}
