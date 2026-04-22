using System.Text.Json;
using System.Text.Json.Nodes;
using Crimson.Core;
using Crimson.Core.Generation;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Generation.Cpp;
using Crimson.Core.Host;
using Crimson.Core.Model;
using Crimson.Core.Projects;

var tests = new (string Name, Action Body)[]
{
    ("Parse interface with docs and members", ParseInterfaceWithDocs),
    ("Emit CSharp files for interface", EmitCSharpFiles),
    ("Emit Cpp files for interface", EmitCppFiles),
    ("Validate project catches unresolved types", ValidateProjectCatchesUnresolvedTypes),
    ("Constants require explicit values", ConstantsRequireExplicitValues),
    ("Split namespaces across files validate and generate", SplitNamespacesAcrossFilesValidateAndGenerate),
    ("Interface composition cycle fails with CRIMSON111", InterfaceCompositionCycleFails),
    ("Multi-level composition emits inherited members", MultiLevelCompositionEmitsInheritedMembers),
    ("Multi-path composition deduplicates same origin members", MultiPathCompositionDeduplicatesSameOriginMembers),
    ("Inherited member collision from different origins errors", InheritedMemberCollisionErrors),
    ("Interface typed parameters returns and containers lower to IName", InterfaceTypedParametersReturnsAndContainersLowerToIName),
    ("Interface typed parameters returns and containers lower to Cpp contracts", InterfaceTypedParametersReturnsAndContainersLowerToCppContracts),
    ("Nullable types emit nullable CSharp", NullableTypesEmitNullableCSharp),
    ("Nullable types emit optional and shared_ptr Cpp", NullableTypesEmitNullableCpp),
    ("Enum defaults resolve in generated code", EnumDefaultsResolveInGeneratedCode),
    ("Structs lower to concrete types", StructsLowerToConcreteTypes),
    ("Structs reject internal members", StructsRejectInternalMembers),
    ("Cpp raw pointer handle style updates support header", CppRawPointerHandleStyleUpdatesSupportHeader),
    ("Abstract interfaces emit only interface projections", AbstractInterfacesEmitOnlyInterfaceProjection),
    ("Abstract interfaces emit only interface projections for Cpp", AbstractInterfacesEmitOnlyInterfaceProjectionCpp),
    ("Nested type resolution through a base contract works", NestedTypeResolutionThroughBaseContractWorks),
    ("Global dot name resolution works", GlobalDotNameResolutionWorks),
    ("Relative name resolution prefers nearer scopes", RelativeNameResolutionPrefersNearerScopes),
    ("Ambiguous type references fail cleanly", AmbiguousTypeReferencesFailCleanly),
    ("Cpp target rejects enums with associated values", CppTargetRejectsEnumsWithAssociatedValues),
    ("Workspace lists registered init profiles", WorkspaceListsRegisteredInitProfiles),
    ("Init uses selected init profile", InitUsesSelectedInitProfile),
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

static void EmitCppFiles()
{
    var emitter = new CppEmitter();
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
                        null,
                        false,
                        [],
                        [
                            new ValueMemberDeclaration("name", [], null, false, false, new PrimitiveTypeReference("string", false, null), null, null),
                            new MethodMemberDeclaration("get_customer", [], null, new PrimitiveTypeReference("string", false, null), [
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
    Assert.True(result.GeneratedHeaders.Any(x => x.RelativePath.EndsWith("ICustomerService.g.hpp", StringComparison.Ordinal)));
    Assert.True(result.GeneratedHeaders.Any(x => x.RelativePath.EndsWith("CustomerService.g.hpp", StringComparison.Ordinal)));
    Assert.True(result.GeneratedSources.Any(x => x.RelativePath.EndsWith("CustomerService.g.cpp", StringComparison.Ordinal)));
    Assert.True(result.UserHeaders.Any(x => x.RelativePath.EndsWith("CustomerService.hpp", StringComparison.Ordinal)));
    Assert.True(result.UserSources.Any(x => x.RelativePath.EndsWith("CustomerService.cpp", StringComparison.Ordinal)));

    var generatedHeader = result.GeneratedHeaders.Single(x => string.Equals(Path.GetFileName(x.RelativePath), "CustomerService.g.hpp", StringComparison.Ordinal));
    var userSource = result.UserSources.Single(x => string.Equals(Path.GetFileName(x.RelativePath), "CustomerService.cpp", StringComparison.Ordinal));
    Assert.Contains("class CustomerServiceGenerated : public ICustomerService", generatedHeader.Content);
    Assert.Contains("::Crimson::Cpp::String GetName() const override;", generatedHeader.Content);
    Assert.Contains("throw std::runtime_error(\"Not implemented\");", userSource.Content);
}

static void ValidateProjectCatchesUnresolvedTypes()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Broken.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: false);
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
    workspace.InitProject(projectFile, "csharp", starter: false);

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

static void InterfaceTypedParametersReturnsAndContainersLowerToCppContracts()
{
    var project = CreateTempProject("cpp-cmake");
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

    var generatedInterface = ReadGeneratedCpp(project.Root, "GeneratedHeaders", "Demo", "IRegistry.g.hpp");
    Assert.Contains("::Crimson::Cpp::InterfaceHandle<IDevice> GetDevice(::Crimson::Cpp::InterfaceHandle<IDevice> device, ::Crimson::Cpp::List<::Crimson::Cpp::InterfaceHandle<IDevice>> devices, ::Crimson::Cpp::Set<::Crimson::Cpp::InterfaceHandle<IDevice>> deviceSet, ::Crimson::Cpp::Map<::Crimson::Cpp::String, ::Crimson::Cpp::InterfaceHandle<IDevice>> deviceMap) = 0;", generatedInterface);
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

static void NullableTypesEmitNullableCpp()
{
    var project = CreateTempProject("cpp-cmake");
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

    var generatedInterface = ReadGeneratedCpp(project.Root, "GeneratedHeaders", "Demo", "IRegistry.g.hpp");
    Assert.Contains("virtual ::Crimson::Cpp::Optional<::Crimson::Cpp::String> GetDisplayName() const = 0;", generatedInterface);
    Assert.Contains("virtual ::Crimson::Cpp::Optional<::Crimson::Cpp::List<::Crimson::Cpp::Optional<::Crimson::Cpp::String>>> GetAliases() const = 0;", generatedInterface);
    Assert.Contains("virtual ::Crimson::Cpp::InterfaceHandle<IDevice> GetPrimaryDevice() const = 0;", generatedInterface);
    Assert.Contains("virtual ::Crimson::Cpp::Optional<::Crimson::Cpp::Map<::Crimson::Cpp::String, ::Crimson::Cpp::InterfaceHandle<IDevice>>> GetDeviceMap() const = 0;", generatedInterface);
    Assert.Contains("virtual ::Crimson::Cpp::Optional<Mode> GetActiveMode() const = 0;", generatedInterface);
}

static void EnumDefaultsResolveInGeneratedCode()
{
    var project = CreateTempProject("cpp-cmake");
    WriteContract(project.Root, "contracts/defaults.idl", """
namespace SmartHome {
    enum SceneMode {
        Home,
        Away,
    }

    interface DemoHomeRuntime {
        SceneMode active_mode = Home;
        SceneMode target_mode = SceneMode.Away;
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedCpp = ReadGeneratedCpp(project.Root, "GeneratedHeaders", "SmartHome", "DemoHomeRuntime.g.hpp");
    Assert.Contains("SceneMode activeMode_ = SceneMode::Home;", generatedCpp);
    Assert.Contains("SceneMode targetMode_ = SceneMode::Away;", generatedCpp);

    var generatedCsharpProject = CreateTempProject();
    WriteContract(generatedCsharpProject.Root, "contracts/defaults.idl", """
namespace SmartHome {
    enum SceneMode {
        Home,
        Away,
    }

    interface DemoHomeRuntime {
        SceneMode active_mode = Home;
        SceneMode target_mode = SceneMode.Away;
    }
}
""");

    generatedCsharpProject.Workspace.Generate(generatedCsharpProject.ProjectFile);
    var generatedCsharp = ReadGenerated(generatedCsharpProject.Root, "Generated", "SmartHome", "DemoHomeRuntime.g.cs");
    Assert.Contains("private SceneMode _activeMode = SceneMode.Home;", generatedCsharp);
    Assert.Contains("private SceneMode _targetMode = SceneMode.Away;", generatedCsharp);
}

static void StructsLowerToConcreteTypes()
{
    var project = CreateTempProject("cpp-cmake");
    WriteContract(project.Root, "contracts/value.idl", """
namespace SmartHome {
    struct DeviceSnapshot {
        string display_name;
    }

    interface DeviceRegistry {
        DeviceSnapshot latest;
        list<DeviceSnapshot> history;
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    var generatedCpp = ReadGeneratedCpp(project.Root, "GeneratedHeaders", "SmartHome", "IDeviceRegistry.g.hpp");
    Assert.Contains("virtual DeviceSnapshot GetLatest() const = 0;", generatedCpp);
    Assert.Contains("virtual ::Crimson::Cpp::List<DeviceSnapshot> GetHistory() const = 0;", generatedCpp);
    Assert.True(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "cpp", "GeneratedHeaders", "SmartHome", "DeviceSnapshot.hpp")));

    var csharpProject = CreateTempProject();
    WriteContract(csharpProject.Root, "contracts/value.idl", """
namespace SmartHome {
    struct DeviceSnapshot {
        string display_name;
    }

    interface DeviceRegistry {
        DeviceSnapshot latest;
        list<DeviceSnapshot> history;
    }
}
""");

    csharpProject.Workspace.Generate(csharpProject.ProjectFile);

    var generatedCsharp = ReadGenerated(csharpProject.Root, "Generated", "SmartHome", "IDeviceRegistry.g.cs");
    Assert.Contains("DeviceSnapshot Latest { get; set; }", generatedCsharp);
    Assert.Contains("List<DeviceSnapshot> History { get; set; }", generatedCsharp);
    Assert.True(File.Exists(Path.Combine(csharpProject.Root, ".merge", "current", "targets", "csharp", "Generated", "SmartHome", "DeviceSnapshot.g.cs")));
}

static void StructsRejectInternalMembers()
{
    var project = CreateTempProject();
    WriteContract(project.Root, "contracts/value.idl", """
namespace Demo {
    struct Snapshot {
        internal string label;
    }
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON117"), "Expected struct member diagnostic.");
}

static void CppRawPointerHandleStyleUpdatesSupportHeader()
{
    var emitter = new CppEmitter();
    var compilation = new CompilationSetModel([
        new CompilationUnitModel("test.idl", [
            new NamespaceDeclaration(
                "Demo",
                [],
                [],
                [],
                null,
                [
                    new InterfaceDeclaration(
                        "Device",
                        ["Demo"],
                        [],
                        [],
                        null,
                        true,
                        [],
                        [],
                        [],
                        null)
                ],
                null)
        ])
    ]);

    var result = emitter.Emit(compilation, new CppTargetOptions("cpp", CppInterfaceHandleStyle.RawPtr));
    var supportHeader = result.GeneratedHeaders.Single(x => string.Equals(x.RelativePath, "Crimson/Cpp/Support.g.hpp", StringComparison.Ordinal));
    Assert.Contains("using InterfaceHandle = T*;", supportHeader.Content);
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

static void AbstractInterfacesEmitOnlyInterfaceProjectionCpp()
{
    var project = CreateTempProject("cpp-cmake");
    WriteContract(project.Root, "contracts/device.idl", """
namespace Demo {
    abstract interface Device {
        string describe();
    }
}
""");

    project.Workspace.Generate(project.ProjectFile);

    Assert.True(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "cpp", "GeneratedHeaders", "Demo", "IDevice.g.hpp")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "cpp", "GeneratedHeaders", "Demo", "Device.g.hpp")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "cpp", "UserHeaders", "Demo", "Device.hpp")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".merge", "current", "targets", "cpp", "UserSources", "Demo", "Device.cpp")));
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

static void CppTargetRejectsEnumsWithAssociatedValues()
{
    var project = CreateTempProject("cpp-cmake");
    WriteContract(project.Root, "contracts/event.idl", """
namespace Demo {
    enum Event : string {
        Created = "created",
    }
}
""");

    var exception = Assert.Throws<DiagnosticException>(() => project.Workspace.ValidateProject(project.ProjectFile));
    Assert.True(exception.Diagnostics.Any(x => x.Code == "CRIMSON201"), "Expected cpp target associated enum value diagnostic.");
}

static void WorkspaceListsRegisteredInitProfiles()
{
    var workspace = new CrimsonWorkspace(
        [new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src")],
        [new FakeHostIntegration("notes-host")],
        [new FakeProjectInitProfile("notes", "Notes Profile", "Notes runtime test profile", "notes", "notes-src", "notes-host")]);

    var profiles = workspace.GetInitProfiles();
    Assert.Equal(1, profiles.Count);
    Assert.Equal("notes", profiles[0].ProfileId);
    Assert.Equal("Notes Profile", profiles[0].DisplayName);
}

static void InitUsesSelectedInitProfile()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace(
        [new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src")],
        [new FakeHostIntegration("notes-host")],
        [new FakeProjectInitProfile("notes", "Notes Profile", "Notes runtime test profile", "notes", "notes-src", "notes-host")]);
    var projectFile = Path.Combine(root, "Notes.crimsonproj");
    workspace.InitProject(projectFile, "notes", starter: true);

    var projectJson = JsonNode.Parse(File.ReadAllText(projectFile))?.AsObject()
        ?? throw new InvalidOperationException("Expected project file JSON.");
    Assert.Equal("notes-src", projectJson["targets"]?["notes"]?["output"]?.GetValue<string>());
    Assert.Equal("notes-host", projectJson["host"]?["kind"]?.GetValue<string>());
    Assert.True(File.Exists(Path.Combine(root, ".crimson", "notes-host", "notes-host.marker")));
    Assert.True(File.Exists(Path.Combine(root, "contracts", "notes.idl")));
    Assert.Contains("notes-build/", File.ReadAllText(Path.Combine(root, ".gitignore")));
}

static void WorkspaceBuildsArbitraryTargetEmitters()
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace(
        [new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src")],
        [new FakeHostIntegration("notes-host")],
        [new FakeProjectInitProfile("notes", "Notes Profile", "Notes runtime test profile", "notes", "notes-src", "notes-host")]);
    var projectFile = Path.Combine(root, "Notes.crimsonproj");
    workspace.InitProject(projectFile, "notes", starter: false);
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
        new FakeTargetEmitter("notes", defaultOutputRoot: "notes-src"),
    ], [
        new DotNetMsbuildHostIntegration(),
        new FakeHostIntegration("notes-host"),
    ], [
        new CSharpProjectInitProfile(),
        new FakeProjectInitProfile("notes", "Notes Profile", "Notes runtime test profile", "notes", "notes-src", "notes-host"),
    ]);
    var projectFile = Path.Combine(root, "Test.crimsonproj");
    workspace.InitProject(projectFile, "csharp", starter: false);

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

static TestProject CreateTempProject(string profileId = "csharp")
{
    var root = Path.Combine(Path.GetTempPath(), $"crimson-unit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);

    var workspace = new CrimsonWorkspace();
    var projectFile = Path.Combine(root, "Test.crimsonproj");
    workspace.InitProject(projectFile, profileId, starter: false);
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

static string ReadGeneratedCpp(string root, params string[] segments)
{
    var path = Path.Combine([root, ".merge", "current", "targets", "cpp", .. segments]);
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

sealed class FakeProjectInitProfile(
    string profileId,
    string displayName,
    string description,
    string targetName,
    string outputRoot,
    string hostName) : IProjectInitProfile
{
    public string ProfileId => profileId;

    public string DisplayName => displayName;

    public string Description => description;

    public ProjectInitPlan CreatePlan(ProjectInitContext context) =>
        new(
            ["contracts/**/*.idl"],
            Array.Empty<string>(),
            [new ProjectInitTarget(targetName, new { output = outputRoot })],
            new ProjectInitHost(hostName, new { scratchDirectory = "notes-build" }),
            context.Starter
                ? [new ProjectInitFile(Path.Combine("contracts", $"{profileId}.idl"), "namespace Demo { interface Notes; }" + Environment.NewLine)]
                : Array.Empty<ProjectInitFile>());
}

sealed class FakeTargetEmitter(string targetName, string defaultOutputRoot) : ITargetEmitter
{
    public string TargetName => targetName;

    public string ResolveOutputRoot(JsonElement configuration) =>
        configuration.TryGetProperty("output", out var output)
            ? output.GetString() ?? defaultOutputRoot
            : defaultOutputRoot;

    public IReadOnlyList<TargetOutputDescriptor> DescribeOutputs(JsonElement configuration) =>
    [
        new TargetOutputDescriptor("Runtime", "Runtime", TargetMergeMode.PreferGenerated, TargetOutputContentType.SourceFiles, TargetOutputOwnership.Generated),
        new TargetOutputDescriptor("Hooks", "Hooks", TargetMergeMode.ThreeWay, TargetOutputContentType.SourceFiles, TargetOutputOwnership.UserOwned),
    ];

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

sealed class FakeHostIntegration(string hostName) : IHostIntegration
{
    public string HostName => hostName;

    public IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration) =>
        configuration.TryGetProperty("scratchDirectory", out var scratchDirectory)
            ? [scratchDirectory.GetString() + "/"]
            : Array.Empty<string>();

    public void ValidateHost(string projectFilePath, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        if (!targets.Any(static target => string.Equals(target.TargetName, "notes", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Host integration '{hostName}' requires a 'notes' target.");
        }
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        var markerDirectory = Path.Combine(projectDirectory, ".crimson", hostName);
        Directory.CreateDirectory(markerDirectory);
        File.WriteAllText(Path.Combine(markerDirectory, $"{hostName}.marker"), targets.Single(static target => string.Equals(target.TargetName, "notes", StringComparison.OrdinalIgnoreCase)).OutputRoot);
    }
}
