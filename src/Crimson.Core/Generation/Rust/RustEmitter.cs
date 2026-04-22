using System.Globalization;
using System.Text;
using Crimson.Core.Model;

namespace Crimson.Core.Generation.Rust;

public sealed record GeneratedRustTargetTree(
    IReadOnlyList<GeneratedFile> GeneratedFiles,
    IReadOnlyList<GeneratedFile> UserFiles);

public sealed class RustEmitter
{
    private CompilationSetModel _compilation = null!;
    private GenerationModelNavigator _model = null!;
    private RustTargetOptions _options = RustTargetOptions.Default;

    public void ValidateTargetSupport(CompilationSetModel compilation, RustTargetOptions options)
    {
        var diagnostics = new List<Diagnostic>();

        if (options.Support.Provider == RustSupportProvider.Generated &&
            !string.Equals(options.Support.ModulePath, "crate::generated::crimson_support", StringComparison.Ordinal))
        {
            diagnostics.Add(new Diagnostic(
                "CRIMSON301",
                $"The rust target currently requires generated support to use modulePath 'crate::generated::crimson_support', but found '{options.Support.ModulePath}'.",
                "error",
                null));
        }

        foreach (var enumDeclaration in GenerationModelNavigator.EnumerateDeclarations(compilation.Files.SelectMany(static file => file.Declarations)).OfType<EnumDeclaration>())
        {
            if (enumDeclaration.AssociatedValueType is null && enumDeclaration.Members.All(static member => member.AssociatedValue is null))
            {
                continue;
            }

            if (!SupportsEnumAssociatedValues(enumDeclaration))
            {
                diagnostics.Add(new Diagnostic(
                    "CRIMSON302",
                    $"The rust target only supports integer-backed enum associated values: '{enumDeclaration.QualifiedName}'.",
                    "error",
                    enumDeclaration.Source));
            }
        }

        if (diagnostics.Count > 0)
        {
            throw new DiagnosticException(diagnostics);
        }
    }

    public GeneratedRustTargetTree Emit(CompilationSetModel compilation, RustTargetOptions options)
    {
        _compilation = compilation;
        _model = new GenerationModelNavigator(compilation);
        _options = options;

        var generatedFiles = new List<GeneratedFile>();
        var userFiles = new List<GeneratedFile>();
        var generatedModules = new HashSet<string>(StringComparer.Ordinal);
        var userModules = new HashSet<string>(StringComparer.Ordinal);

        foreach (var declaration in compilation.Files.SelectMany(static file => file.Declarations))
        {
            EmitDeclaration(declaration, generatedFiles, userFiles, generatedModules, userModules);
        }

        if (_options.Support.Provider == RustSupportProvider.Generated)
        {
            generatedModules.Add("crimson_support");
            generatedFiles.Add(new GeneratedFile("crimson_support.rs", RenderSupportModule()));
        }

        generatedFiles.Add(new GeneratedFile("mod.rs", RenderRootModule(generatedModules)));
        userFiles.Add(new GeneratedFile("mod.rs", RenderRootModule(userModules)));

        return new GeneratedRustTargetTree(generatedFiles, userFiles);
    }

    private void EmitDeclaration(
        Declaration declaration,
        List<GeneratedFile> generatedFiles,
        List<GeneratedFile> userFiles,
        HashSet<string> generatedModules,
        HashSet<string> userModules)
    {
        switch (declaration)
        {
            case NamespaceDeclaration namespaceDeclaration:
                foreach (var member in namespaceDeclaration.Members)
                {
                    EmitDeclaration(member, generatedFiles, userFiles, generatedModules, userModules);
                }

                break;

            case InterfaceDeclaration interfaceDeclaration:
            {
                var moduleName = GetModuleName(interfaceDeclaration);
                generatedModules.Add(moduleName);
                generatedFiles.Add(new GeneratedFile($"{moduleName}.rs", RenderInterfaceModule(interfaceDeclaration)));

                if (!interfaceDeclaration.IsAbstract)
                {
                    userModules.Add(moduleName);
                    userFiles.Add(new GeneratedFile($"{moduleName}.rs", RenderUserModule(interfaceDeclaration)));
                }

                foreach (var nested in interfaceDeclaration.NestedDeclarations)
                {
                    EmitDeclaration(nested, generatedFiles, userFiles, generatedModules, userModules);
                }

                break;
            }

            case StructDeclaration structDeclaration:
            {
                var moduleName = GetModuleName(structDeclaration);
                generatedModules.Add(moduleName);
                generatedFiles.Add(new GeneratedFile($"{moduleName}.rs", RenderStructModule(structDeclaration)));
                break;
            }

            case EnumDeclaration enumDeclaration:
            {
                var moduleName = GetModuleName(enumDeclaration);
                generatedModules.Add(moduleName);
                generatedFiles.Add(new GeneratedFile($"{moduleName}.rs", RenderEnumModule(enumDeclaration)));
                break;
            }

            case ConstantDeclaration constantDeclaration:
            {
                var moduleName = GetModuleName(constantDeclaration);
                generatedModules.Add(moduleName);
                generatedFiles.Add(new GeneratedFile($"{moduleName}.rs", RenderConstantModule(constantDeclaration)));
                break;
            }
        }
    }

    private string RenderInterfaceModule(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendModuleHeader(builder);

        builder.Append(RenderTrait(declaration));

        if (!declaration.IsAbstract)
        {
            builder.AppendLine();
            builder.Append(RenderGeneratedState(declaration));
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderTrait(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var baseTraits = _model.GetDirectBaseInterfaces(declaration)
            .Select(baseInterface => RenderTraitPath(baseInterface))
            .ToArray();
        var traitHeader = baseTraits.Length == 0
            ? $"pub trait {GetTraitName(declaration)}"
            : $"pub trait {GetTraitName(declaration)}: {string.Join(" + ", baseTraits)}";

        builder.AppendLine(traitHeader);
        builder.AppendLine("{");

        foreach (var constantMember in declaration.Members.OfType<ConstantMemberDeclaration>())
        {
            builder.AppendLine($"    const {constantMember.Name.ToUpperInvariant()}: {RenderConstantType(constantMember.Type, declaration)} = {RenderConstantValueExpression(constantMember.Value ?? throw new InvalidOperationException($"Constant member '{declaration.QualifiedName}.{constantMember.Name}' must declare a value."), constantMember.Type, declaration)};");
        }

        if (declaration.Members.OfType<ConstantMemberDeclaration>().Any() &&
            declaration.Members.Any(static member => member is not ConstantMemberDeclaration))
        {
            builder.AppendLine();
        }

        foreach (var member in declaration.Members)
        {
            switch (member)
            {
                case ValueMemberDeclaration valueMember when !valueMember.IsInternal:
                    builder.AppendLine($"    fn get_{valueMember.Name}(&self) -> {RenderType(valueMember.Type, declaration)};");
                    if (!valueMember.IsReadonly)
                    {
                        builder.AppendLine($"    fn set_{valueMember.Name}(&mut self, value: {RenderType(valueMember.Type, declaration)});");
                    }

                    builder.AppendLine();
                    break;

                case MethodMemberDeclaration methodMember:
                    builder.AppendLine($"    fn {methodMember.Name}(&mut self{RenderParameters(methodMember.Parameters, declaration)}{RenderReturnType(methodMember.ReturnType, declaration)};");
                    builder.AppendLine();
                    break;
            }
        }

        var refinements = GetInterfaceRefinements(declaration).ToArray();
        foreach (var refinement in refinements)
        {
            builder.AppendLine($"    fn as_{GetRefinementHelperName(refinement)}(&self) -> Option<&dyn {RenderTraitPath(refinement)}>");
            builder.AppendLine("    {");
            builder.AppendLine("        None");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine($"    fn as_{GetRefinementHelperName(refinement)}_mut(&mut self) -> Option<&mut dyn {RenderTraitPath(refinement)}>");
            builder.AppendLine("    {");
            builder.AppendLine("        None");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private string RenderGeneratedState(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        var effectiveValues = _model.GetEffectiveMembers(declaration).OfType<ValueMemberDeclaration>().ToArray();

        builder.AppendLine("#[derive(Clone)]");
        builder.AppendLine($"pub struct {GetGeneratedStateName(declaration)}");
        builder.AppendLine("{");

        foreach (var valueMember in effectiveValues)
        {
            builder.AppendLine($"    {valueMember.Name}: {RenderType(valueMember.Type, declaration)},");
        }

        if (effectiveValues.Length == 0)
        {
            builder.AppendLine("    _private: (),");
        }

        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"impl Default for {GetGeneratedStateName(declaration)}");
        builder.AppendLine("{");
        builder.AppendLine("    fn default() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self");
        builder.AppendLine("        {");
        foreach (var valueMember in effectiveValues)
        {
            builder.AppendLine($"            {valueMember.Name}: {RenderDefaultValue(valueMember.Type, valueMember.DefaultValue, declaration)},");
        }
        if (effectiveValues.Length == 0)
        {
            builder.AppendLine("            _private: (),");
        }
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"impl {GetGeneratedStateName(declaration)}");
        builder.AppendLine("{");
        builder.AppendLine("    pub fn new() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self::default()");
        builder.AppendLine("    }");

        foreach (var valueMember in effectiveValues)
        {
            builder.AppendLine();
            builder.AppendLine($"    pub fn get_{valueMember.Name}(&self) -> {RenderType(valueMember.Type, declaration)}");
            builder.AppendLine("    {");
            builder.AppendLine($"        self.{valueMember.Name}.clone()");
            builder.AppendLine("    }");

            builder.AppendLine();
            builder.AppendLine($"    pub fn set_{valueMember.Name}(&mut self, value: {RenderType(valueMember.Type, declaration)})");
            builder.AppendLine("    {");
            builder.AppendLine($"        self.{valueMember.Name} = value;");
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private string RenderUserModule(InterfaceDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendModuleHeader(builder);

        builder.AppendLine("#[derive(Clone, Default)]");
        builder.AppendLine($"pub struct {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    generated: {RenderGeneratedStatePath(declaration)},");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"impl {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine("    pub fn new() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self::default()");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        var implementedContracts = _model.GetAllBaseInterfaces(declaration)
            .Concat([declaration])
            .ToArray();

        foreach (var contract in implementedContracts)
        {
            builder.AppendLine();
            builder.AppendLine($"impl {RenderTraitPath(contract)} for {declaration.Name}");
            builder.AppendLine("{");

            foreach (var member in contract.Members)
            {
                switch (member)
                {
                    case ValueMemberDeclaration valueMember when !valueMember.IsInternal:
                        builder.AppendLine($"    fn get_{valueMember.Name}(&self) -> {RenderType(valueMember.Type, declaration)}");
                        builder.AppendLine("    {");
                        builder.AppendLine($"        self.generated.get_{valueMember.Name}()");
                        builder.AppendLine("    }");
                        if (!valueMember.IsReadonly)
                        {
                            builder.AppendLine();
                            builder.AppendLine($"    fn set_{valueMember.Name}(&mut self, value: {RenderType(valueMember.Type, declaration)})");
                            builder.AppendLine("    {");
                            builder.AppendLine($"        self.generated.set_{valueMember.Name}(value);");
                            builder.AppendLine("    }");
                        }

                        builder.AppendLine();
                        break;

                case MethodMemberDeclaration methodMember:
                    builder.AppendLine($"    fn {methodMember.Name}(&mut self{RenderParameters(methodMember.Parameters, declaration)}{RenderReturnType(methodMember.ReturnType, declaration)}");
                    builder.AppendLine("    {");
                    builder.AppendLine($"        unimplemented!(\"{contract.QualifiedName}.{methodMember.Name}\");");
                    builder.AppendLine("    }");
                    builder.AppendLine();
                    break;
                }
            }

            foreach (var refinement in GetImplementedRefinements(contract, declaration))
            {
                builder.AppendLine($"    fn as_{GetRefinementHelperName(refinement)}(&self) -> Option<&dyn {RenderTraitPath(refinement)}>");
                builder.AppendLine("    {");
                builder.AppendLine("        Some(self)");
                builder.AppendLine("    }");
                builder.AppendLine();
                builder.AppendLine($"    fn as_{GetRefinementHelperName(refinement)}_mut(&mut self) -> Option<&mut dyn {RenderTraitPath(refinement)}>");
                builder.AppendLine("    {");
                builder.AppendLine("        Some(self)");
                builder.AppendLine("    }");
                builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderStructModule(StructDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendModuleHeader(builder);

        builder.AppendLine("#[derive(Clone, Debug)]");
        builder.AppendLine($"pub struct {declaration.Name}");
        builder.AppendLine("{");
        foreach (var valueMember in declaration.Members.OfType<ValueMemberDeclaration>())
        {
            builder.AppendLine($"    pub {valueMember.Name}: {RenderType(valueMember.Type, declaration)},");
        }
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"impl Default for {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine("    fn default() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self");
        builder.AppendLine("        {");
        foreach (var valueMember in declaration.Members.OfType<ValueMemberDeclaration>())
        {
            builder.AppendLine($"            {valueMember.Name}: {RenderDefaultValue(valueMember.Type, valueMember.DefaultValue, declaration)},");
        }
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"impl {declaration.Name}");
        builder.AppendLine("{");

        foreach (var constantMember in declaration.Members.OfType<ConstantMemberDeclaration>())
        {
            builder.AppendLine($"    pub const {constantMember.Name.ToUpperInvariant()}: {RenderConstantType(constantMember.Type, declaration)} = {RenderConstantValueExpression(constantMember.Value ?? throw new InvalidOperationException($"Constant member '{declaration.QualifiedName}.{constantMember.Name}' must declare a value."), constantMember.Type, declaration)};");
        }

        if (declaration.Members.OfType<ConstantMemberDeclaration>().Any())
        {
            builder.AppendLine();
        }

        builder.AppendLine("    pub fn new() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self::default()");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderEnumModule(EnumDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendModuleHeader(builder);

        if (declaration.AssociatedValueType is PrimitiveTypeReference primitive &&
            IsIntegralPrimitive(primitive.Name))
        {
            builder.AppendLine($"#[repr({RenderRustRepr(primitive.Name)})]");
        }
        builder.AppendLine("#[derive(Clone, Copy, Debug, PartialEq, Eq, Default)]");
        builder.AppendLine($"pub enum {declaration.Name}");
        builder.AppendLine("{");
        for (var index = 0; index < declaration.Members.Count; index++)
        {
            var member = declaration.Members[index];
            var defaultAttribute = index == 0 ? "    #[default]" + Environment.NewLine : string.Empty;
            builder.Append(defaultAttribute);
            var valueSuffix = member.AssociatedValue is not null
                ? $" = {RenderEnumAssociatedValue(member.AssociatedValue)}"
                : string.Empty;
            builder.AppendLine($"    {member.Name}{valueSuffix},");
        }
        builder.AppendLine("}");

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderConstantModule(ConstantDeclaration declaration)
    {
        var builder = new StringBuilder();
        AppendModuleHeader(builder);

        builder.AppendLine($"pub struct {declaration.Name};");
        builder.AppendLine();
        builder.AppendLine($"impl {declaration.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    pub const VALUE: {RenderConstantType(declaration.Type, declaration)} = {RenderConstantValueExpression(declaration.Value ?? throw new InvalidOperationException($"Constant '{declaration.QualifiedName}' must declare a value."), declaration.Type, declaration)};");
        builder.AppendLine("}");

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderSupportModule()
    {
        var builder = new StringBuilder();

        if (_options.Support.Profile == RustSupportProfile.NoStd)
        {
            builder.AppendLine("extern crate alloc;");
            builder.AppendLine();
            builder.AppendLine("use alloc::boxed::Box;");
            builder.AppendLine("use alloc::collections::BTreeMap;");
            builder.AppendLine("use alloc::rc::Rc;");
            builder.AppendLine("use alloc::string::String as AllocString;");
            builder.AppendLine("use alloc::vec::Vec;");
        }
        else
        {
            builder.AppendLine("use core::cell::RefCell;");
            builder.AppendLine("use std::boxed::Box;");
            builder.AppendLine("use std::collections::BTreeMap;");
            builder.AppendLine("use std::rc::Rc;");
            builder.AppendLine("use std::string::String as StdString;");
            builder.AppendLine("use std::vec::Vec;");
        }

        if (_options.Support.Profile == RustSupportProfile.NoStd)
        {
            builder.AppendLine("use core::cell::RefCell;");
        }

        builder.AppendLine();
        builder.AppendLine(_options.Support.Profile == RustSupportProfile.NoStd
            ? "pub type String = AllocString;"
            : "pub type String = StdString;");
        builder.AppendLine("pub type Optional<T> = core::option::Option<T>;");
        builder.AppendLine("pub type List<T> = Vec<T>;");
        builder.AppendLine("pub type Map<K, V> = BTreeMap<K, V>;");
        builder.AppendLine("pub type InterfaceHandle<T> = core::option::Option<Rc<RefCell<Box<T>>>>;");
        builder.AppendLine();
        builder.AppendLine("#[derive(Clone, Debug, Default, PartialEq, Eq)]");
        builder.AppendLine("pub struct Set<T>(pub Vec<T>);");
        builder.AppendLine();
        builder.AppendLine("impl<T> Set<T>");
        builder.AppendLine("{");
        builder.AppendLine("    pub fn new() -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self(Vec::new())");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    pub fn from_items(items: Vec<T>) -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self(items)");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("impl<T> From<Vec<T>> for Set<T>");
        builder.AppendLine("{");
        builder.AppendLine("    fn from(value: Vec<T>) -> Self");
        builder.AppendLine("    {");
        builder.AppendLine("        Self(value)");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("impl<T> Set<T>");
        builder.AppendLine("{");
        builder.AppendLine("    pub fn iter(&self) -> core::slice::Iter<'_, T>");
        builder.AppendLine("    {");
        builder.AppendLine("        self.0.iter()");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("impl<T: PartialEq> Set<T>");
        builder.AppendLine("{");
        builder.AppendLine("    pub fn contains(&self, value: &T) -> bool");
        builder.AppendLine("    {");
        builder.AppendLine("        self.0.contains(value)");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    pub fn insert(&mut self, value: T)");
        builder.AppendLine("    {");
        builder.AppendLine("        if !self.contains(&value)");
        builder.AppendLine("        {");
        builder.AppendLine("            self.0.push(value);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("pub fn interface_handle<T: ?Sized>(value: Box<T>) -> InterfaceHandle<T>");
        builder.AppendLine("{");
        builder.AppendLine("    Some(Rc::new(RefCell::new(value)))");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("pub fn with_interface<T: ?Sized, TResult>(handle: &InterfaceHandle<T>, f: impl FnOnce(&T) -> TResult) -> Option<TResult>");
        builder.AppendLine("{");
        builder.AppendLine("    handle.as_ref().map(|value|");
        builder.AppendLine("    {");
        builder.AppendLine("        let borrowed = value.borrow();");
        builder.AppendLine("        f(&**borrowed)");
        builder.AppendLine("    })");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("pub fn with_interface_mut<T: ?Sized, TResult>(handle: &InterfaceHandle<T>, f: impl FnOnce(&mut T) -> TResult) -> Option<TResult>");
        builder.AppendLine("{");
        builder.AppendLine("    handle.as_ref().map(|value|");
        builder.AppendLine("    {");
        builder.AppendLine("        let mut borrowed = value.borrow_mut();");
        builder.AppendLine("        f(&mut **borrowed)");
        builder.AppendLine("    })");
        builder.AppendLine("}");

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private string RenderRootModule(IEnumerable<string> modules)
    {
        var builder = new StringBuilder();
        foreach (var module in modules.OrderBy(static module => module, StringComparer.Ordinal))
        {
            builder.AppendLine($"pub mod {module};");
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private void AppendModuleHeader(StringBuilder builder)
    {
        builder.AppendLine("#![allow(clippy::module_name_repetitions)]");
        builder.AppendLine();
    }

    private string RenderParameters(IEnumerable<MethodParameter> parameters, Declaration scope) =>
        string.Concat(parameters.Select(parameter => $", {parameter.Name}: {RenderType(parameter.Type, scope)}")) + ")";

    private string RenderReturnType(TypeReference type, Declaration scope) =>
        type is VoidTypeReference ? string.Empty : $" -> {RenderType(type, scope)}";

    private string RenderType(TypeReference type, Declaration scope) =>
        ApplyNullability(RenderTypeCore(type, scope), type, scope);

    private string RenderTypeCore(TypeReference type, Declaration scope) =>
        type switch
        {
            VoidTypeReference => "()",
            PrimitiveTypeReference primitive => primitive.Name switch
            {
                "bool" => "bool",
                "string" => $"{SupportModulePath}::String",
                "int8" => "i8",
                "uint8" => "u8",
                "int16" => "i16",
                "uint16" => "u16",
                "int32" => "i32",
                "uint32" => "u32",
                "int64" => "i64",
                "uint64" => "u64",
                "float32" => "f32",
                "float64" => "f64",
                _ => primitive.Name,
            },
            NamedTypeReference named => RenderNamedType(named, scope),
            ListTypeReference list => $"{SupportModulePath}::List<{RenderType(list.ElementType, scope)}>",
            SetTypeReference set => $"{SupportModulePath}::Set<{RenderType(set.ElementType, scope)}>",
            MapTypeReference map => $"{SupportModulePath}::Map<{RenderType(map.KeyType, scope)}, {RenderType(map.ValueType, scope)}>",
            ArrayTypeReference array when array.Length is int length => $"[{RenderType(array.ElementType, scope)}; {length}]",
            ArrayTypeReference array => $"{SupportModulePath}::List<{RenderType(array.ElementType, scope)}>",
            _ => throw new NotSupportedException($"Unsupported type: {type.GetType().Name}"),
        };

    private string ApplyNullability(string renderedType, TypeReference type, Declaration scope)
    {
        if (!type.IsNullable || type is VoidTypeReference)
        {
            return renderedType;
        }

        if (type is NamedTypeReference named &&
            _model.ResolveNamedDeclaration(named, scope) is InterfaceDeclaration)
        {
            return renderedType;
        }

        return $"{SupportModulePath}::Optional<{renderedType}>";
    }

    private string RenderNamedType(NamedTypeReference named, Declaration scope)
    {
        var resolved = _model.ResolveNamedDeclaration(named, scope);
        return resolved switch
        {
            InterfaceDeclaration interfaceDeclaration => $"{SupportModulePath}::InterfaceHandle<dyn {RenderTraitPath(interfaceDeclaration)}>",
            StructDeclaration structDeclaration => RenderDeclarationPath(structDeclaration, structDeclaration.Name),
            EnumDeclaration enumDeclaration => RenderDeclarationPath(enumDeclaration, enumDeclaration.Name),
            ConstantDeclaration constantDeclaration => RenderDeclarationPath(constantDeclaration, constantDeclaration.Name),
            Declaration declaration => RenderDeclarationPath(declaration, declaration.Name),
            _ => string.Join("::", named.Segments),
        };
    }

    private string RenderValueExpression(ValueExpression expression, TypeReference declaredType, Declaration scope) =>
        expression switch
        {
            LiteralValueExpression literalExpression => RenderLiteral(literalExpression.Value, declaredType),
            NamedValueExpression namedValue => RenderNamedValueExpression(namedValue, declaredType, scope),
            _ => throw new NotSupportedException($"Unsupported value expression: {expression.GetType().Name}"),
        };

    private string RenderConstantValueExpression(ValueExpression expression, TypeReference declaredType, Declaration scope) =>
        expression switch
        {
            LiteralValueExpression literalExpression => RenderConstantLiteral(literalExpression.Value, declaredType),
            NamedValueExpression namedValue => RenderNamedValueExpression(namedValue, declaredType, scope),
            _ => throw new NotSupportedException($"Unsupported constant value expression: {expression.GetType().Name}"),
        };

    private string RenderNamedValueExpression(NamedValueExpression namedValue, TypeReference declaredType, Declaration scope)
    {
        if (declaredType is NamedTypeReference namedType &&
            _model.ResolveNamedDeclaration(namedType, scope) is EnumDeclaration targetEnum)
        {
            var enumDeclaration = _model.ResolveEnumReference(namedValue, targetEnum, scope) ?? targetEnum;
            return $"{RenderDeclarationPath(enumDeclaration, enumDeclaration.Name)}::{namedValue.Segments[^1]}";
        }

        return string.Join("::", namedValue.Segments);
    }

    private string RenderLiteral(LiteralValue literal, TypeReference declaredType) =>
        literal switch
        {
            IntegerLiteralValue integer when IsFloatPrimitive(declaredType) => integer.Value.ToString(CultureInfo.InvariantCulture) + ".0",
            IntegerLiteralValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            FloatLiteralValue floating => RenderFloatLiteral(floating.Value),
            StringLiteralValue text => $"{SupportModulePath}::String::from(\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\")",
            BooleanLiteralValue boolean => boolean.Value ? "true" : "false",
            _ => throw new NotSupportedException($"Unsupported literal: {literal.GetType().Name}"),
        };

    private static string RenderConstantLiteral(LiteralValue literal, TypeReference declaredType) =>
        literal switch
        {
            IntegerLiteralValue integer when IsFloatPrimitive(declaredType) => integer.Value.ToString(CultureInfo.InvariantCulture) + ".0",
            StringLiteralValue text when declaredType is PrimitiveTypeReference { Name: "string" } =>
                $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            IntegerLiteralValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            FloatLiteralValue floating => RenderFloatLiteral(floating.Value),
            StringLiteralValue text => $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            BooleanLiteralValue boolean => boolean.Value ? "true" : "false",
            _ => throw new NotSupportedException($"Unsupported constant literal: {literal.GetType().Name}"),
        };

    private string RenderDefaultValue(TypeReference type, ValueExpression? valueExpression, Declaration scope) =>
        valueExpression is not null ? RenderValueExpression(valueExpression, type, scope) : DefaultLiteral(type, scope);

    private string DefaultLiteral(TypeReference type, Declaration scope) =>
        type switch
        {
            PrimitiveTypeReference primitive when primitive.IsNullable => "None",
            PrimitiveTypeReference primitive when primitive.Name == "string" => $"{SupportModulePath}::String::new()",
            PrimitiveTypeReference primitive when primitive.Name == "bool" => "false",
            PrimitiveTypeReference primitive when IsFloatPrimitive(primitive) => "0.0",
            PrimitiveTypeReference => "0",
            NamedTypeReference named when _model.ResolveNamedDeclaration(named, scope) is InterfaceDeclaration => "None",
            NamedTypeReference named when named.IsNullable => "None",
            NamedTypeReference named => $"{RenderNamedType(named, scope)}::default()",
            ListTypeReference list when list.IsNullable => "None",
            ListTypeReference => $"{SupportModulePath}::List::new()",
            SetTypeReference set when set.IsNullable => "None",
            SetTypeReference => $"{SupportModulePath}::Set::new()",
            MapTypeReference map when map.IsNullable => "None",
            MapTypeReference => $"{SupportModulePath}::Map::new()",
            ArrayTypeReference array when array.IsNullable => "None",
            ArrayTypeReference array when array.Length is int _ => $"{RenderTypeCore(array, scope)}::default()",
            ArrayTypeReference => $"{SupportModulePath}::List::new()",
            _ => "Default::default()",
        };

    private string RenderConstantType(TypeReference type, Declaration scope) =>
        type switch
        {
            PrimitiveTypeReference primitive when primitive.Name == "string" => "&'static str",
            _ => RenderType(type, scope),
        };

    private string RenderTraitPath(InterfaceDeclaration declaration) =>
        RenderDeclarationPath(declaration, GetTraitName(declaration));

    private string RenderGeneratedStatePath(InterfaceDeclaration declaration) =>
        RenderDeclarationPath(declaration, GetGeneratedStateName(declaration));

    private string RenderDeclarationPath(Declaration declaration, string localName) =>
        $"crate::generated::{GetModuleName(declaration)}::{localName}";

    private static string GetTraitName(InterfaceDeclaration declaration) =>
        declaration.Name + "Contract";

    private static string GetGeneratedStateName(InterfaceDeclaration declaration) =>
        declaration.Name + "Generated";

    private static string GetModuleName(Declaration declaration) =>
        string.Join("__",
            declaration.NamespacePath
                .Concat(declaration.ContainingTypes)
                .Concat([declaration.Name])
                .Select(ToSnakeCase));

    private string RenderEnumAssociatedValue(ValueExpression valueExpression) =>
        valueExpression switch
        {
            LiteralValueExpression literalExpression when literalExpression.Value is IntegerLiteralValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("Rust enum associated values currently support integer literals only."),
        };

    private static bool SupportsEnumAssociatedValues(EnumDeclaration declaration) =>
        declaration.AssociatedValueType is PrimitiveTypeReference primitive &&
        IsIntegralPrimitive(primitive.Name) &&
        declaration.Members.All(static member => member.AssociatedValue is null ||
            member.AssociatedValue is LiteralValueExpression { Value: IntegerLiteralValue });

    private static bool IsIntegralPrimitive(string name) =>
        name is "int8" or "uint8" or "int16" or "uint16" or "int32" or "uint32" or "int64" or "uint64";

    private static string RenderRustRepr(string primitiveName) =>
        primitiveName switch
        {
            "int8" => "i8",
            "uint8" => "u8",
            "int16" => "i16",
            "uint16" => "u16",
            "int32" => "i32",
            "uint32" => "u32",
            "int64" => "i64",
            "uint64" => "u64",
            _ => throw new InvalidOperationException($"Unsupported rust repr primitive '{primitiveName}'."),
        };

    private string SupportModulePath =>
        _options.Support.Provider == RustSupportProvider.Generated
            ? "crate::generated::crimson_support"
            : _options.Support.ModulePath;

    private IEnumerable<InterfaceDeclaration> GetInterfaceRefinements(InterfaceDeclaration declaration) =>
        GenerationModelNavigator.EnumerateDeclarations(_compilation.Files.SelectMany(static file => file.Declarations))
            .OfType<InterfaceDeclaration>()
            .Where(candidate =>
                !string.Equals(candidate.QualifiedName, declaration.QualifiedName, StringComparison.Ordinal) &&
                IsDescendantInterface(candidate, declaration))
            .OrderBy(GetModuleName, StringComparer.Ordinal);

    private IEnumerable<InterfaceDeclaration> GetImplementedRefinements(InterfaceDeclaration contract, InterfaceDeclaration concreteDeclaration)
    {
        var implementedContracts = _model.GetAllBaseInterfaces(concreteDeclaration)
            .Concat([concreteDeclaration])
            .Select(static declaration => declaration.QualifiedName)
            .ToHashSet(StringComparer.Ordinal);

        return GetInterfaceRefinements(contract)
            .Where(refinement => implementedContracts.Contains(refinement.QualifiedName));
    }

    private bool IsDescendantInterface(InterfaceDeclaration candidate, InterfaceDeclaration declaration) =>
        _model.GetAllBaseInterfaces(candidate)
            .Any(baseInterface => string.Equals(baseInterface.QualifiedName, declaration.QualifiedName, StringComparison.Ordinal));

    private static string RenderFloatLiteral(double value)
    {
        var rendered = value.ToString("R", CultureInfo.InvariantCulture);
        return rendered.IndexOfAny(['.', 'e', 'E']) >= 0
            ? rendered
            : rendered + ".0";
    }

    private static bool IsFloatPrimitive(TypeReference declaredType) =>
        declaredType is PrimitiveTypeReference { Name: "float32" or "float64" };

    private static string GetRefinementHelperName(InterfaceDeclaration declaration) =>
        GetModuleName(declaration);

    private static string ToSnakeCase(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < identifier.Length; index++)
        {
            var character = identifier[index];
            if (char.IsUpper(character))
            {
                if (index > 0 && identifier[index - 1] != '_')
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
