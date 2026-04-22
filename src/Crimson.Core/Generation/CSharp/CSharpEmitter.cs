using System.Text;
using Crimson.Core.Model;

namespace Crimson.Core.Generation.CSharp;

public sealed record GeneratedTargetTree(
    IReadOnlyList<GeneratedFile> GeneratedFiles,
    IReadOnlyList<GeneratedFile> UserFiles);

public sealed class CSharpEmitter
{
    private Dictionary<string, Declaration> _declarationsByQualifiedName = new(StringComparer.Ordinal);

    public GeneratedTargetTree Emit(CompilationSetModel compilation)
    {
        _declarationsByQualifiedName = compilation.Files
            .SelectMany(static file => EnumerateDeclarations(file.Declarations))
            .Where(static declaration => declaration is not NamespaceDeclaration)
            .GroupBy(static declaration => declaration.QualifiedName, StringComparer.Ordinal)
            .ToDictionary(static declarations => declarations.Key, static declarations => declarations.First(), StringComparer.Ordinal);

        var generatedFiles = new List<GeneratedFile>();
        var userFiles = new List<GeneratedFile>();

        foreach (var declaration in compilation.Files.SelectMany(static x => x.Declarations))
        {
            EmitDeclaration(declaration, generatedFiles, userFiles);
        }

        return new GeneratedTargetTree(generatedFiles, userFiles);
    }

    private void EmitDeclaration(Declaration declaration, List<GeneratedFile> generatedFiles, List<GeneratedFile> userFiles)
    {
        switch (declaration)
        {
            case NamespaceDeclaration namespaceDeclaration:
                foreach (var member in namespaceDeclaration.Members)
                {
                    EmitDeclaration(member, generatedFiles, userFiles);
                }

                break;

            case InterfaceDeclaration interfaceDeclaration:
                EmitInterface(interfaceDeclaration, generatedFiles, userFiles);
                foreach (var nested in interfaceDeclaration.NestedDeclarations)
                {
                    EmitDeclaration(nested, generatedFiles, userFiles);
                }

                break;

            case EnumDeclaration enumDeclaration:
                generatedFiles.Add(new GeneratedFile(GetDeclarationPath(enumDeclaration, $"{enumDeclaration.Name}.g.cs"), RenderEnum(enumDeclaration)));
                break;

            case ConstantDeclaration constantDeclaration:
                generatedFiles.Add(new GeneratedFile(GetDeclarationPath(constantDeclaration, $"{constantDeclaration.Name}.g.cs"), RenderConstantHolder(constantDeclaration)));
                break;
        }
    }

    private void EmitInterface(InterfaceDeclaration declaration, List<GeneratedFile> generatedFiles, List<GeneratedFile> userFiles)
    {
        generatedFiles.Add(new GeneratedFile(GetDeclarationPath(declaration, $"I{declaration.Name}.g.cs"), RenderContractInterface(declaration)));

        if (!declaration.IsAbstract)
        {
            generatedFiles.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.g.cs"), RenderGeneratedClass(declaration)));
            userFiles.Add(new GeneratedFile(GetDeclarationPath(declaration, $"{declaration.Name}.cs"), RenderUserClass(declaration)));
        }
    }

    private static string GetDeclarationPath(Declaration declaration, string fileName)
    {
        var segments = declaration.NamespacePath.Concat(declaration.ContainingTypes).ToArray();
        return segments.Length == 0
            ? fileName
            : Path.Combine(Path.Combine(segments), fileName);
    }

    private string RenderContractInterface(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);
        var baseNames = declaration.BaseContracts.Count == 0
            ? string.Empty
            : " : " + string.Join(", ", declaration.BaseContracts.Select(baseContract => RenderContractType(baseContract, declaration)));

        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
        }

        AppendDocs(builder, declaration.Documentation, 0);
        builder.AppendLine($"public interface I{declaration.Name}{baseNames}");
        builder.AppendLine("{");

        foreach (var member in declaration.Members)
        {
            switch (member)
            {
                case ValueMemberDeclaration valueMember when !valueMember.IsInternal:
                    AppendDocs(builder, valueMember.Documentation, 1);
                    builder.AppendLine($"    {RenderContractType(valueMember.Type, declaration)} {ToPascalCase(valueMember.Name)} {{ get;{(valueMember.IsReadonly ? string.Empty : " set;")} }}");
                    builder.AppendLine();
                    break;

                case MethodMemberDeclaration methodMember:
                    AppendDocs(builder, methodMember.Documentation, 1);
                    builder.AppendLine($"    {RenderContractType(methodMember.ReturnType, declaration)} {ToPascalCase(methodMember.Name)}({RenderParameters(methodMember.Parameters, declaration)});");
                    builder.AppendLine();
                    break;
            }
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderGeneratedClass(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);

        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
        }

        builder.AppendLine($"public partial class {declaration.Name} : I{declaration.Name}");
        builder.AppendLine("{");

        foreach (var valueMember in GetEffectiveMembers(declaration).OfType<ValueMemberDeclaration>())
        {
            var fieldName = $"_{ToCamelCase(valueMember.Name)}";
            builder.AppendLine($"    private {RenderContractType(valueMember.Type, declaration)} {fieldName} = {RenderDefaultValue(valueMember.Type, valueMember.DefaultValue, declaration)};");
            builder.AppendLine();

            var visibility = valueMember.IsInternal ? "protected" : "public";
            var setterVisibility = valueMember.IsInternal
                ? "set"
                : valueMember.IsReadonly
                    ? "protected set"
                    : "set";

            AppendDocs(builder, valueMember.Documentation, 1);
            builder.AppendLine($"    {visibility} {RenderContractType(valueMember.Type, declaration)} {ToPascalCase(valueMember.Name)}");
            builder.AppendLine("    {");
            builder.AppendLine("        get");
            builder.AppendLine("        {");
            builder.AppendLine($"            var currentValue = {fieldName};");
            builder.AppendLine($"            On{ToPascalCase(valueMember.Name)}Getting(ref currentValue);");
            builder.AppendLine("            return currentValue;");
            builder.AppendLine("        }");
            builder.AppendLine($"        {setterVisibility}");
            builder.AppendLine("        {");
            builder.AppendLine("            var newValue = value;");
            builder.AppendLine($"            On{ToPascalCase(valueMember.Name)}Setting(ref newValue);");
            builder.AppendLine($"            {fieldName} = newValue;");
            builder.AppendLine($"            On{ToPascalCase(valueMember.Name)}Set(newValue);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Getting(ref {RenderContractType(valueMember.Type, declaration)} value);");
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Setting(ref {RenderContractType(valueMember.Type, declaration)} value);");
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Set({RenderContractType(valueMember.Type, declaration)} value);");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderUserClass(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);

        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine();
        }

        builder.AppendLine($"public partial class {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    public {declaration.Name}()");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var method in GetEffectiveMembers(declaration).OfType<MethodMemberDeclaration>())
        {
            AppendDocs(builder, method.Documentation, 1);
            builder.AppendLine($"    public virtual {RenderContractType(method.ReturnType, declaration)} {ToPascalCase(method.Name)}({RenderParameters(method.Parameters, declaration)})");
            builder.AppendLine("    {");
            builder.AppendLine("        throw new NotImplementedException();");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string RenderEnum(EnumDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine();
        }

        builder.AppendLine($"public enum {declaration.Name}");
        builder.AppendLine("{");
        for (var index = 0; index < declaration.Members.Count; index++)
        {
            var member = declaration.Members[index];
            var suffix = index == declaration.Members.Count - 1 ? string.Empty : ",";
            builder.AppendLine($"    {member.Name}{suffix}");
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderConstantHolder(ConstantDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine();
        }

        builder.AppendLine($"public static class {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    public const {RenderContractType(declaration.Type, declaration)} Value = {RenderValueExpression(declaration.Value ?? throw new InvalidOperationException($"Constant '{declaration.QualifiedName}' must declare a value."), declaration.Type, declaration)};");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderContractType(TypeReference type, Declaration scope) =>
        RenderType(type, scope, preferInterfaceContracts: true);

    private string RenderType(TypeReference type, Declaration scope, bool preferInterfaceContracts) =>
        ApplyNullability(RenderTypeCore(type, scope, preferInterfaceContracts), type);

    private string RenderTypeCore(TypeReference type, Declaration scope, bool preferInterfaceContracts) =>
        type switch
        {
            VoidTypeReference => "void",
            PrimitiveTypeReference primitive => primitive.Name switch
            {
                "bool" => "bool",
                "string" => "string",
                "int8" => "sbyte",
                "uint8" => "byte",
                "int16" => "short",
                "uint16" => "ushort",
                "int32" => "int",
                "uint32" => "uint",
                "int64" => "long",
                "uint64" => "ulong",
                "float32" => "float",
                "float64" => "double",
                _ => primitive.Name,
            },
            NamedTypeReference named => RenderNamedType(named, scope, preferInterfaceContracts),
            ListTypeReference list => $"List<{RenderType(list.ElementType, scope, preferInterfaceContracts)}>",
            SetTypeReference set => $"HashSet<{RenderType(set.ElementType, scope, preferInterfaceContracts)}>",
            MapTypeReference map => $"Dictionary<{RenderType(map.KeyType, scope, preferInterfaceContracts)}, {RenderType(map.ValueType, scope, preferInterfaceContracts)}>",
            ArrayTypeReference array => $"{RenderType(array.ElementType, scope, preferInterfaceContracts)}[]",
            _ => throw new NotSupportedException($"Unsupported type: {type.GetType().Name}"),
        };

    private static string ApplyNullability(string renderedType, TypeReference type) =>
        type.IsNullable && type is not VoidTypeReference
            ? $"{renderedType}?"
            : renderedType;

    private string RenderParameters(IEnumerable<MethodParameter> parameters, Declaration scope) =>
        string.Join(", ", parameters.Select(parameter =>
        {
            var defaultValue = parameter.DefaultValue is null ? string.Empty : $" = {RenderValueExpression(parameter.DefaultValue, parameter.Type, scope)}";
            return $"{RenderContractType(parameter.Type, scope)} {ToCamelCase(parameter.Name)}{defaultValue}";
        }));

    private string RenderValueExpression(ValueExpression expression, TypeReference declaredType, Declaration scope) =>
        expression switch
        {
            LiteralValueExpression literalExpression => RenderLiteral(literalExpression.Value),
            NamedValueExpression namedValue => RenderNamedValueExpression(namedValue, declaredType, scope),
            _ => throw new NotSupportedException($"Unsupported value expression: {expression.GetType().Name}"),
        };

    private static string RenderLiteral(LiteralValue literal) =>
        literal switch
        {
            IntegerLiteralValue integer => integer.Value.ToString(),
            FloatLiteralValue floating => floating.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringLiteralValue text => $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            BooleanLiteralValue boolean => boolean.Value ? "true" : "false",
            _ => throw new NotSupportedException($"Unsupported literal: {literal.GetType().Name}"),
        };

    private string RenderDefaultValue(TypeReference type, ValueExpression? valueExpression, Declaration scope) =>
        valueExpression is not null ? RenderValueExpression(valueExpression, type, scope) : DefaultLiteral(type, scope);

    private string DefaultLiteral(TypeReference type, Declaration scope) =>
        type.IsNullable ? "null" :
        type switch
        {
            PrimitiveTypeReference primitive when primitive.Name == "string" => "\"\"",
            PrimitiveTypeReference primitive when primitive.Name == "bool" => "false",
            PrimitiveTypeReference => "0",
            NamedTypeReference named when ResolveNamedDeclaration(named, scope) is EnumDeclaration => "default",
            NamedTypeReference named when ResolveNamedDeclaration(named, scope) is InterfaceDeclaration interfaceDeclaration && IsValueContract(interfaceDeclaration) => $"new {RenderResolvedTypeName(interfaceDeclaration, scope, interfaceDeclaration.Name)}()",
            ListTypeReference list => $"new List<{RenderType(list.ElementType, scope, preferInterfaceContracts: true)}>()",
            SetTypeReference set => $"new HashSet<{RenderType(set.ElementType, scope, preferInterfaceContracts: true)}>()",
            MapTypeReference map => $"new Dictionary<{RenderType(map.KeyType, scope, preferInterfaceContracts: true)}, {RenderType(map.ValueType, scope, preferInterfaceContracts: true)}>()",
            ArrayTypeReference array when array.Length is int length => $"new {RenderType(array.ElementType, scope, preferInterfaceContracts: true)}[{length}]",
            ArrayTypeReference array => $"Array.Empty<{RenderType(array.ElementType, scope, preferInterfaceContracts: true)}>()",
            _ => "default!",
        };

    private string RenderNamedType(NamedTypeReference named, Declaration scope, bool preferInterfaceContracts)
    {
        var resolved = ResolveNamedDeclaration(named, scope);
        return resolved switch
        {
            InterfaceDeclaration interfaceDeclaration when IsValueContract(interfaceDeclaration) => RenderResolvedTypeName(interfaceDeclaration, scope, interfaceDeclaration.Name),
            InterfaceDeclaration interfaceDeclaration when preferInterfaceContracts => RenderResolvedTypeName(interfaceDeclaration, scope, $"I{interfaceDeclaration.Name}"),
            Declaration declaration => RenderResolvedTypeName(declaration, scope, declaration.Name),
            _ => string.Join(".", named.Segments),
        };
    }

    private string RenderNamedValueExpression(NamedValueExpression namedValue, TypeReference declaredType, Declaration scope)
    {
        if (declaredType is NamedTypeReference namedType &&
            ResolveNamedDeclaration(namedType, scope) is EnumDeclaration targetEnum)
        {
            var enumDeclaration = ResolveEnumReference(namedValue, targetEnum, scope) ?? targetEnum;
            return $"{RenderResolvedTypeName(enumDeclaration, scope, enumDeclaration.Name)}.{namedValue.Segments[^1]}";
        }

        return namedValue.DisplayName;
    }

    private EnumDeclaration? ResolveEnumReference(NamedValueExpression namedValue, EnumDeclaration fallback, Declaration scope)
    {
        if (namedValue.Segments.Count <= 1)
        {
            return fallback;
        }

        var qualifier = new NamedTypeReference(namedValue.Segments.Take(namedValue.Segments.Count - 1).ToArray(), namedValue.IsGlobal, false, namedValue.Source);
        return ResolveNamedDeclaration(qualifier, scope) as EnumDeclaration;
    }

    private static bool IsValueContract(InterfaceDeclaration declaration) =>
        declaration.Annotations.Any(annotation => string.Equals(annotation.Name.Split('.').Last(), "value", StringComparison.OrdinalIgnoreCase));

    private static string RenderResolvedTypeName(Declaration declaration, Declaration scope, string localName)
    {
        if (declaration.NamespacePath.SequenceEqual(scope.NamespacePath))
        {
            return localName;
        }

        if (declaration.NamespacePath.Count == 0)
        {
            return $"global::{localName}";
        }

        return $"global::{string.Join(".", declaration.NamespacePath)}.{localName}";
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

    private static void AppendDocs(StringBuilder builder, DocumentationComment? documentation, int indentLevel)
    {
        if (documentation is null)
        {
            return;
        }

        var indent = new string(' ', indentLevel * 4);
        foreach (var line in documentation.Summary.Split(Environment.NewLine, StringSplitOptions.None))
        {
            builder.AppendLine($"{indent}/// <summary>{EscapeXml(line)}</summary>");
        }

        foreach (var parameter in documentation.Parameters)
        {
            builder.AppendLine($"{indent}/// <param name=\"{ToCamelCase(parameter.Key)}\">{EscapeXml(parameter.Value)}</param>");
        }

        if (!string.IsNullOrWhiteSpace(documentation.Returns))
        {
            builder.AppendLine($"{indent}/// <returns>{EscapeXml(documentation.Returns!)}</returns>");
        }
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

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
