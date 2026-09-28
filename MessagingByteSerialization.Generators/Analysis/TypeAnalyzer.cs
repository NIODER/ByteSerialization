using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MessagingByteSerialization.Generators.Model;

namespace MessagingByteSerialization.Generators.Analysis;

/// <summary>
/// Walks a <c>[ByteSerializable]</c> type's public instance fields/properties and produces a
/// <see cref="TypeModel"/> ready for emission, or <c>null</c> (with diagnostics reported into
/// <paramref name="diagnostics"/>) when the type cannot be generated.
///
/// Nested <c>[ByteSerializable]</c> members are classified recursively through the same
/// <see cref="ClassifyType"/> core so that embedding a type requires it to independently qualify for
/// generation too (partial, non-generic, top-level, resolvable members and constructor) - otherwise the
/// outer type would emit calls to a <c>ToBytes</c>/<c>FromBytes</c> pair that will never exist.
/// </summary>
internal static class TypeAnalyzer
{
    private static readonly SymbolDisplayFormat QualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public static TypeModel? Analyze(GeneratorAttributeSyntaxContext context, List<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        KnownSymbols? known = KnownSymbols.Create(context.SemanticModel.Compilation);

        if (known is null)
        {
            return null;
        }

        var cache = new Dictionary<INamedTypeSymbol, ClassifiedShape?>(SymbolEqualityComparer.Default);
        var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        Location rootLocation = context.TargetNode.GetLocation();

        ClassifiedShape? shape = ClassifyType(typeSymbol, known, cache, visiting, diagnostics, rootLocation, cancellationToken);

        if (shape is null)
        {
            return null;
        }

        string ns = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : string.Empty;

        string header = BuildDeclarationHeader((TypeDeclarationSyntax)context.TargetNode);
        ushort typeHash = StableHash.Compute(typeSymbol.ToDisplayString(QualifiedFormat));

        return new TypeModel
        {
            Namespace = ns,
            Name = typeSymbol.Name,
            DeclarationHeader = header,
            TypeHash = typeHash,
            Construction = shape.Construction,
            Members = shape.Members,
        };
    }

    private static ClassifiedShape? ClassifyType(
        INamedTypeSymbol typeSymbol,
        KnownSymbols known,
        Dictionary<INamedTypeSymbol, ClassifiedShape?> cache,
        HashSet<INamedTypeSymbol> visiting,
        List<Diagnostic> diagnostics,
        Location referencingLocation,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(typeSymbol, out ClassifiedShape? cached))
        {
            return cached;
        }

        if (visiting.Contains(typeSymbol))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.CircularFixedSizeReference, referencingLocation, typeSymbol.ToDisplayString()));
            return null;
        }

        SyntaxReference? syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        TypeDeclarationSyntax? typeDecl = syntaxRef?.GetSyntax(cancellationToken) as TypeDeclarationSyntax;
        Location declLocation = typeDecl?.Identifier.GetLocation() ?? typeSymbol.Locations.FirstOrDefault() ?? Location.None;

        if (typeDecl is null || !typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TypeMustBePartial, declLocation, typeSymbol.ToDisplayString()));
            cache[typeSymbol] = null;
            return null;
        }

        if (typeSymbol.Arity > 0)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.GenericTypeNotSupported, declLocation, typeSymbol.ToDisplayString()));
            cache[typeSymbol] = null;
            return null;
        }

        if (typeSymbol.ContainingType is not null)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.NestedTypeNotSupported, declLocation, typeSymbol.ToDisplayString()));
            cache[typeSymbol] = null;
            return null;
        }

        visiting.Add(typeSymbol);

        try
        {
            List<(string Name, ITypeSymbol Type, ISymbol Symbol)> rawMembers = GetSerializableMembers(typeSymbol);
            ImmutableArray<MemberModel>.Builder members = ImmutableArray.CreateBuilder<MemberModel>(rawMembers.Count);
            bool anyUnsupported = false;

            foreach ((string name, ITypeSymbol type, ISymbol symbol) in rawMembers)
            {
                Location memberLocation = symbol.Locations.FirstOrDefault() ?? declLocation;

                // Ignored members bypass classification entirely - their type is never inspected, so it
                // does not need to be a supported (or even serializable) type, and nothing else about the
                // member (e.g. a [ByteSerializableAs<T>] also present on it) is relevant.
                if (known.HasByteSerializableIgnoreAttribute(symbol))
                {
                    members.Add(new MemberModel
                    {
                        Name = name,
                        IsIgnored = true,
                        OriginalTypeDisplayName = type.ToDisplayString(QualifiedFormat),
                    });
                    continue;
                }

                ITypeSymbol? castTarget = known.TryGetByteSerializableAsTarget(symbol);

                MemberModel? member = castTarget is null
                    ? ClassifyMember(name, type, known, cache, visiting, diagnostics, memberLocation, cancellationToken)
                    : ClassifyMemberWithCast(name, type, castTarget, known, cache, visiting, diagnostics, memberLocation, cancellationToken);

                if (member is null)
                {
                    // [ByteSerializableAs<T>] failures already reported a specific MBS009/MBS010 -
                    // the generic "unsupported type" message would just be confusing on top of that,
                    // since the member's own declared type is often perfectly fine on its own.
                    if (castTarget is null)
                    {
                        diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnsupportedMemberType, memberLocation, name, typeSymbol.ToDisplayString(), type.ToDisplayString()));
                    }

                    anyUnsupported = true;
                    continue;
                }

                members.Add(member);
            }

            if (anyUnsupported)
            {
                cache[typeSymbol] = null;
                return null;
            }

            ImmutableArray<MemberModel> membersImmutable = members.MoveToImmutable();
            ConstructionStrategy construction;

            if (TryFindPositionalConstructor(typeSymbol, rawMembers))
            {
                construction = ConstructionStrategy.PositionalConstructor;
            }
            else if (CanUseParameterlessConstructor(typeSymbol, rawMembers))
            {
                construction = ConstructionStrategy.ParameterlessConstructorWithInitializer;
            }
            else
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.NoSuitableConstructor, declLocation, typeSymbol.ToDisplayString()));
                cache[typeSymbol] = null;
                return null;
            }

            bool isFixedSize = membersImmutable.All(static m => !m.IsVariableSize);
            int fixedSize = isFixedSize ? membersImmutable.Where(static m => !m.IsIgnored).Sum(static m => m.Scalar!.FixedSize) : 0;

            var shape = new ClassifiedShape
            {
                IsFixedSize = isFixedSize,
                FixedSize = fixedSize,
                Construction = construction,
                Members = membersImmutable,
            };

            cache[typeSymbol] = shape;
            return shape;
        }
        finally
        {
            visiting.Remove(typeSymbol);
        }
    }

    private static MemberModel? ClassifyMember(
        string name,
        ITypeSymbol type,
        KnownSymbols known,
        Dictionary<INamedTypeSymbol, ClassifiedShape?> cache,
        HashSet<INamedTypeSymbol> visiting,
        List<Diagnostic> diagnostics,
        Location memberLocation,
        CancellationToken cancellationToken)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return new MemberModel { Name = name, Shape = MemberShape.String };
        }

        if (type is IArrayTypeSymbol { Rank: 1 } arrayType)
        {
            ItemTypeModel? element = ClassifyItemType(arrayType.ElementType, known, cache, visiting, diagnostics, memberLocation, cancellationToken);
            return element is null ? null : new MemberModel { Name = name, Shape = MemberShape.Array, Element = element };
        }

        if (type is INamedTypeSymbol { IsGenericType: true } listCandidate && SymbolEqualityComparer.Default.Equals(listCandidate.ConstructedFrom, known.ListOfT))
        {
            ItemTypeModel? element = ClassifyItemType(listCandidate.TypeArguments[0], known, cache, visiting, diagnostics, memberLocation, cancellationToken);
            return element is null ? null : new MemberModel { Name = name, Shape = MemberShape.List, Element = element };
        }

        if (type is INamedTypeSymbol { IsGenericType: true } dictCandidate && SymbolEqualityComparer.Default.Equals(dictCandidate.ConstructedFrom, known.DictionaryOfTKeyTValue))
        {
            // Classify both independently (rather than short-circuiting) so a bad key AND a bad value are both reported at once.
            ScalarTypeModel? key = ClassifyScalarElement(dictCandidate.TypeArguments[0], known, cache, visiting, diagnostics, memberLocation, cancellationToken);
            ItemTypeModel? value = ClassifyItemType(dictCandidate.TypeArguments[1], known, cache, visiting, diagnostics, memberLocation, cancellationToken);

            // Keys stay scalar (no List/T[]/Dictionary - ClassifyScalarElement already can't produce those),
            // and additionally may not be a *variable-size* [ByteSerializable] type: string is the most
            // "variable" a key is allowed to be, and a fixed-size custom type is fine since it has a
            // well-defined size and (typically, via records) well-defined equality.
            if (key is { Kind: ScalarKind.NestedByteSerializable, NestedIsFixedSize: false })
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.DictionaryKeyMustBeFixedSize, memberLocation, key.NestedTypeFullName));
                key = null;
            }

            return key is null || value is null ? null : new MemberModel { Name = name, Shape = MemberShape.Dictionary, Key = key, DictionaryValue = value };
        }

        ScalarTypeModel? scalar = ClassifyScalarElement(type, known, cache, visiting, diagnostics, memberLocation, cancellationToken);
        return scalar is null ? null : new MemberModel { Name = name, Shape = MemberShape.Scalar, Scalar = scalar };
    }

    /// <summary>
    /// Classifies a member annotated <c>[ByteSerializableAs&lt;TTarget&gt;]</c>: validates that
    /// <paramref name="declaredType"/> converts to and from <paramref name="targetType"/>, then classifies
    /// <paramref name="targetType"/> itself through the normal <see cref="ClassifyMember"/> pipeline - the
    /// returned model's Shape/Scalar/Element/Key/DictionaryValue describe TTarget (that's what actually
    /// goes on the wire), with the member's own declared type recorded separately so the emitter knows to
    /// cast when writing and cast back when reading.
    /// </summary>
    private static MemberModel? ClassifyMemberWithCast(
        string name,
        ITypeSymbol declaredType,
        ITypeSymbol targetType,
        KnownSymbols known,
        Dictionary<INamedTypeSymbol, ClassifiedShape?> cache,
        HashSet<INamedTypeSymbol> visiting,
        List<Diagnostic> diagnostics,
        Location memberLocation,
        CancellationToken cancellationToken)
    {
        Conversion toTarget = known.Compilation.ClassifyConversion(declaredType, targetType);
        Conversion fromTarget = known.Compilation.ClassifyConversion(targetType, declaredType);

        if (!toTarget.Exists || !fromTarget.Exists)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.ByteSerializableAsConversionMissing,
                memberLocation,
                name,
                declaredType.ToDisplayString(QualifiedFormat),
                targetType.ToDisplayString(QualifiedFormat)));
            return null;
        }

        MemberModel? targetMember = ClassifyMember(name, targetType, known, cache, visiting, diagnostics, memberLocation, cancellationToken);
        if (targetMember is null)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.ByteSerializableAsTargetUnsupported,
                memberLocation,
                name,
                targetType.ToDisplayString(QualifiedFormat)));
            return null;
        }

        return new MemberModel
        {
            Name = targetMember.Name,
            Shape = targetMember.Shape,
            Scalar = targetMember.Scalar,
            Element = targetMember.Element,
            Key = targetMember.Key,
            DictionaryValue = targetMember.DictionaryValue,
            OriginalTypeDisplayName = declaredType.ToDisplayString(QualifiedFormat),
            TargetTypeDisplayName = targetType.ToDisplayString(QualifiedFormat),
        };
    }

    /// <summary>
    /// Classifies a List/array element's type, which may itself be a nested List&lt;T&gt;/T[] (supporting
    /// List&lt;List&lt;T&gt;&gt;, T[][], and arbitrary combinations/depths) before bottoming out at a
    /// <see cref="ClassifyScalarElement"/> leaf. Deliberately not shared with Dictionary key/value
    /// classification - those stay scalar-only.
    /// </summary>
    private static ItemTypeModel? ClassifyItemType(
        ITypeSymbol type,
        KnownSymbols known,
        Dictionary<INamedTypeSymbol, ClassifiedShape?> cache,
        HashSet<INamedTypeSymbol> visiting,
        List<Diagnostic> diagnostics,
        Location location,
        CancellationToken cancellationToken)
    {
        if (type is IArrayTypeSymbol { Rank: 1 } arrayType)
        {
            ItemTypeModel? inner = ClassifyItemType(arrayType.ElementType, known, cache, visiting, diagnostics, location, cancellationToken);
            return inner is null ? null : new ItemTypeModel { Shape = ItemShape.Array, Element = inner };
        }

        if (type is INamedTypeSymbol { IsGenericType: true } listCandidate && SymbolEqualityComparer.Default.Equals(listCandidate.ConstructedFrom, known.ListOfT))
        {
            ItemTypeModel? inner = ClassifyItemType(listCandidate.TypeArguments[0], known, cache, visiting, diagnostics, location, cancellationToken);
            return inner is null ? null : new ItemTypeModel { Shape = ItemShape.List, Element = inner };
        }

        ScalarTypeModel? scalar = ClassifyScalarElement(type, known, cache, visiting, diagnostics, location, cancellationToken);
        return scalar is null ? null : new ItemTypeModel { Shape = ItemShape.Scalar, Scalar = scalar };
    }

    private static ScalarTypeModel? ClassifyScalarElement(
        ITypeSymbol type,
        KnownSymbols known,
        Dictionary<INamedTypeSymbol, ClassifiedShape?> cache,
        HashSet<INamedTypeSymbol> visiting,
        List<Diagnostic> diagnostics,
        Location location,
        CancellationToken cancellationToken)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return new ScalarTypeModel { Kind = ScalarKind.String, TypeDisplayName = "string" };
        }

        if (TryGetPrimitiveKind(type, known, out PrimitiveKind? primitiveKind))
        {
            return new ScalarTypeModel { Kind = ScalarKind.FixedPrimitive, Primitive = primitiveKind!.Value, TypeDisplayName = type.ToDisplayString(QualifiedFormat) };
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType
            && TryGetPrimitiveKind(enumType.EnumUnderlyingType!, known, out PrimitiveKind? underlyingKind))
        {
            return new ScalarTypeModel { Kind = ScalarKind.Enum, Primitive = underlyingKind!.Value, TypeDisplayName = type.ToDisplayString(QualifiedFormat) };
        }

        if (type is INamedTypeSymbol nestedCandidate)
        {
            if (known.HasByteSerializableAttribute(nestedCandidate))
            {
                ClassifiedShape? nestedShape = ClassifyType(nestedCandidate, known, cache, visiting, diagnostics, location, cancellationToken);
                if (nestedShape is null)
                {
                    return null;
                }

                return new ScalarTypeModel
                {
                    Kind = ScalarKind.NestedByteSerializable,
                    TypeDisplayName = type.ToDisplayString(QualifiedFormat),
                    NestedTypeFullName = type.ToDisplayString(QualifiedFormat),
                    NestedIsFixedSize = nestedShape.IsFixedSize,
                    NestedFixedSize = nestedShape.FixedSize,
                };
            }

            // Not [ByteSerializable] (so nothing to generate for it), but it already implements
            // IByteSerializable<TSelf> for itself - use that existing implementation as-is. There is no
            // way to know whether a hand-written implementation is fixed-size without generating it
            // ourselves, so it's always treated as variable-size: correct either way, just means one
            // GetByteSize() call at runtime instead of an inlined constant when it happens to be fixed.
            if (known.ImplementsIByteSerializableForSelf(nestedCandidate))
            {
                return new ScalarTypeModel
                {
                    Kind = ScalarKind.NestedByteSerializable,
                    TypeDisplayName = type.ToDisplayString(QualifiedFormat),
                    NestedTypeFullName = type.ToDisplayString(QualifiedFormat),
                    NestedIsFixedSize = false,
                    NestedFixedSize = 0,
                };
            }
        }

        return null;
    }

    private static bool TryGetPrimitiveKind(ITypeSymbol type, KnownSymbols known, out PrimitiveKind? kind)
    {
        kind = type.SpecialType switch
        {
            SpecialType.System_Boolean => PrimitiveKind.Boolean,
            SpecialType.System_Byte => PrimitiveKind.Byte,
            SpecialType.System_SByte => PrimitiveKind.SByte,
            SpecialType.System_Int16 => PrimitiveKind.Int16,
            SpecialType.System_UInt16 => PrimitiveKind.UInt16,
            SpecialType.System_Int32 => PrimitiveKind.Int32,
            SpecialType.System_UInt32 => PrimitiveKind.UInt32,
            SpecialType.System_Int64 => PrimitiveKind.Int64,
            SpecialType.System_UInt64 => PrimitiveKind.UInt64,
            SpecialType.System_Single => PrimitiveKind.Single,
            SpecialType.System_Double => PrimitiveKind.Double,
            SpecialType.System_Char => PrimitiveKind.Char,
            SpecialType.System_Decimal => PrimitiveKind.Decimal,
            SpecialType.System_DateTime => PrimitiveKind.DateTime,
            _ when SymbolEqualityComparer.Default.Equals(type, known.GuidType) => PrimitiveKind.Guid,
            _ when known.TimeOnlyType is not null && SymbolEqualityComparer.Default.Equals(type, known.TimeOnlyType) => PrimitiveKind.TimeOnly,
            _ => null
        };

        return kind is not null;
    }

    private static List<(string Name, ITypeSymbol Type, ISymbol Symbol)> GetSerializableMembers(INamedTypeSymbol typeSymbol)
    {
        var result = new List<(string, ITypeSymbol, ISymbol)>();

        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            switch (member)
            {
                case IFieldSymbol { IsConst: false, IsImplicitlyDeclared: false } field:
                    result.Add((field.Name, field.Type, field));
                    break;
                case IPropertySymbol { IsIndexer: false, GetMethod: not null } property:
                    result.Add((property.Name, property.Type, property));
                    break;
            }
        }

        return result;
    }

    private static bool TryFindPositionalConstructor(INamedTypeSymbol typeSymbol, List<(string Name, ITypeSymbol Type, ISymbol Symbol)> members)
    {
        if (!typeSymbol.IsRecord)
        {
            return false;
        }

        foreach (IMethodSymbol candidate in typeSymbol.Constructors)
        {
            if (candidate.IsImplicitlyDeclared || candidate.Parameters.Length != members.Count)
            {
                continue;
            }

            bool matches = true;

            for (int i = 0; i < members.Count; i++)
            {
                IParameterSymbol parameter = candidate.Parameters[i];

                if (parameter.Name != members[i].Name || !SymbolEqualityComparer.Default.Equals(parameter.Type, members[i].Type))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanUseParameterlessConstructor(INamedTypeSymbol typeSymbol, List<(string Name, ITypeSymbol Type, ISymbol Symbol)> members)
    {
        bool hasParameterlessCtor = typeSymbol.TypeKind == TypeKind.Struct
            || typeSymbol.Constructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);

        if (!hasParameterlessCtor)
        {
            return false;
        }

        foreach ((_, _, ISymbol symbol) in members)
        {
            bool settable = symbol switch
            {
                IPropertySymbol property => property.SetMethod is not null,
                IFieldSymbol field => !field.IsReadOnly,
                _ => false,
            };

            if (!settable)
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildDeclarationHeader(TypeDeclarationSyntax syntax)
    {
        IEnumerable<string> modifiers = syntax.Modifiers
            .Where(static m => !m.IsKind(SyntaxKind.PartialKeyword))
            .Select(static m => m.Text);

        string keyword;
        if (syntax is RecordDeclarationSyntax record)
        {
            keyword = record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record";
        }
        else if (syntax is StructDeclarationSyntax)
        {
            keyword = "struct";
        }
        else
        {
            keyword = "class";
        }

        return string.Join(" ", modifiers.Concat(["partial", keyword]));
    }
}
