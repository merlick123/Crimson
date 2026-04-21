using System.Text.Json;
using Crimson.Core;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Model;

var tests = new (string Name, Action Body)[]
{
    ("Parse interface with docs and members", ParseInterfaceWithDocs),
    ("Emit CSharp files for interface", EmitCSharpFiles),
    ("Validate project catches unresolved types", ValidateProjectCatchesUnresolvedTypes),
    ("Split namespaces across files validate and generate", SplitNamespacesAcrossFilesValidateAndGenerate),
    ("Interface composition cycle fails with CRIMSON111", InterfaceCompositionCycleFails),
    ("Multi-level composition emits inherited members", MultiLevelCompositionEmitsInheritedMembers),
    ("Multi-path composition deduplicates same origin members", MultiPathCompositionDeduplicatesSameOriginMembers),
    ("Inherited member collision from different origins errors", InheritedMemberCollisionErrors),
    ("Interface typed parameters returns and containers lower to IName", InterfaceTypedParametersReturnsAndContainersLowerToIName),
    ("Abstract interfaces emit only interface projections", AbstractInterfacesEmitOnlyInterfaceProjection),
    ("Nested type resolution through a base contract works", NestedTypeResolutionThroughBaseContractWorks),
    ("Global dot name resolution works", GlobalDotNameResolutionWorks),
    ("Relative name resolution prefers nearer scopes", RelativeNameResolutionPrefersNearerScopes),
    ("Ambiguous type references fail cleanly", AmbiguousTypeReferencesFailCleanly),
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

    var generatedInterface = Path.Combine(root, ".crimson", "raw-current", "Generated", "SmartHome", "IDemoCamera.g.cs");
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

    Assert.True(File.Exists(Path.Combine(project.Root, ".crimson", "raw-current", "Generated", "Demo", "IDevice.g.cs")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".crimson", "raw-current", "Generated", "Demo", "Device.g.cs")));
    Assert.False(File.Exists(Path.Combine(project.Root, ".crimson", "raw-current", "User", "Demo", "Device.cs")));
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
    var path = Path.Combine([root, ".crimson", "raw-current", .. segments]);
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
