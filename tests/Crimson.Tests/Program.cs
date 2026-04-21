using System.Text.Json;
using Crimson.Core;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Model;

var tests = new (string Name, Action Body)[]
{
    ("Parse interface with docs and members", ParseInterfaceWithDocs),
    ("Emit CSharp files for interface", EmitCSharpFiles),
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
    Assert.True(result.GeneratedFiles.Any(x => x.RelativePath.EndsWith("CustomerService.g.cs", StringComparison.Ordinal)));
    Assert.True(result.UserFiles.Any(x => x.RelativePath.EndsWith("CustomerService.cs", StringComparison.Ordinal)));
}

static string CreateTempIdl(string content)
{
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.idl");
    File.WriteAllText(path, content);
    return path;
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

    public static T IsType<T>(object value)
    {
        if (value is not T typed)
        {
            throw new InvalidOperationException($"Expected type {typeof(T).Name} but found {value.GetType().Name}.");
        }

        return typed;
    }
}
