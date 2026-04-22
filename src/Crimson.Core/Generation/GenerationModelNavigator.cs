using Crimson.Core.Model;

namespace Crimson.Core.Generation;

internal sealed class GenerationModelNavigator
{
    private readonly IReadOnlyDictionary<string, Declaration> _declarationsByQualifiedName;
    private readonly Dictionary<string, IReadOnlyList<InterfaceMember>> _effectiveMembersByInterface = new(StringComparer.Ordinal);

    public GenerationModelNavigator(CompilationSetModel compilation)
    {
        _declarationsByQualifiedName = compilation.Files
            .SelectMany(static file => EnumerateDeclarations(file.Declarations))
            .Where(static declaration => declaration is not NamespaceDeclaration)
            .GroupBy(static declaration => declaration.QualifiedName, StringComparer.Ordinal)
            .ToDictionary(static declarations => declarations.Key, static declarations => declarations.First(), StringComparer.Ordinal);
    }

    public static IEnumerable<Declaration> EnumerateDeclarations(IEnumerable<Declaration> declarations)
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

    public static string GetDeclarationPath(Declaration declaration, string fileName)
    {
        var segments = declaration.NamespacePath.Concat(declaration.ContainingTypes).ToArray();
        return segments.Length == 0
            ? fileName
            : Path.Combine(Path.Combine(segments), fileName);
    }

    public Declaration? ResolveNamedDeclaration(NamedTypeReference named, Declaration scope)
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

    public EnumDeclaration? ResolveEnumReference(NamedValueExpression namedValue, EnumDeclaration fallback, Declaration scope)
    {
        if (namedValue.Segments.Count <= 1)
        {
            return fallback;
        }

        var qualifier = new NamedTypeReference(namedValue.Segments.Take(namedValue.Segments.Count - 1).ToArray(), namedValue.IsGlobal, false, namedValue.Source);
        return ResolveNamedDeclaration(qualifier, scope) as EnumDeclaration;
    }

    public IReadOnlyList<InterfaceMember> GetEffectiveMembers(InterfaceDeclaration declaration)
    {
        if (_effectiveMembersByInterface.TryGetValue(declaration.QualifiedName, out var cached))
        {
            return cached;
        }

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

        var result = effective.Values.ToArray();
        _effectiveMembersByInterface[declaration.QualifiedName] = result;
        return result;
    }

    public IReadOnlyList<InterfaceDeclaration> GetAllBaseInterfaces(InterfaceDeclaration declaration)
    {
        var resolved = new Dictionary<string, InterfaceDeclaration>(StringComparer.Ordinal);
        AddBaseInterfaces(declaration, resolved);
        return resolved.Values.ToArray();
    }

    public IReadOnlyList<InterfaceDeclaration> GetDirectBaseInterfaces(InterfaceDeclaration declaration) =>
        declaration.BaseContracts
            .OfType<NamedTypeReference>()
            .Select(baseContract => ResolveNamedDeclaration(baseContract, declaration) as InterfaceDeclaration)
            .Where(static baseInterface => baseInterface is not null)
            .Cast<InterfaceDeclaration>()
            .ToArray();

    private Declaration? ResolveExact(IReadOnlyList<string> segments)
    {
        var key = string.Join(".", segments);
        return _declarationsByQualifiedName.GetValueOrDefault(key);
    }

    private void AddBaseInterfaces(InterfaceDeclaration declaration, Dictionary<string, InterfaceDeclaration> resolved)
    {
        foreach (var baseInterface in GetDirectBaseInterfaces(declaration))
        {
            AddBaseInterfaces(baseInterface, resolved);
            resolved[baseInterface.QualifiedName] = baseInterface;
        }
    }
}
