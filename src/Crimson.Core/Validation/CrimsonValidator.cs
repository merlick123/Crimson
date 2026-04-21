using Crimson.Core.Model;

namespace Crimson.Core.Validation;

public sealed class CrimsonValidator
{
    private readonly List<Diagnostic> _diagnostics = [];
    private Dictionary<string, Declaration> _declarationsByName = new(StringComparer.Ordinal);
    private Dictionary<string, List<Declaration>> _allDeclarationsByName = new(StringComparer.Ordinal);
    private Dictionary<InterfaceDeclaration, List<InterfaceDeclaration>> _resolvedBaseInterfaces = new();

    public void Validate(CompilationSetModel compilation)
    {
        _diagnostics.Clear();
        _declarationsByName = new Dictionary<string, Declaration>(StringComparer.Ordinal);
        _allDeclarationsByName = new Dictionary<string, List<Declaration>>(StringComparer.Ordinal);
        _resolvedBaseInterfaces = new Dictionary<InterfaceDeclaration, List<InterfaceDeclaration>>();

        var declarations = compilation.Files.SelectMany(static file => file.Declarations).ToArray();
        IndexDeclarations(declarations);
        ValidateDeclarations(declarations);
        ValidateInterfaceCycles();

        if (_diagnostics.Count > 0)
        {
            throw new DiagnosticException(_diagnostics.ToArray());
        }
    }

    private void IndexDeclarations(IEnumerable<Declaration> declarations)
    {
        foreach (var declaration in EnumerateDeclarations(declarations))
        {
            if (!_allDeclarationsByName.TryGetValue(declaration.QualifiedName, out var list))
            {
                list = [];
                _allDeclarationsByName[declaration.QualifiedName] = list;
            }

            list.Add(declaration);
        }

        foreach (var entry in _allDeclarationsByName)
        {
            if (entry.Value.All(static declaration => declaration is NamespaceDeclaration))
            {
                continue;
            }

            if (entry.Value.Count > 1)
            {
                foreach (var declaration in entry.Value)
                {
                    _diagnostics.Add(new Diagnostic(
                        "CRIMSON100",
                        $"Duplicate declaration '{entry.Key}'.",
                        "error",
                        declaration.Source));
                }

                continue;
            }

            _declarationsByName[entry.Key] = entry.Value[0];
        }
    }

    private void ValidateDeclarations(IEnumerable<Declaration> declarations)
    {
        foreach (var declaration in declarations)
        {
            ValidateDeclaration(declaration);
        }
    }

    private void ValidateDeclaration(Declaration declaration)
    {
        switch (declaration)
        {
            case NamespaceDeclaration namespaceDeclaration:
                foreach (var member in namespaceDeclaration.Members)
                {
                    ValidateDeclaration(member);
                }

                break;

            case InterfaceDeclaration interfaceDeclaration:
                ValidateInterface(interfaceDeclaration);
                foreach (var nested in interfaceDeclaration.NestedDeclarations)
                {
                    ValidateDeclaration(nested);
                }

                break;

            case EnumDeclaration enumDeclaration:
                ValidateEnum(enumDeclaration);
                break;

            case ConstantDeclaration constantDeclaration:
                ValidateNonVoidType(constantDeclaration.Type, constantDeclaration.Source, $"Constant '{constantDeclaration.QualifiedName}'");
                ValidateTypeReference(constantDeclaration.Type, declaration.NamespacePath.Concat(declaration.ContainingTypes).ToArray(), declaration.Source);
                ValidateLiteralCompatibility(constantDeclaration.Type, constantDeclaration.Value, constantDeclaration.Source);
                break;
        }
    }

    private void ValidateInterface(InterfaceDeclaration declaration)
    {
        var scope = declaration.NamespacePath.Concat(declaration.ContainingTypes).Concat([declaration.Name]).ToArray();
        var membersByName = new HashSet<string>(StringComparer.Ordinal);
        var resolvedBases = new List<InterfaceDeclaration>();

        foreach (var baseContract in declaration.BaseContracts)
        {
            ValidateTypeReference(baseContract, scope, declaration.Source);
            if (baseContract is not NamedTypeReference namedType)
            {
                _diagnostics.Add(new Diagnostic("CRIMSON101", $"Interface '{declaration.QualifiedName}' has a non-named base contract.", "error", baseContract.Source));
                continue;
            }

            var resolved = ResolveNamedType(namedType, scope).ToArray();
            if (resolved.Length == 0)
            {
                _diagnostics.Add(new Diagnostic("CRIMSON102", $"Unable to resolve base contract '{namedType.DisplayName}' for interface '{declaration.QualifiedName}'.", "error", baseContract.Source));
                continue;
            }

            foreach (var target in resolved)
            {
                if (target is InterfaceDeclaration interfaceTarget)
                {
                    resolvedBases.Add(interfaceTarget);
                }
                else
                {
                    _diagnostics.Add(new Diagnostic("CRIMSON103", $"Base contract '{namedType.DisplayName}' for interface '{declaration.QualifiedName}' does not resolve to an interface.", "error", baseContract.Source));
                }
            }
        }

        _resolvedBaseInterfaces[declaration] = resolvedBases;
        var inheritedMembers = CollectInheritedMembers(declaration);

        foreach (var member in declaration.Members)
        {
            if (!membersByName.Add(member.Name))
            {
                _diagnostics.Add(new Diagnostic("CRIMSON104", $"Interface '{declaration.QualifiedName}' contains duplicate member '{member.Name}'.", "error", member.Source));
            }

            if (inheritedMembers.TryGetValue(member.Name, out var inheritedOrigin))
            {
                _diagnostics.Add(new Diagnostic(
                    "CRIMSON113",
                    $"Interface '{declaration.QualifiedName}' redeclares inherited member '{member.Name}' from '{inheritedOrigin.Origin.QualifiedName}'.",
                    "error",
                    member.Source));
            }

            switch (member)
            {
                case ValueMemberDeclaration valueMember:
                    ValidateNonVoidType(valueMember.Type, valueMember.Source, $"Value member '{declaration.QualifiedName}.{valueMember.Name}'");
                    ValidateTypeReference(valueMember.Type, scope, valueMember.Source, declaration);
                    ValidateLiteralCompatibility(valueMember.Type, valueMember.DefaultValue, valueMember.Source);
                    break;

                case MethodMemberDeclaration methodMember:
                    ValidateTypeReference(methodMember.ReturnType, scope, methodMember.Source, declaration);
                    var parameterNames = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var parameter in methodMember.Parameters)
                    {
                        if (!parameterNames.Add(parameter.Name))
                        {
                            _diagnostics.Add(new Diagnostic("CRIMSON105", $"Method '{declaration.QualifiedName}.{methodMember.Name}' contains duplicate parameter '{parameter.Name}'.", "error", parameter.Source));
                        }

                        ValidateNonVoidType(parameter.Type, parameter.Source, $"Parameter '{declaration.QualifiedName}.{methodMember.Name}({parameter.Name})'");
                        ValidateTypeReference(parameter.Type, scope, parameter.Source, declaration);
                        ValidateLiteralCompatibility(parameter.Type, parameter.DefaultValue, parameter.Source);
                    }

                    break;

                case ConstantMemberDeclaration constantMember:
                    ValidateNonVoidType(constantMember.Type, constantMember.Source, $"Constant member '{declaration.QualifiedName}.{constantMember.Name}'");
                    ValidateTypeReference(constantMember.Type, scope, constantMember.Source, declaration);
                    ValidateLiteralCompatibility(constantMember.Type, constantMember.Value, constantMember.Source);
                    break;
            }
        }
    }

    private Dictionary<string, MemberOrigin> CollectInheritedMembers(InterfaceDeclaration declaration)
    {
        var inheritedMembers = new Dictionary<string, MemberOrigin>(StringComparer.Ordinal);
        var visitedOrigins = new HashSet<InterfaceDeclaration>();

        foreach (var baseInterface in _resolvedBaseInterfaces.GetValueOrDefault(declaration, []))
        {
            CollectInheritedMembersRecursive(declaration, baseInterface, inheritedMembers, visitedOrigins);
        }

        return inheritedMembers;
    }

    private void CollectInheritedMembersRecursive(
        InterfaceDeclaration target,
        InterfaceDeclaration origin,
        Dictionary<string, MemberOrigin> inheritedMembers,
        HashSet<InterfaceDeclaration> visitedOrigins)
    {
        if (!visitedOrigins.Add(origin))
        {
            return;
        }

        foreach (var member in origin.Members)
        {
            if (inheritedMembers.TryGetValue(member.Name, out var existing))
            {
                if (!ReferenceEquals(existing.Origin, origin))
                {
                    _diagnostics.Add(new Diagnostic(
                        "CRIMSON113",
                        $"Interface '{target.QualifiedName}' inherits conflicting member '{member.Name}' from '{existing.Origin.QualifiedName}' and '{origin.QualifiedName}'.",
                        "error",
                        member.Source));
                }

                continue;
            }

            inheritedMembers[member.Name] = new MemberOrigin(origin, member);
        }

        foreach (var baseInterface in _resolvedBaseInterfaces.GetValueOrDefault(origin, []))
        {
            CollectInheritedMembersRecursive(target, baseInterface, inheritedMembers, visitedOrigins);
        }
    }

    private void ValidateEnum(EnumDeclaration declaration)
    {
        var scope = declaration.NamespacePath.Concat(declaration.ContainingTypes).Concat([declaration.Name]).ToArray();
        var memberNames = new HashSet<string>(StringComparer.Ordinal);

        if (declaration.AssociatedValueType is not null)
        {
            ValidateNonVoidType(declaration.AssociatedValueType, declaration.AssociatedValueType.Source, $"Enum '{declaration.QualifiedName}' associated value type");
            ValidateTypeReference(declaration.AssociatedValueType, scope, declaration.AssociatedValueType.Source);
        }

        foreach (var member in declaration.Members)
        {
            if (!memberNames.Add(member.Name))
            {
                _diagnostics.Add(new Diagnostic("CRIMSON106", $"Enum '{declaration.QualifiedName}' contains duplicate member '{member.Name}'.", "error", member.Source));
            }

            if (declaration.AssociatedValueType is null && member.AssociatedValue is not null)
            {
                _diagnostics.Add(new Diagnostic("CRIMSON107", $"Enum member '{declaration.QualifiedName}.{member.Name}' cannot declare an associated value without an enum associated value type.", "error", member.Source));
            }

            if (declaration.AssociatedValueType is not null)
            {
                ValidateLiteralCompatibility(declaration.AssociatedValueType, member.AssociatedValue, member.Source);
            }
        }
    }

    private void ValidateTypeReference(TypeReference typeReference, IReadOnlyList<string> scope, SourceSpan? source, InterfaceDeclaration? containingInterface = null)
    {
        switch (typeReference)
        {
            case PrimitiveTypeReference:
            case VoidTypeReference:
                return;

            case NamedTypeReference namedType:
                if (!ResolveNamedType(namedType, scope, containingInterface).Any())
                {
                    _diagnostics.Add(new Diagnostic("CRIMSON108", $"Unable to resolve type '{namedType.DisplayName}'.", "error", source ?? namedType.Source));
                }

                return;

            case ListTypeReference listType:
                ValidateTypeReference(listType.ElementType, scope, listType.Source, containingInterface);
                return;

            case SetTypeReference setType:
                ValidateTypeReference(setType.ElementType, scope, setType.Source, containingInterface);
                return;

            case ArrayTypeReference arrayType:
                ValidateTypeReference(arrayType.ElementType, scope, arrayType.Source, containingInterface);
                return;

            case MapTypeReference mapType:
                ValidateTypeReference(mapType.KeyType, scope, mapType.Source, containingInterface);
                ValidateTypeReference(mapType.ValueType, scope, mapType.Source, containingInterface);
                ValidateMapKeyType(mapType.KeyType, scope, mapType.Source, containingInterface);
                return;
        }
    }

    private void ValidateMapKeyType(TypeReference keyType, IReadOnlyList<string> scope, SourceSpan? source, InterfaceDeclaration? containingInterface = null)
    {
        switch (keyType)
        {
            case PrimitiveTypeReference:
                return;

            case NamedTypeReference namedType:
                var resolved = ResolveNamedType(namedType, scope, containingInterface).FirstOrDefault();
                if (resolved is EnumDeclaration)
                {
                    return;
                }

                break;
        }

        _diagnostics.Add(new Diagnostic("CRIMSON109", "Map keys must be primitive scalars or enums.", "error", source ?? keyType.Source));
    }

    private void ValidateNonVoidType(TypeReference typeReference, SourceSpan? source, string context)
    {
        if (typeReference is VoidTypeReference)
        {
            _diagnostics.Add(new Diagnostic("CRIMSON112", $"{context} cannot use 'void' as a declared type.", "error", source ?? typeReference.Source));
        }
    }

    private void ValidateLiteralCompatibility(TypeReference declaredType, LiteralValue? literal, SourceSpan? source)
    {
        if (literal is null)
        {
            return;
        }

        var isCompatible = declaredType switch
        {
            PrimitiveTypeReference primitive => primitive.Name switch
            {
                "string" => literal is StringLiteralValue,
                "bool" => literal is BooleanLiteralValue,
                "float32" or "float64" => literal is FloatLiteralValue or IntegerLiteralValue,
                _ => literal is IntegerLiteralValue,
            },
            VoidTypeReference => false,
            _ => literal is StringLiteralValue or IntegerLiteralValue or FloatLiteralValue or BooleanLiteralValue,
        };

        if (!isCompatible)
        {
            _diagnostics.Add(new Diagnostic("CRIMSON110", $"Literal is not compatible with declared type '{DescribeType(declaredType)}'.", "error", source ?? literal.Source));
        }
    }

    private void ValidateInterfaceCycles()
    {
        var visited = new HashSet<InterfaceDeclaration>();
        var stack = new HashSet<InterfaceDeclaration>();

        foreach (var declaration in _resolvedBaseInterfaces.Keys)
        {
            VisitInterfaceCycle(declaration, visited, stack);
        }
    }

    private void VisitInterfaceCycle(InterfaceDeclaration declaration, HashSet<InterfaceDeclaration> visited, HashSet<InterfaceDeclaration> stack)
    {
        if (stack.Contains(declaration))
        {
            _diagnostics.Add(new Diagnostic("CRIMSON111", $"Interface composition cycle detected involving '{declaration.QualifiedName}'.", "error", declaration.Source));
            return;
        }

        if (!visited.Add(declaration))
        {
            return;
        }

        stack.Add(declaration);
        foreach (var baseInterface in _resolvedBaseInterfaces.GetValueOrDefault(declaration, []))
        {
            VisitInterfaceCycle(baseInterface, visited, stack);
        }

        stack.Remove(declaration);
    }

    private IEnumerable<Declaration> ResolveNamedType(NamedTypeReference namedType, IReadOnlyList<string> scope, InterfaceDeclaration? containingInterface = null)
    {
        if (namedType.IsGlobal)
        {
            return ResolveExact(namedType.Segments);
        }

        for (var index = scope.Count; index >= 0; index--)
        {
            var candidate = scope.Take(index).Concat(namedType.Segments).ToArray();
            var resolved = ResolveExact(candidate).ToArray();
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        if (containingInterface is not null)
        {
            var visitedBases = new HashSet<InterfaceDeclaration>();
            foreach (var baseInterface in _resolvedBaseInterfaces.GetValueOrDefault(containingInterface, []))
            {
                var resolved = ResolveNamedTypeFromBase(namedType, baseInterface, visitedBases).ToArray();
                if (resolved.Length > 0)
                {
                    return resolved;
                }
            }
        }

        return Array.Empty<Declaration>();
    }

    private IEnumerable<Declaration> ResolveNamedTypeFromBase(NamedTypeReference namedType, InterfaceDeclaration currentInterface, HashSet<InterfaceDeclaration> visitedBases)
    {
        if (!visitedBases.Add(currentInterface))
        {
            return Array.Empty<Declaration>();
        }

        var scope = currentInterface.NamespacePath.Concat(currentInterface.ContainingTypes).Concat([currentInterface.Name]).ToArray();
        for (var index = scope.Length; index >= 0; index--)
        {
            var candidate = scope.Take(index).Concat(namedType.Segments).ToArray();
            var resolved = ResolveExact(candidate).ToArray();
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        foreach (var baseInterface in _resolvedBaseInterfaces.GetValueOrDefault(currentInterface, []))
        {
            var resolved = ResolveNamedTypeFromBase(namedType, baseInterface, visitedBases).ToArray();
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        return Array.Empty<Declaration>();
    }

    private IEnumerable<Declaration> ResolveExact(IReadOnlyList<string> segments)
    {
        var key = string.Join(".", segments);
        if (_allDeclarationsByName.TryGetValue(key, out var matches))
        {
            return matches;
        }

        return Array.Empty<Declaration>();
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

    private static string DescribeType(TypeReference typeReference) =>
        typeReference switch
        {
            PrimitiveTypeReference primitive => primitive.Name,
            NamedTypeReference named => named.DisplayName,
            VoidTypeReference => "void",
            ListTypeReference list => $"list<{DescribeType(list.ElementType)}>",
            SetTypeReference set => $"set<{DescribeType(set.ElementType)}>",
            MapTypeReference map => $"map<{DescribeType(map.KeyType)}, {DescribeType(map.ValueType)}>",
            ArrayTypeReference array when array.Length is int length => $"{DescribeType(array.ElementType)}[{length}]",
            ArrayTypeReference array => $"{DescribeType(array.ElementType)}[]",
            _ => typeReference.GetType().Name,
        };

    private sealed record MemberOrigin(InterfaceDeclaration Origin, InterfaceMember Member);
}
