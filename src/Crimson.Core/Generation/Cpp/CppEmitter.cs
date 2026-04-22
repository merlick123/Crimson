using System.Globalization;
using System.Text;
using Crimson.Core.Model;
using Crimson.Core.Utility;

namespace Crimson.Core.Generation.Cpp;

public sealed record GeneratedCppTargetTree(
    IReadOnlyList<GeneratedFile> GeneratedHeaders,
    IReadOnlyList<GeneratedFile> GeneratedSources,
    IReadOnlyList<GeneratedFile> UserHeaders,
    IReadOnlyList<GeneratedFile> UserSources);

public sealed class CppEmitter
{
    private Dictionary<string, Declaration> _declarationsByQualifiedName = new(StringComparer.Ordinal);

    public void ValidateTargetSupport(CompilationSetModel compilation)
    {
        var diagnostics = EnumerateDeclarations(compilation.Files.SelectMany(static file => file.Declarations))
            .OfType<EnumDeclaration>()
            .Where(static declaration => declaration.AssociatedValueType is not null || declaration.Members.Any(static member => member.AssociatedValue is not null))
            .Select(static declaration => new Diagnostic(
                "CRIMSON201",
                $"The 'cpp' target does not support enums with associated values yet: '{declaration.QualifiedName}'.",
                "error",
                declaration.Source))
            .ToArray();

        if (diagnostics.Length > 0)
        {
            throw new DiagnosticException(diagnostics);
        }
    }

    public GeneratedCppTargetTree Emit(CompilationSetModel compilation)
    {
        _declarationsByQualifiedName = compilation.Files
            .SelectMany(static file => EnumerateDeclarations(file.Declarations))
            .Where(static declaration => declaration is not NamespaceDeclaration)
            .GroupBy(static declaration => declaration.QualifiedName, StringComparer.Ordinal)
            .ToDictionary(static declarations => declarations.Key, static declarations => declarations.First(), StringComparer.Ordinal);

        var generatedHeaders = new List<GeneratedFile>();
        var generatedSources = new List<GeneratedFile>();
        var userHeaders = new List<GeneratedFile>();
        var userSources = new List<GeneratedFile>();

        foreach (var declaration in compilation.Files.SelectMany(static file => file.Declarations))
        {
            EmitDeclaration(declaration, generatedHeaders, generatedSources, userHeaders, userSources);
        }

        return new GeneratedCppTargetTree(generatedHeaders, generatedSources, userHeaders, userSources);
    }

    private void EmitDeclaration(
        Declaration declaration,
        List<GeneratedFile> generatedHeaders,
        List<GeneratedFile> generatedSources,
        List<GeneratedFile> userHeaders,
        List<GeneratedFile> userSources)
    {
        switch (declaration)
        {
            case NamespaceDeclaration namespaceDeclaration:
                foreach (var member in namespaceDeclaration.Members)
                {
                    EmitDeclaration(member, generatedHeaders, generatedSources, userHeaders, userSources);
                }

                break;

            case InterfaceDeclaration interfaceDeclaration:
                EmitInterface(interfaceDeclaration, generatedHeaders, generatedSources, userHeaders, userSources);
                foreach (var nested in interfaceDeclaration.NestedDeclarations)
                {
                    EmitDeclaration(nested, generatedHeaders, generatedSources, userHeaders, userSources);
                }

                break;

            case EnumDeclaration enumDeclaration:
                generatedHeaders.Add(new GeneratedFile(GetDeclarationPath(enumDeclaration, $"{enumDeclaration.Name}.g.hpp"), RenderEnumHeader(enumDeclaration)));
                break;

            case ConstantDeclaration constantDeclaration:
                generatedHeaders.Add(new GeneratedFile(GetDeclarationPath(constantDeclaration, $"{constantDeclaration.Name}.g.hpp"), RenderConstantHeader(constantDeclaration)));
                break;
        }
    }

    private void EmitInterface(
        InterfaceDeclaration declaration,
        List<GeneratedFile> generatedHeaders,
        List<GeneratedFile> generatedSources,
        List<GeneratedFile> userHeaders,
        List<GeneratedFile> userSources)
    {
        generatedHeaders.Add(new GeneratedFile(GetDeclarationPath(declaration, $"I{declaration.Name}.g.hpp"), RenderContractInterfaceHeader(declaration)));

        if (!declaration.IsAbstract)
        {
            generatedHeaders.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.g.hpp"), RenderGeneratedClassHeader(declaration)));
            generatedSources.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.g.cpp"), RenderGeneratedClassSource(declaration)));
            userHeaders.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.hpp"), RenderUserClassHeader(declaration)));
            userSources.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.cpp"), RenderUserClassSource(declaration)));
        }
    }

    private static string GetDeclarationPath(Declaration declaration, string fileName)
    {
        var segments = declaration.NamespacePath.Concat(declaration.ContainingTypes).ToArray();
        return segments.Length == 0
            ? fileName
            : Path.Combine(Path.Combine(segments), fileName);
    }

    private string RenderContractInterfaceHeader(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var includes = CollectNamedIncludes(
            declaration.BaseContracts.Concat(GetEffectiveMembers(declaration).SelectMany(GetMemberTypes)),
            declaration,
            declaration);

        AppendHeaderPreamble(builder, includes);
        AppendNamespaceOpen(builder, declaration.NamespacePath);

        var baseNames = declaration.BaseContracts.Count == 0
            ? string.Empty
            : " : " + string.Join(", ", declaration.BaseContracts.Select(baseContract => $"public {RenderContractType(baseContract, declaration)}"));

        builder.AppendLine($"class I{declaration.Name}{baseNames}");
        builder.AppendLine("{");
        builder.AppendLine("public:");
        builder.AppendLine($"    virtual ~I{declaration.Name}() = default;");
        builder.AppendLine();

        foreach (var constantMember in declaration.Members.OfType<ConstantMemberDeclaration>())
        {
            builder.AppendLine($"    inline static constexpr {RenderConstantType(constantMember.Type, declaration)} {ToPascalCase(constantMember.Name)} = {RenderLiteral(constantMember.Value ?? throw new InvalidOperationException($"Constant member '{declaration.QualifiedName}.{constantMember.Name}' must declare a value."))};");
        }

        if (declaration.Members.OfType<ConstantMemberDeclaration>().Any())
        {
            builder.AppendLine();
        }

        foreach (var member in declaration.Members)
        {
            switch (member)
            {
                case ValueMemberDeclaration valueMember when !valueMember.IsInternal:
                    builder.AppendLine($"    virtual {RenderContractType(valueMember.Type, declaration)} Get{ToPascalCase(valueMember.Name)}() const = 0;");
                    if (!valueMember.IsReadonly)
                    {
                        builder.AppendLine($"    virtual void Set{ToPascalCase(valueMember.Name)}({RenderContractType(valueMember.Type, declaration)} value) = 0;");
                    }

                    builder.AppendLine();
                    break;

                case MethodMemberDeclaration methodMember:
                    builder.AppendLine($"    virtual {RenderContractType(methodMember.ReturnType, declaration)} {ToPascalCase(methodMember.Name)}({RenderParameters(methodMember.Parameters, declaration, includeDefaults: true)}) = 0;");
                    builder.AppendLine();
                    break;
            }
        }

        builder.AppendLine("};");
        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderGeneratedClassHeader(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendHeaderPreamble(builder, [PathHelpers.NormalizeRelativePath(GetDeclarationPath(declaration, $"I{declaration.Name}.g.hpp"))]);
        AppendNamespaceOpen(builder, declaration.NamespacePath);

        builder.AppendLine($"class {declaration.Name}Generated : public I{declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine("public:");
        builder.AppendLine($"    {declaration.Name}Generated() = default;");
        builder.AppendLine($"    ~{declaration.Name}Generated() override = default;");

        var effectiveValues = GetEffectiveMembers(declaration).OfType<ValueMemberDeclaration>().ToArray();
        if (effectiveValues.Length > 0)
        {
            builder.AppendLine();
        }

        foreach (var valueMember in effectiveValues.Where(static member => !member.IsInternal))
        {
            builder.AppendLine($"    {RenderContractType(valueMember.Type, declaration)} Get{ToPascalCase(valueMember.Name)}() const override;");
            if (!valueMember.IsReadonly)
            {
                builder.AppendLine($"    void Set{ToPascalCase(valueMember.Name)}({RenderContractType(valueMember.Type, declaration)} value) override;");
            }

            builder.AppendLine();
        }

        if (effectiveValues.Any(static member => member.IsInternal))
        {
            builder.AppendLine("protected:");
            foreach (var valueMember in effectiveValues.Where(static member => member.IsInternal))
            {
                builder.AppendLine($"    {RenderContractType(valueMember.Type, declaration)} Get{ToPascalCase(valueMember.Name)}() const;");
                builder.AppendLine($"    void Set{ToPascalCase(valueMember.Name)}({RenderContractType(valueMember.Type, declaration)} value);");
                builder.AppendLine();
            }
        }

        builder.AppendLine("private:");
        foreach (var valueMember in effectiveValues)
        {
            builder.AppendLine($"    {RenderContractType(valueMember.Type, declaration)} {ToCamelCase(valueMember.Name)}_ = {RenderDefaultValue(valueMember.Type, valueMember.DefaultValue, declaration)};");
        }

        if (effectiveValues.Length == 0)
        {
            builder.AppendLine("    // No generated state.");
        }

        builder.AppendLine("};");
        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderGeneratedClassSource(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"#include \"{PathHelpers.NormalizeRelativePath(GetDeclarationPath(declaration, $"{declaration.Name}.g.hpp"))}\"");
        builder.AppendLine();

        AppendNamespaceOpen(builder, declaration.NamespacePath);

        foreach (var valueMember in GetEffectiveMembers(declaration).OfType<ValueMemberDeclaration>())
        {
            builder.AppendLine($"{RenderContractType(valueMember.Type, declaration)} {declaration.Name}Generated::Get{ToPascalCase(valueMember.Name)}() const");
            builder.AppendLine("{");
            builder.AppendLine($"    return {ToCamelCase(valueMember.Name)}_;");
            builder.AppendLine("}");
            builder.AppendLine();

            if (!valueMember.IsReadonly || valueMember.IsInternal)
            {
                builder.AppendLine($"void {declaration.Name}Generated::Set{ToPascalCase(valueMember.Name)}({RenderContractType(valueMember.Type, declaration)} value)");
                builder.AppendLine("{");
                builder.AppendLine($"    {ToCamelCase(valueMember.Name)}_ = value;");
                builder.AppendLine("}");
                builder.AppendLine();
            }
        }

        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderUserClassHeader(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendHeaderPreamble(builder, [PathHelpers.NormalizeRelativePath(GetDeclarationPath(declaration, $"{declaration.Name}.g.hpp"))]);
        AppendNamespaceOpen(builder, declaration.NamespacePath);

        builder.AppendLine($"class {declaration.Name} : public {declaration.Name}Generated");
        builder.AppendLine("{");
        builder.AppendLine("public:");
        builder.AppendLine($"    {declaration.Name}() = default;");
        builder.AppendLine($"    ~{declaration.Name}() override = default;");

        var methods = GetEffectiveMembers(declaration).OfType<MethodMemberDeclaration>().ToArray();
        if (methods.Length > 0)
        {
            builder.AppendLine();
            foreach (var method in methods)
            {
                builder.AppendLine($"    {RenderContractType(method.ReturnType, declaration)} {ToPascalCase(method.Name)}({RenderParameters(method.Parameters, declaration, includeDefaults: false)}) override;");
            }
        }

        builder.AppendLine("};");
        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderUserClassSource(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"#include \"{PathHelpers.NormalizeRelativePath(GetDeclarationPath(declaration, $"{declaration.Name}.hpp"))}\"");
        builder.AppendLine();
        builder.AppendLine("#include <stdexcept>");
        builder.AppendLine();

        AppendNamespaceOpen(builder, declaration.NamespacePath);

        foreach (var method in GetEffectiveMembers(declaration).OfType<MethodMemberDeclaration>())
        {
            builder.AppendLine($"{RenderContractType(method.ReturnType, declaration)} {declaration.Name}::{ToPascalCase(method.Name)}({RenderParameters(method.Parameters, declaration, includeDefaults: false)})");
            builder.AppendLine("{");
            builder.AppendLine("    throw std::runtime_error(\"Not implemented\");");
            builder.AppendLine("}");
            builder.AppendLine();
        }

        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderEnumHeader(EnumDeclaration declaration)
    {
        var builder = new StringBuilder();
        builder.AppendLine("#pragma once");
        builder.AppendLine();
        AppendNamespaceOpen(builder, declaration.NamespacePath);
        builder.AppendLine($"enum class {declaration.Name}");
        builder.AppendLine("{");

        for (var index = 0; index < declaration.Members.Count; index++)
        {
            var member = declaration.Members[index];
            var suffix = index == declaration.Members.Count - 1 ? string.Empty : ",";
            builder.AppendLine($"    {member.Name}{suffix}");
        }

        builder.AppendLine("};");
        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderConstantHeader(ConstantDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendHeaderPreamble(builder, Array.Empty<string>());
        AppendNamespaceOpen(builder, declaration.NamespacePath);
        builder.AppendLine($"struct {declaration.Name} final");
        builder.AppendLine("{");
        builder.AppendLine($"    inline static constexpr {RenderConstantType(declaration.Type, declaration)} Value = {RenderLiteral(declaration.Value ?? throw new InvalidOperationException($"Constant '{declaration.QualifiedName}' must declare a value."))};");
        builder.AppendLine("};");
        AppendNamespaceClose(builder, declaration.NamespacePath);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void AppendHeaderPreamble(StringBuilder builder, IEnumerable<string> includes)
    {
        builder.AppendLine("#pragma once");
        builder.AppendLine();

        foreach (var include in CommonIncludes.Concat(includes).Distinct(StringComparer.Ordinal).OrderBy(static include => include, StringComparer.Ordinal))
        {
            if (include.StartsWith("<", StringComparison.Ordinal))
            {
                builder.AppendLine($"#include {include}");
            }
            else
            {
                builder.AppendLine($"#include \"{include}\"");
            }
        }

        builder.AppendLine();
    }

    private static readonly string[] CommonIncludes =
    [
        "<array>",
        "<cstdint>",
        "<map>",
        "<memory>",
        "<optional>",
        "<set>",
        "<string>",
        "<string_view>",
        "<vector>",
    ];

    private static void AppendNamespaceOpen(StringBuilder builder, IReadOnlyList<string> namespacePath)
    {
        if (namespacePath.Count == 0)
        {
            return;
        }

        builder.AppendLine($"namespace {string.Join("::", namespacePath)}");
        builder.AppendLine("{");
        builder.AppendLine();
    }

    private static void AppendNamespaceClose(StringBuilder builder, IReadOnlyList<string> namespacePath)
    {
        if (namespacePath.Count == 0)
        {
            return;
        }

        builder.AppendLine("}");
    }

    private IReadOnlyList<string> CollectNamedIncludes(IEnumerable<TypeReference> types, Declaration scope, Declaration currentDeclaration)
    {
        var includes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            CollectNamedIncludes(type, scope, currentDeclaration, includes);
        }

        return includes.OrderBy(static include => include, StringComparer.Ordinal).ToArray();
    }

    private void CollectNamedIncludes(TypeReference type, Declaration scope, Declaration currentDeclaration, HashSet<string> includes)
    {
        switch (type)
        {
            case NamedTypeReference named:
                if (ResolveNamedDeclaration(named, scope) is { } declaration &&
                    !ReferenceEquals(declaration, currentDeclaration))
                {
                    includes.Add(GetHeaderIncludePath(declaration));
                }

                break;

            case ListTypeReference list:
                CollectNamedIncludes(list.ElementType, scope, currentDeclaration, includes);
                break;

            case SetTypeReference set:
                CollectNamedIncludes(set.ElementType, scope, currentDeclaration, includes);
                break;

            case MapTypeReference map:
                CollectNamedIncludes(map.KeyType, scope, currentDeclaration, includes);
                CollectNamedIncludes(map.ValueType, scope, currentDeclaration, includes);
                break;

            case ArrayTypeReference array:
                CollectNamedIncludes(array.ElementType, scope, currentDeclaration, includes);
                break;
        }
    }

    private static string GetHeaderIncludePath(Declaration declaration) =>
        declaration switch
        {
            InterfaceDeclaration interfaceDeclaration => PathHelpers.NormalizeRelativePath(GetDeclarationPath(interfaceDeclaration, $"I{interfaceDeclaration.Name}.g.hpp")),
            EnumDeclaration enumDeclaration => PathHelpers.NormalizeRelativePath(GetDeclarationPath(enumDeclaration, $"{enumDeclaration.Name}.g.hpp")),
            _ => PathHelpers.NormalizeRelativePath(GetDeclarationPath(declaration, $"{declaration.Name}.g.hpp")),
        };

    private static IEnumerable<TypeReference> GetMemberTypes(InterfaceMember member) =>
        member switch
        {
            ValueMemberDeclaration valueMember => [valueMember.Type],
            MethodMemberDeclaration methodMember => new[] { methodMember.ReturnType }.Concat(methodMember.Parameters.Select(static parameter => parameter.Type)),
            ConstantMemberDeclaration constantMember => [constantMember.Type],
            _ => Array.Empty<TypeReference>(),
        };

    private string RenderContractType(TypeReference type, Declaration scope) =>
        RenderType(type, scope);

    private string RenderType(TypeReference type, Declaration scope) =>
        ApplyNullability(RenderTypeCore(type, scope), type, scope);

    private string RenderTypeCore(TypeReference type, Declaration scope) =>
        type switch
        {
            VoidTypeReference => "void",
            PrimitiveTypeReference primitive => primitive.Name switch
            {
                "bool" => "bool",
                "string" => "std::string",
                "int8" => "std::int8_t",
                "uint8" => "std::uint8_t",
                "int16" => "std::int16_t",
                "uint16" => "std::uint16_t",
                "int32" => "std::int32_t",
                "uint32" => "std::uint32_t",
                "int64" => "std::int64_t",
                "uint64" => "std::uint64_t",
                "float32" => "float",
                "float64" => "double",
                _ => primitive.Name,
            },
            NamedTypeReference named => RenderNamedType(named, scope),
            ListTypeReference list => $"std::vector<{RenderType(list.ElementType, scope)}>",
            SetTypeReference set => $"std::set<{RenderType(set.ElementType, scope)}>",
            MapTypeReference map => $"std::map<{RenderType(map.KeyType, scope)}, {RenderType(map.ValueType, scope)}>",
            ArrayTypeReference array when array.Length is int length => $"std::array<{RenderType(array.ElementType, scope)}, {length}>",
            ArrayTypeReference array => $"std::vector<{RenderType(array.ElementType, scope)}>",
            _ => throw new NotSupportedException($"Unsupported type: {type.GetType().Name}"),
        };

    private string RenderConstantType(TypeReference type, Declaration scope) =>
        type switch
        {
            PrimitiveTypeReference primitive when primitive.Name == "string" => "std::string_view",
            _ => RenderTypeCore(type, scope),
        };

    private string ApplyNullability(string renderedType, TypeReference type, Declaration scope)
    {
        if (!type.IsNullable || type is VoidTypeReference)
        {
            return renderedType;
        }

        if (type is NamedTypeReference named && ResolveNamedDeclaration(named, scope) is InterfaceDeclaration)
        {
            return renderedType;
        }

        return $"std::optional<{renderedType}>";
    }

    private string RenderParameters(IEnumerable<MethodParameter> parameters, Declaration scope, bool includeDefaults) =>
        string.Join(", ", parameters.Select(parameter =>
        {
            var defaultValue = includeDefaults && parameter.DefaultValue is not null
                ? $" = {RenderLiteral(parameter.DefaultValue)}"
                : string.Empty;
            return $"{RenderContractType(parameter.Type, scope)} {ToCamelCase(parameter.Name)}{defaultValue}";
        }));

    private static string RenderLiteral(LiteralValue literal) =>
        literal switch
        {
            IntegerLiteralValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            FloatLiteralValue floating => floating.Value.ToString(CultureInfo.InvariantCulture),
            StringLiteralValue text => $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            BooleanLiteralValue boolean => boolean.Value ? "true" : "false",
            _ => throw new NotSupportedException($"Unsupported literal: {literal.GetType().Name}"),
        };

    private string RenderDefaultValue(TypeReference type, LiteralValue? literal, Declaration scope) =>
        literal is not null ? RenderLiteral(literal) : DefaultLiteral(type, scope);

    private string DefaultLiteral(TypeReference type, Declaration scope) =>
        type switch
        {
            PrimitiveTypeReference primitive when primitive.IsNullable => "std::nullopt",
            PrimitiveTypeReference primitive when primitive.Name == "string" => "std::string{}",
            PrimitiveTypeReference primitive when primitive.Name == "bool" => "false",
            PrimitiveTypeReference => "0",
            NamedTypeReference named when ResolveNamedDeclaration(named, scope) is InterfaceDeclaration => "nullptr",
            NamedTypeReference named when named.IsNullable => "std::nullopt",
            NamedTypeReference named => $"{RenderNamedType(named, scope)}{{}}",
            ListTypeReference list when list.IsNullable => "std::nullopt",
            ListTypeReference => "{}",
            SetTypeReference set when set.IsNullable => "std::nullopt",
            SetTypeReference => "{}",
            MapTypeReference map when map.IsNullable => "std::nullopt",
            MapTypeReference => "{}",
            ArrayTypeReference array when array.IsNullable => "std::nullopt",
            ArrayTypeReference => "{}",
            _ => "{}",
        };

    private string RenderNamedType(NamedTypeReference named, Declaration scope)
    {
        var resolved = ResolveNamedDeclaration(named, scope);
        return resolved switch
        {
            InterfaceDeclaration interfaceDeclaration => $"std::shared_ptr<{RenderResolvedTypeName(interfaceDeclaration, scope, $"I{interfaceDeclaration.Name}")}>",
            Declaration declaration => RenderResolvedTypeName(declaration, scope, declaration.Name),
            _ => string.Join("::", named.Segments),
        };
    }

    private static string RenderResolvedTypeName(Declaration declaration, Declaration scope, string localName)
    {
        if (declaration.NamespacePath.SequenceEqual(scope.NamespacePath))
        {
            return localName;
        }

        if (declaration.NamespacePath.Count == 0)
        {
            return $"::{localName}";
        }

        return $"::{string.Join("::", declaration.NamespacePath)}::{localName}";
    }

    private Declaration? ResolveNamedDeclaration(NamedTypeReference named, Declaration scope)
    {
        if (named.IsGlobal)
        {
            return ResolveExact(named.Segments);
        }

        var scopeSegments = scope.NamespacePath.Concat(scope.ContainingTypes).Concat([scope.Name]).ToArray();
        for (var index = scopeSegments.Length; index >= 0; index--)
        {
            var candidate = scopeSegments.Take(index).Concat(named.Segments).ToArray();
            var resolved = ResolveExact(candidate);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        if (scope is InterfaceDeclaration interfaceDeclaration)
        {
            foreach (var baseContract in interfaceDeclaration.BaseContracts.OfType<NamedTypeReference>())
            {
                if (ResolveNamedDeclaration(baseContract, interfaceDeclaration) is InterfaceDeclaration baseInterface)
                {
                    var resolved = ResolveNamedDeclaration(named, baseInterface);
                    if (resolved is not null)
                    {
                        return resolved;
                    }
                }
            }
        }

        return null;
    }

    private Declaration? ResolveExact(IReadOnlyList<string> segments)
    {
        var key = string.Join(".", segments);
        return _declarationsByQualifiedName.GetValueOrDefault(key);
    }

    private IReadOnlyList<InterfaceMember> GetEffectiveMembers(InterfaceDeclaration declaration)
    {
        var effective = new Dictionary<string, InterfaceMember>(StringComparer.Ordinal);

        foreach (var baseContract in declaration.BaseContracts.OfType<NamedTypeReference>())
        {
            if (ResolveNamedDeclaration(baseContract, declaration) is InterfaceDeclaration baseInterface)
            {
                foreach (var member in GetEffectiveMembers(baseInterface))
                {
                    effective[member.Name] = member;
                }
            }
        }

        foreach (var member in declaration.Members)
        {
            effective[member.Name] = member;
        }

        return effective.Values.ToArray();
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

    private static string ToPascalCase(string identifier) =>
        string.Concat(identifier
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(static segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static string ToCamelCase(string identifier)
    {
        var pascal = ToPascalCase(identifier);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
