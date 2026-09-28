using System;

namespace MessagingByteSerialization;

/// <summary>
/// Non-generic facet of a byte-serializable type. Kept separate from <see cref="IByteSerializable{TSelf}"/>
/// so that values can be handled polymorphically (e.g. by <c>ByteSerializableFactory</c>) even though
/// <c>FromBytes</c>/<c>TypeHash</c> are static-abstract and therefore cannot appear on a non-generic interface.
///
/// Only the two members below live on the interface. The <c>byte[] ToBytes()</c> and
/// <c>static TSelf FromBytes(byte[])</c> array-based convenience overloads are emitted directly on every
/// generated type instead of as default interface members: C# only resolves default interface members
/// through an interface-typed (or generically-constrained) expression, never through the concrete type's
/// own name - which is how virtually every caller would try to use them. Emitting them concretely makes
/// <c>instance.ToBytes()</c> and <c>MyType.FromBytes(bytes)</c> work the way callers expect.
/// </summary>
public interface IByteSerializable
{
    /// <summary>Exact number of bytes <see cref="ToBytes(Span{byte})"/> will write.</summary>
    int GetByteSize();

    /// <summary>
    /// Writes the wire representation of this instance to <paramref name="destination"/>.
    /// <paramref name="destination"/> must be at least <see cref="GetByteSize"/> bytes long.
    /// </summary>
    void ToBytes(Span<byte> destination);
}

/// <summary>
/// Byte-serializable type contract implemented by generated code for every type marked with
/// <see cref="ByteSerializableAttribute"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself (CRTP-style self type).</typeparam>
public interface IByteSerializable<TSelf> : IByteSerializable
    where TSelf : IByteSerializable<TSelf>
{
    /// <summary>
    /// Stable identifier of <typeparamref name="TSelf"/> within the wire format, derived from the type's
    /// fully-qualified name. Used as the collection item-type code and as the message discriminator
    /// consumed by <c>ByteSerializableFactory</c>.
    /// </summary>
    static abstract ushort TypeHash { get; }

    /// <summary>
    /// Reconstructs an instance of <typeparamref name="TSelf"/> from <paramref name="source"/>.
    /// </summary>
    /// <param name="bytesRead">Number of bytes consumed from the start of <paramref name="source"/>.</param>
    static abstract TSelf FromBytes(ReadOnlySpan<byte> source, out int bytesRead);
}
