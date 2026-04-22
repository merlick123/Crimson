using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crimson.Core.Model;

public sealed record SourcePoint(int Line, int Column);

public sealed record SourceSpan(string FilePath, SourcePoint Start, SourcePoint End);

public sealed record DocumentationComment(
    string Summary,
    IReadOnlyDictionary<string, string> Parameters,
    string? Returns,
    IReadOnlyList<string> RawLines);

public sealed record AnnotationArgument(string? Name, LiteralValue Value);

public sealed record Annotation(
    string Name,
    IReadOnlyList<AnnotationArgument> Arguments,
    SourceSpan? Source);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(IntegerLiteralValue), "integer")]
[JsonDerivedType(typeof(FloatLiteralValue), "float")]
[JsonDerivedType(typeof(StringLiteralValue), "string")]
[JsonDerivedType(typeof(BooleanLiteralValue), "bool")]
public abstract record LiteralValue(SourceSpan? Source);

public sealed record IntegerLiteralValue(long Value, SourceSpan? Source) : LiteralValue(Source);

public sealed record FloatLiteralValue(double Value, SourceSpan? Source) : LiteralValue(Source);

public sealed record StringLiteralValue(string Value, SourceSpan? Source) : LiteralValue(Source);

public sealed record BooleanLiteralValue(bool Value, SourceSpan? Source) : LiteralValue(Source);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(LiteralValueExpression), "literal")]
[JsonDerivedType(typeof(NamedValueExpression), "named")]
public abstract record ValueExpression(SourceSpan? Source);

public sealed record LiteralValueExpression(LiteralValue Value, SourceSpan? Source) : ValueExpression(Source);

public sealed record NamedValueExpression(
    IReadOnlyList<string> Segments,
    bool IsGlobal,
    SourceSpan? Source)
    : ValueExpression(Source)
{
    public string DisplayName => $"{(IsGlobal ? "." : string.Empty)}{string.Join(".", Segments)}";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(VoidTypeReference), "void")]
[JsonDerivedType(typeof(PrimitiveTypeReference), "primitive")]
[JsonDerivedType(typeof(NamedTypeReference), "named")]
[JsonDerivedType(typeof(ListTypeReference), "list")]
[JsonDerivedType(typeof(SetTypeReference), "set")]
[JsonDerivedType(typeof(MapTypeReference), "map")]
[JsonDerivedType(typeof(ArrayTypeReference), "array")]
public abstract record TypeReference(bool IsNullable, SourceSpan? Source);

public sealed record VoidTypeReference(SourceSpan? Source) : TypeReference(false, Source);

public sealed record PrimitiveTypeReference(string Name, bool IsNullable, SourceSpan? Source)
    : TypeReference(IsNullable, Source);

public sealed record NamedTypeReference(
    IReadOnlyList<string> Segments,
    bool IsGlobal,
    bool IsNullable,
    SourceSpan? Source)
    : TypeReference(IsNullable, Source)
{
    public string DisplayName => $"{(IsGlobal ? "." : string.Empty)}{string.Join(".", Segments)}";
}

public sealed record ListTypeReference(TypeReference ElementType, bool IsNullable, SourceSpan? Source)
    : TypeReference(IsNullable, Source);

public sealed record SetTypeReference(TypeReference ElementType, bool IsNullable, SourceSpan? Source)
    : TypeReference(IsNullable, Source);

public sealed record MapTypeReference(TypeReference KeyType, TypeReference ValueType, bool IsNullable, SourceSpan? Source)
    : TypeReference(IsNullable, Source);

public sealed record ArrayTypeReference(TypeReference ElementType, int? Length, bool IsNullable, SourceSpan? Source)
    : TypeReference(IsNullable, Source);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NamespaceDeclaration), "namespace")]
[JsonDerivedType(typeof(InterfaceDeclaration), "interface")]
[JsonDerivedType(typeof(StructDeclaration), "struct")]
[JsonDerivedType(typeof(EnumDeclaration), "enum")]
[JsonDerivedType(typeof(ConstantDeclaration), "constant")]
public abstract record Declaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    SourceSpan? Source)
{
    public string QualifiedName =>
        string.Join(".",
            NamespacePath
                .Concat(ContainingTypes)
                .Concat([Name]));
}

public sealed record NamespaceDeclaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    IReadOnlyList<Declaration> Members,
    SourceSpan? Source)
    : Declaration(Name, NamespacePath, ContainingTypes, Annotations, Documentation, Source);

public sealed record InterfaceDeclaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    bool IsAbstract,
    IReadOnlyList<TypeReference> BaseContracts,
    IReadOnlyList<InterfaceMember> Members,
    IReadOnlyList<Declaration> NestedDeclarations,
    SourceSpan? Source)
    : Declaration(Name, NamespacePath, ContainingTypes, Annotations, Documentation, Source);

public sealed record StructDeclaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    IReadOnlyList<InterfaceMember> Members,
    SourceSpan? Source)
    : Declaration(Name, NamespacePath, ContainingTypes, Annotations, Documentation, Source);

public sealed record EnumDeclaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    TypeReference? AssociatedValueType,
    IReadOnlyList<EnumMemberDeclaration> Members,
    SourceSpan? Source)
    : Declaration(Name, NamespacePath, ContainingTypes, Annotations, Documentation, Source);

public sealed record ConstantDeclaration(
    string Name,
    IReadOnlyList<string> NamespacePath,
    IReadOnlyList<string> ContainingTypes,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    TypeReference Type,
    ValueExpression? Value,
    SourceSpan? Source)
    : Declaration(Name, NamespacePath, ContainingTypes, Annotations, Documentation, Source);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ValueMemberDeclaration), "valueMember")]
[JsonDerivedType(typeof(MethodMemberDeclaration), "methodMember")]
[JsonDerivedType(typeof(ConstantMemberDeclaration), "constantMember")]
public abstract record InterfaceMember(
    string Name,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    SourceSpan? Source);

public sealed record ValueMemberDeclaration(
    string Name,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    bool IsReadonly,
    bool IsInternal,
    TypeReference Type,
    ValueExpression? DefaultValue,
    SourceSpan? Source)
    : InterfaceMember(Name, Annotations, Documentation, Source);

public sealed record MethodParameter(
    string Name,
    TypeReference Type,
    ValueExpression? DefaultValue,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    SourceSpan? Source);

public sealed record MethodMemberDeclaration(
    string Name,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    TypeReference ReturnType,
    IReadOnlyList<MethodParameter> Parameters,
    SourceSpan? Source)
    : InterfaceMember(Name, Annotations, Documentation, Source);

public sealed record ConstantMemberDeclaration(
    string Name,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    TypeReference Type,
    ValueExpression? Value,
    SourceSpan? Source)
    : InterfaceMember(Name, Annotations, Documentation, Source);

public sealed record EnumMemberDeclaration(
    string Name,
    IReadOnlyList<Annotation> Annotations,
    DocumentationComment? Documentation,
    ValueExpression? AssociatedValue,
    SourceSpan? Source);

public sealed record CompilationUnitModel(
    string FilePath,
    IReadOnlyList<Declaration> Declarations);

public sealed record CompilationSetModel(
    IReadOnlyList<CompilationUnitModel> Files);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
