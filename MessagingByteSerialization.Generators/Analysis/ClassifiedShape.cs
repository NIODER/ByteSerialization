using System.Collections.Immutable;
using MessagingByteSerialization.Generators.Model;

namespace MessagingByteSerialization.Generators.Analysis;

/// <summary>Result of recursively classifying one <c>[ByteSerializable]</c> type's data shape.</summary>
internal sealed class ClassifiedShape
{
    public bool IsFixedSize { get; init; }

    public int FixedSize { get; init; }

    public ConstructionStrategy Construction { get; init; }

    public ImmutableArray<MemberModel> Members { get; init; } = [];
}
