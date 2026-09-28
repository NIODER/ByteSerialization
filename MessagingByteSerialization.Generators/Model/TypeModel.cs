using System.Collections.Immutable;
using System.Linq;

namespace MessagingByteSerialization.Generators.Model;

internal enum ConstructionStrategy
{
    /// <summary>Positional record (or record struct) whose primary constructor matches the serializable members 1:1, in order.</summary>
    PositionalConstructor,

    /// <summary>Public parameterless constructor followed by an object initializer over the serializable members.</summary>
    ParameterlessConstructorWithInitializer,
}

/// <summary>Fully analyzed, ready-to-emit description of one <c>[ByteSerializable]</c> type.</summary>
internal sealed class TypeModel
{
    /// <summary>Empty string means the global namespace.</summary>
    public string Namespace { get; init; } = "";

    public string Name { get; init; } = "";

    /// <summary>e.g. "public partial record struct" - modifiers and keyword(s) copied from the source declaration, plus "partial".</summary>
    public string DeclarationHeader { get; init; } = "";

    public ushort TypeHash { get; init; }

    public ConstructionStrategy Construction { get; init; }

    public ImmutableArray<MemberModel> Members { get; init; } = [];

    public bool IsFixedSize => Members.All(static m => !m.IsVariableSize);

    /// <summary>Ignored members are excluded here (rather than contributing 0 via a shared code path) because their <see cref="MemberModel.Scalar"/> is null - there is nothing fixed-size-shaped to sum for them.</summary>
    public int FixedSize => Members.Where(static m => !m.IsIgnored).Sum(static m => m.Scalar!.FixedSize);

    public string FullyQualifiedName => Namespace.Length == 0 ? $"global::{Name}" : $"global::{Namespace}.{Name}";
}
