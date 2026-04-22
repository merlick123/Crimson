using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Crimson.Core.Model;
using Crimson.Core.Parsing.Generated;

namespace Crimson.Core.Parsing;

internal sealed class SemanticModelBuilder(string filePath, CommonTokenStream tokenStream)
{
    public CompilationUnitModel Build(CrimsonParser.CompilationUnitContext compilationUnit)
    {
        var declarations = compilationUnit.declaration()
            .Select(context => BuildDeclaration(context, [], []))
            .ToArray();

        return new CompilationUnitModel(filePath, declarations);
    }

    private Declaration BuildDeclaration(CrimsonParser.DeclarationContext context, IReadOnlyList<string> namespacePath, IReadOnlyList<string> containingTypes)
    {
        if (context.namespaceDeclaration() is { } namespaceDeclaration)
        {
            return BuildNamespace(namespaceDeclaration, namespacePath, containingTypes);
        }

        if (context.interfaceDeclaration() is { } interfaceDeclaration)
        {
            return BuildInterface(interfaceDeclaration, namespacePath, containingTypes);
        }

        if (context.enumDeclaration() is { } enumDeclaration)
        {
            return BuildEnum(enumDeclaration, namespacePath, containingTypes);
        }

        return BuildConstant(context.constantDeclaration(), namespacePath, containingTypes);
    }

    private NamespaceDeclaration BuildNamespace(CrimsonParser.NamespaceDeclarationContext context, IReadOnlyList<string> namespacePath, IReadOnlyList<string> containingTypes)
    {
        var nameSegments = BuildQualifiedName(context.qualifiedName());
        var combinedNamespace = namespacePath.Concat(nameSegments.Segments).ToArray();
        var members = context.namespaceBody().declaration()
            .Select(child => BuildDeclaration(child, combinedNamespace, containingTypes))
            .ToArray();

        return new NamespaceDeclaration(
            string.Join(".", combinedNamespace),
            namespacePath,
            containingTypes,
            BuildAnnotations(context.annotation()),
            GetDocumentation(context.Start),
            members,
            GetSource(context));
    }

    private InterfaceDeclaration BuildInterface(CrimsonParser.InterfaceDeclarationContext context, IReadOnlyList<string> namespacePath, IReadOnlyList<string> containingTypes)
    {
        var name = context.Identifier().GetText();
        var members = new List<InterfaceMember>();
        var nested = new List<Declaration>();

        foreach (var item in context.interfaceBody().interfaceItem())
        {
            if (item.interfaceMember() is { } interfaceMember)
            {
                members.Add(BuildInterfaceMember(interfaceMember));
                continue;
            }

            if (item.interfaceDeclaration() is { } nestedInterface)
            {
                nested.Add(BuildInterface(nestedInterface, namespacePath, containingTypes.Concat([name]).ToArray()));
                continue;
            }

            nested.Add(BuildEnum(item.enumDeclaration(), namespacePath, containingTypes.Concat([name]).ToArray()));
        }

        return new InterfaceDeclaration(
            name,
            namespacePath,
            containingTypes,
            BuildAnnotations(context.annotation()),
            GetDocumentation(context.Start),
            context.ABSTRACT() is not null,
            context.interfaceBases()?.typeReference().Select(BuildTypeReference).ToArray() ?? Array.Empty<TypeReference>(),
            members,
            nested,
            GetSource(context));
    }

    private EnumDeclaration BuildEnum(CrimsonParser.EnumDeclarationContext context, IReadOnlyList<string> namespacePath, IReadOnlyList<string> containingTypes)
    {
        var members = context.enumBody().enumMemberList()?.enumMember()
            .Select(member => new EnumMemberDeclaration(
                member.Identifier().GetText(),
                BuildAnnotations(member.annotation()),
                GetDocumentation(member.Start),
                member.valueExpression() is null ? null : BuildValueExpression(member.valueExpression()),
                GetSource(member)))
            .ToArray() ?? Array.Empty<EnumMemberDeclaration>();

        return new EnumDeclaration(
            context.Identifier().GetText(),
            namespacePath,
            containingTypes,
            BuildAnnotations(context.annotation()),
            GetDocumentation(context.Start),
            context.enumAssociatedType() is null ? null : BuildTypeReference(context.enumAssociatedType().typeReference()),
            members,
            GetSource(context));
    }

    private ConstantDeclaration BuildConstant(CrimsonParser.ConstantDeclarationContext context, IReadOnlyList<string> namespacePath, IReadOnlyList<string> containingTypes)
    {
        return new ConstantDeclaration(
            context.Identifier().GetText(),
            namespacePath,
            containingTypes,
            BuildAnnotations(context.annotation()),
            GetDocumentation(context.Start),
            BuildTypeReference(context.typeReference()),
            context.valueExpression() is null ? null : BuildValueExpression(context.valueExpression()),
            GetSource(context));
    }

    private InterfaceMember BuildInterfaceMember(CrimsonParser.InterfaceMemberContext context)
    {
        if (context.constantMember() is { } constantMember)
        {
            return new ConstantMemberDeclaration(
                constantMember.Identifier().GetText(),
                BuildAnnotations(constantMember.annotation()),
                GetDocumentation(constantMember.Start),
                BuildTypeReference(constantMember.typeReference()),
                constantMember.valueExpression() is null ? null : BuildValueExpression(constantMember.valueExpression()),
                GetSource(constantMember));
        }

        if (context.valueMember() is { } valueMember)
        {
            return new ValueMemberDeclaration(
                valueMember.Identifier().GetText(),
                BuildAnnotations(valueMember.annotation()),
                GetDocumentation(valueMember.Start),
                valueMember.memberModifier().Any(static x => x.READONLY() is not null),
                valueMember.memberModifier().Any(static x => x.INTERNAL() is not null),
                BuildTypeReference(valueMember.typeReference()),
                valueMember.valueExpression() is null ? null : BuildValueExpression(valueMember.valueExpression()),
                GetSource(valueMember));
        }

        var methodMember = context.methodMember();
        var methodDocs = GetDocumentation(methodMember.Start);
        return new MethodMemberDeclaration(
            methodMember.Identifier().GetText(),
            BuildAnnotations(methodMember.annotation()),
            methodDocs,
            BuildTypeReferenceOrVoid(methodMember.typeReferenceOrVoid()),
            methodMember.parameterList()?.parameter().Select(parameter => new MethodParameter(
                parameter.Identifier().GetText(),
                BuildTypeReference(parameter.typeReference()),
                parameter.valueExpression() is null ? null : BuildValueExpression(parameter.valueExpression()),
                BuildAnnotations(parameter.annotation()),
                methodDocs is null || !methodDocs.Parameters.TryGetValue(parameter.Identifier().GetText(), out var parameterDoc)
                    ? null
                    : new DocumentationComment(parameterDoc, new Dictionary<string, string>(), null, [parameterDoc]),
                GetSource(parameter))).ToArray() ?? Array.Empty<MethodParameter>(),
            GetSource(methodMember));
    }

    private IReadOnlyList<Annotation> BuildAnnotations(IEnumerable<CrimsonParser.AnnotationContext> contexts) =>
        contexts.Select(BuildAnnotation).ToArray();

    private Annotation BuildAnnotation(CrimsonParser.AnnotationContext context)
    {
        var arguments = context.annotationArguments()?.annotationArgumentList()?.annotationArgument()
            .Select(argument =>
            {
                if (argument.literal() is { } positional)
                {
                    return new AnnotationArgument(null, BuildLiteral(positional));
                }

                return new AnnotationArgument(argument.Identifier().GetText(), BuildLiteral(argument.literal()));
            })
            .ToArray() ?? Array.Empty<AnnotationArgument>();

        return new Annotation(
            BuildQualifiedName(context.qualifiedName()).DisplayName,
            arguments,
            GetSource(context));
    }

    private TypeReference BuildTypeReferenceOrVoid(CrimsonParser.TypeReferenceOrVoidContext context) =>
        context.VOID() is not null
            ? new VoidTypeReference(GetSource(context))
            : BuildTypeReference(context.typeReference());

    private TypeReference BuildTypeReference(CrimsonParser.TypeReferenceContext context)
    {
        var isNullable = context.nullableSuffix() is not null;
        var primary = context.typePrimary();
        TypeReference current;
        if (primary.primitiveType() is not null)
        {
            current = new PrimitiveTypeReference(primary.GetText(), isNullable, GetSource(context));
        }
        else if (primary.collectionType() is not null)
        {
            current = BuildCollectionType(primary.collectionType(), isNullable, GetSource(context));
        }
        else
        {
            current = BuildNamedType(primary.qualifiedName(), isNullable, GetSource(context));
        }

        foreach (var arraySuffix in context.arraySuffix())
        {
            int? length = arraySuffix.IntegerLiteral() is null
                ? null
                : int.Parse(arraySuffix.IntegerLiteral().GetText(), System.Globalization.CultureInfo.InvariantCulture);
            current = new ArrayTypeReference(current, length, isNullable, GetSource(arraySuffix));
        }

        return current;
    }

    private TypeReference BuildCollectionType(CrimsonParser.CollectionTypeContext context, bool isNullable, SourceSpan? source) =>
        context.LIST() is not null
            ? new ListTypeReference(BuildTypeReference(context.typeReference(0)), isNullable, source)
            : context.SET() is not null
                ? new SetTypeReference(BuildTypeReference(context.typeReference(0)), isNullable, source)
                : new MapTypeReference(BuildTypeReference(context.typeReference(0)), BuildTypeReference(context.typeReference(1)), isNullable, source);

    private TypeReference BuildNamedType(CrimsonParser.QualifiedNameContext context, bool isNullable, SourceSpan? source)
    {
        var qualifiedName = BuildQualifiedName(context);
        return new NamedTypeReference(qualifiedName.Segments, qualifiedName.IsGlobal, isNullable, source);
    }

    private static (string DisplayName, string[] Segments, bool IsGlobal) BuildQualifiedName(CrimsonParser.QualifiedNameContext context)
    {
        var isGlobal = context.DOT().Length > 0 && context.GetChild(0).GetText() == ".";
        var segments = context.Identifier().Select(static x => x.GetText()).ToArray();
        var display = $"{(isGlobal ? "." : string.Empty)}{string.Join(".", segments)}";
        return (display, segments, isGlobal);
    }

    private LiteralValue BuildLiteral(CrimsonParser.LiteralContext context)
    {
        if (context.IntegerLiteral() is { } integerLiteral)
        {
            return new IntegerLiteralValue(long.Parse(integerLiteral.GetText(), System.Globalization.CultureInfo.InvariantCulture), GetSource(context));
        }

        if (context.FloatLiteral() is { } floatLiteral)
        {
            return new FloatLiteralValue(double.Parse(floatLiteral.GetText(), System.Globalization.CultureInfo.InvariantCulture), GetSource(context));
        }

        if (context.StringLiteral() is { } stringLiteral)
        {
            var text = stringLiteral.GetText();
            return new StringLiteralValue(UnescapeString(text[1..^1]), GetSource(context));
        }

        return new BooleanLiteralValue(context.TRUE() is not null, GetSource(context));
    }

    private ValueExpression BuildValueExpression(CrimsonParser.ValueExpressionContext context)
    {
        if (context.literal() is { } literal)
        {
            return new LiteralValueExpression(BuildLiteral(literal), GetSource(context));
        }

        var qualifiedName = BuildQualifiedName(context.qualifiedName());
        return new NamedValueExpression(qualifiedName.Segments, qualifiedName.IsGlobal, GetSource(context));
    }

    private DocumentationComment? GetDocumentation(IToken token)
    {
        var hiddenTokens = tokenStream.GetHiddenTokensToLeft(token.TokenIndex);
        if (hiddenTokens is null)
        {
            return null;
        }

        var docs = hiddenTokens
            .Where(static token => token.Type == CrimsonLexer.DOC_LINE_COMMENT || token.Type == CrimsonLexer.DOC_BLOCK_COMMENT)
            .Select(static token => token.Text)
            .ToArray();

        return DocumentationComments.Parse(docs);
    }

    private SourceSpan? GetSource(ParserRuleContext context)
    {
        if (context.Start is null || context.Stop is null)
        {
            return null;
        }

        return new SourceSpan(
            filePath,
            new SourcePoint(context.Start.Line, context.Start.Column + 1),
            new SourcePoint(context.Stop.Line, context.Stop.Column + Math.Max(1, context.Stop.Text?.Length ?? 1)));
    }

    private SourceSpan? GetSource(IToken? token)
    {
        if (token is null)
        {
            return null;
        }

        return new SourceSpan(
            filePath,
            new SourcePoint(token.Line, token.Column + 1),
            new SourcePoint(token.Line, token.Column + Math.Max(1, token.Text?.Length ?? 1)));
    }

    private static string UnescapeString(string text)
    {
        return text
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal);
    }
}
