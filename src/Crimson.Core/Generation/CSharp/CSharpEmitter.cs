using System.Text;
using Crimson.Core.Model;

namespace Crimson.Core.Generation.CSharp;

public sealed record GeneratedFile(string RelativePath, string Content);

public sealed record GeneratedTargetTree(
    IReadOnlyList<GeneratedFile> GeneratedFiles,
    IReadOnlyList<GeneratedFile> UserFiles);

public sealed class CSharpEmitter
{
    public GeneratedTargetTree Emit(CompilationSetModel compilation)
    {
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

    private static string RenderContractInterface(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);
        var baseNames = declaration.BaseContracts.Count == 0
            ? string.Empty
            : " : " + string.Join(", ", declaration.BaseContracts.Select(RenderType));

        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine($"namespace {namespaceName};");
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
                    builder.AppendLine($"    {RenderType(valueMember.Type)} {ToPascalCase(valueMember.Name)} {{ get;{(valueMember.IsReadonly ? string.Empty : " set;")} }}");
                    builder.AppendLine();
                    break;

                case MethodMemberDeclaration methodMember:
                    AppendDocs(builder, methodMember.Documentation, 1);
                    builder.AppendLine($"    {RenderType(methodMember.ReturnType)} {ToPascalCase(methodMember.Name)}({RenderParameters(methodMember.Parameters)});");
                    builder.AppendLine();
                    break;
            }
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string RenderGeneratedClass(InterfaceDeclaration declaration)
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

        foreach (var valueMember in declaration.Members.OfType<ValueMemberDeclaration>())
        {
            var fieldName = $"_{ToCamelCase(valueMember.Name)}";
            builder.AppendLine($"    private {RenderType(valueMember.Type)} {fieldName} = {RenderDefaultValue(valueMember.Type, valueMember.DefaultValue)};");
            builder.AppendLine();

            var visibility = valueMember.IsInternal ? "protected" : "public";
            var setterVisibility = valueMember.IsInternal
                ? "set"
                : valueMember.IsReadonly
                    ? "protected set"
                    : "set";

            AppendDocs(builder, valueMember.Documentation, 1);
            builder.AppendLine($"    {visibility} {RenderType(valueMember.Type)} {ToPascalCase(valueMember.Name)}");
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
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Getting(ref {RenderType(valueMember.Type)} value);");
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Setting(ref {RenderType(valueMember.Type)} value);");
            builder.AppendLine($"    partial void On{ToPascalCase(valueMember.Name)}Set({RenderType(valueMember.Type)} value);");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string RenderUserClass(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var namespaceName = string.Join(".", declaration.NamespacePath);

        builder.AppendLine("using System;");
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

        foreach (var method in declaration.Members.OfType<MethodMemberDeclaration>())
        {
            AppendDocs(builder, method.Documentation, 1);
            builder.AppendLine($"    public virtual {RenderType(method.ReturnType)} {ToPascalCase(method.Name)}({RenderParameters(method.Parameters)})");
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

    private static string RenderConstantHolder(ConstantDeclaration declaration)
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
        builder.AppendLine($"    public const {RenderType(declaration.Type)} Value = {RenderLiteral(declaration.Value ?? new StringLiteralValue(string.Empty, null))};");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string RenderType(TypeReference type) =>
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
            NamedTypeReference named => string.Join(".", named.Segments),
            ListTypeReference list => $"List<{RenderType(list.ElementType)}>",
            SetTypeReference set => $"HashSet<{RenderType(set.ElementType)}>",
            MapTypeReference map => $"Dictionary<{RenderType(map.KeyType)}, {RenderType(map.ValueType)}>",
            ArrayTypeReference array => $"{RenderType(array.ElementType)}[]",
            _ => throw new NotSupportedException($"Unsupported type: {type.GetType().Name}"),
        };

    private static string RenderParameters(IEnumerable<MethodParameter> parameters) =>
        string.Join(", ", parameters.Select(parameter =>
        {
            var defaultValue = parameter.DefaultValue is null ? string.Empty : $" = {RenderLiteral(parameter.DefaultValue)}";
            return $"{RenderType(parameter.Type)} {ToCamelCase(parameter.Name)}{defaultValue}";
        }));

    private static string RenderLiteral(LiteralValue literal) =>
        literal switch
        {
            IntegerLiteralValue integer => integer.Value.ToString(),
            FloatLiteralValue floating => floating.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringLiteralValue text => $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            BooleanLiteralValue boolean => boolean.Value ? "true" : "false",
            _ => throw new NotSupportedException($"Unsupported literal: {literal.GetType().Name}"),
        };

    private static string RenderDefaultValue(TypeReference type, LiteralValue? literal) =>
        literal is not null ? RenderLiteral(literal) : DefaultLiteral(type);

    private static string DefaultLiteral(TypeReference type) =>
        type switch
        {
            PrimitiveTypeReference primitive when primitive.Name == "string" => "\"\"",
            PrimitiveTypeReference primitive when primitive.Name == "bool" => "false",
            PrimitiveTypeReference => "0",
            ListTypeReference list => $"new List<{RenderType(list.ElementType)}>()",
            SetTypeReference set => $"new HashSet<{RenderType(set.ElementType)}>()",
            MapTypeReference map => $"new Dictionary<{RenderType(map.KeyType)}, {RenderType(map.ValueType)}>()",
            ArrayTypeReference array when array.Length is int length => $"new {RenderType(array.ElementType)}[{length}]",
            ArrayTypeReference array => $"Array.Empty<{RenderType(array.ElementType)}>()",
            _ => "default!",
        };

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
