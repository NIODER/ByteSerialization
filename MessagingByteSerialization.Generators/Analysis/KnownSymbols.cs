using Microsoft.CodeAnalysis;

namespace MessagingByteSerialization.Generators.Analysis;

/// <summary>Well-known BCL/Abstractions symbols resolved once per analyzed type.</summary>
internal sealed class KnownSymbols
{
    private KnownSymbols(
        Compilation compilation,
        INamedTypeSymbol listOfT,
        INamedTypeSymbol dictionaryOfTKeyTValue,
        INamedTypeSymbol byteSerializableAttribute,
        INamedTypeSymbol byteSerializableOfT,
        INamedTypeSymbol guidType,
        INamedTypeSymbol? timeOnlyType,
        INamedTypeSymbol? byteSerializableAsAttribute,
        INamedTypeSymbol? byteSerializableIgnoreAttribute)
    {
        Compilation = compilation;
        ListOfT = listOfT;
        DictionaryOfTKeyTValue = dictionaryOfTKeyTValue;
        ByteSerializableAttribute = byteSerializableAttribute;
        ByteSerializableOfT = byteSerializableOfT;
        GuidType = guidType;
        TimeOnlyType = timeOnlyType;
        ByteSerializableAsAttribute = byteSerializableAsAttribute;
        ByteSerializableIgnoreAttribute = byteSerializableIgnoreAttribute;
    }

    public Compilation Compilation { get; }

    public INamedTypeSymbol ListOfT { get; }

    public INamedTypeSymbol DictionaryOfTKeyTValue { get; }

    public INamedTypeSymbol ByteSerializableAttribute { get; }

    /// <summary>Unbound generic definition of <c>IByteSerializable&lt;TSelf&gt;</c>, used to recognize hand-written implementations (see <see cref="ImplementsIByteSerializableForSelf"/>).</summary>
    public INamedTypeSymbol ByteSerializableOfT { get; }

    public INamedTypeSymbol GuidType { get; }

    /// <summary>Null when targeting an older TFM without <c>System.TimeOnly</c> (introduced in .NET 6); <c>TimeOnly</c> members are simply unsupported there.</summary>
    public INamedTypeSymbol? TimeOnlyType { get; }

    /// <summary>Unbound generic definition of <c>ByteSerializableAsAttribute&lt;TTargetType&gt;</c>. Null when the referenced Abstractions assembly predates the attribute - [ByteSerializableAs&lt;T&gt;] is simply not recognized there.</summary>
    public INamedTypeSymbol? ByteSerializableAsAttribute { get; }

    /// <summary>Null when the referenced Abstractions assembly predates the attribute - [ByteSerializableIgnore] is simply not recognized there.</summary>
    public INamedTypeSymbol? ByteSerializableIgnoreAttribute { get; }

    public static KnownSymbols? Create(Compilation compilation)
    {
        INamedTypeSymbol? listOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
        INamedTypeSymbol? dictionaryOfTKeyTValue = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        INamedTypeSymbol? attribute = compilation.GetTypeByMetadataName("MessagingByteSerialization.ByteSerializableAttribute");
        INamedTypeSymbol? byteSerializableOfT = compilation.GetTypeByMetadataName("MessagingByteSerialization.IByteSerializable`1");
        INamedTypeSymbol? guid = compilation.GetTypeByMetadataName("System.Guid");

        if (listOfT is null || dictionaryOfTKeyTValue is null || attribute is null || byteSerializableOfT is null || guid is null)
        {
            return null;
        }

        INamedTypeSymbol? timeOnly = compilation.GetTypeByMetadataName("System.TimeOnly");
        INamedTypeSymbol? byteSerializableAsAttribute = compilation.GetTypeByMetadataName("MessagingByteSerialization.ByteSerializableAsAttribute`1");
        INamedTypeSymbol? byteSerializableIgnoreAttribute = compilation.GetTypeByMetadataName("MessagingByteSerialization.ByteSerializableIgnoreAttribute");

        return new KnownSymbols(compilation, listOfT, dictionaryOfTKeyTValue, attribute, byteSerializableOfT, guid, timeOnly, byteSerializableAsAttribute, byteSerializableIgnoreAttribute);
    }

    public bool HasByteSerializableAttribute(INamedTypeSymbol candidate)
    {
        foreach (AttributeData attribute in candidate.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, ByteSerializableAttribute))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when <paramref name="candidate"/> already implements <c>IByteSerializable&lt;candidate&gt;</c>
    /// for itself (hand-written, typically) - regardless of whether it also carries [ByteSerializable].
    /// Such a type can be embedded as-is: its existing ToBytes/FromBytes/TypeHash are called directly,
    /// nothing is generated for it.
    /// </summary>
    public bool ImplementsIByteSerializableForSelf(INamedTypeSymbol candidate)
    {
        foreach (INamedTypeSymbol candidateInterface in candidate.AllInterfaces)
        {
            if (candidateInterface.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(candidateInterface.OriginalDefinition, ByteSerializableOfT)
                && SymbolEqualityComparer.Default.Equals(candidateInterface.TypeArguments[0], candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns TTargetType if <paramref name="member"/> carries <c>[ByteSerializableAs&lt;TTargetType&gt;]</c>, else null.</summary>
    public ITypeSymbol? TryGetByteSerializableAsTarget(ISymbol member)
    {
        if (ByteSerializableAsAttribute is null)
        {
            return null;
        }

        foreach (AttributeData attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass is { IsGenericType: true } attributeClass
                && attributeClass.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(attributeClass.ConstructedFrom, ByteSerializableAsAttribute))
            {
                return attributeClass.TypeArguments[0];
            }
        }

        return null;
    }

    public bool HasByteSerializableIgnoreAttribute(ISymbol member)
    {
        if (ByteSerializableIgnoreAttribute is null)
        {
            return false;
        }

        foreach (AttributeData attribute in member.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, ByteSerializableIgnoreAttribute))
            {
                return true;
            }
        }

        return false;
    }
}
