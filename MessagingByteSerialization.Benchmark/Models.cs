using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using MessagingByteSerialization;

namespace MessagingByteSerialization.Benchmark;

public enum SomeRandomEnum
{
    Alpha,
    Bravo,
    Charlie,
    Delta,
    Echo,
    Foxtrot,
    Golf,
    Hotel,
}

/// <summary>
/// Has no "natural" wire representation of its own - it only has conversions to/from string. Exercises
/// [ByteSerializableAs&lt;string&gt;], which serializes it as the string produced/consumed by those casts.
/// </summary>
public sealed class Tag
{
    public string Value { get; }

    public Tag(string value) => Value = value;

    public static implicit operator string(Tag tag) => tag.Value;

    public static implicit operator Tag(string value) => new(value);
}

/// <summary>
/// Hand-written IByteSerializable&lt;Checksum&gt; implementation - no [ByteSerializable], nothing
/// generated for it. Exercises embedding an already-serializable type as-is: the generator just calls
/// ToBytes/FromBytes/TypeHash directly.
/// </summary>
public sealed class Checksum : IByteSerializable<Checksum>
{
    public uint Value { get; }

    public Checksum(uint value) => Value = value;

    public static ushort TypeHash => 9001;

    public int GetByteSize() => sizeof(uint);

    public void ToBytes(Span<byte> destination) => BinaryPrimitives.WriteUInt32LittleEndian(destination, Value);

    public static Checksum FromBytes(ReadOnlySpan<byte> source, out int bytesRead)
    {
        bytesRead = sizeof(uint);
        return new Checksum(BinaryPrimitives.ReadUInt32LittleEndian(source));
    }
}

/// <summary>Mutable reference type: exercises the parameterless-constructor + property-initializer generation strategy.</summary>
[ByteSerializable]
public partial class MessageClass
{
    public string Name { get; set; } = "";

    public Guid Id { get; set; }

    public DateTime CreationTime { get; set; }

    public TimeOnly LastCheck { get; set; }

    public List<string> States { get; set; } = new();

    public int[] Numbers { get; set; } = Array.Empty<int>();

    public int[][] Grid { get; set; } = Array.Empty<int[]>();

    public Dictionary<string, int> Metadata { get; set; } = new();

    public Dictionary<string, List<int>> Groups { get; set; } = new();

    [ByteSerializableAs<string>]
    public Tag Label { get; set; } = new("");

    public Checksum Checksum { get; set; } = new(0);

    /// <summary>Transient/derived state - never meant to cross the wire. Exercises [ByteSerializableIgnore].</summary>
    [ByteSerializableIgnore]
    public object Cache { get; set; }

    public SomeRandomEnum RandomEnum { get; set; }
}

/// <summary>Mutable value type: same property-initializer strategy as <see cref="MessageClass"/>, but a struct.</summary>
[ByteSerializable]
public partial struct MessageStruct
{
    public string Name { get; set; }

    public Guid Id { get; set; }

    public DateTime CreationTime { get; set; }

    public TimeOnly LastCheck { get; set; }

    public List<string> States { get; set; }

    public int[] Numbers { get; set; }

    public int[][] Grid { get; set; }

    public Dictionary<string, int> Metadata { get; set; }

    public Dictionary<string, List<int>> Groups { get; set; }

    [ByteSerializableAs<string>]
    public Tag Label { get; set; }

    public Checksum Checksum { get; set; }

    /// <summary>Transient/derived state - never meant to cross the wire. Exercises [ByteSerializableIgnore].</summary>
    [ByteSerializableIgnore]
    public object Cache { get; set; }

    public SomeRandomEnum RandomEnum { get; set; }
}

/// <summary>Reference type record: exercises the positional-constructor generation strategy.</summary>
[ByteSerializable]
public partial record MessageRecord(
    string Name,
    Guid Id,
    DateTime CreationTime,
    TimeOnly LastCheck,
    List<string> States,
    int[] Numbers,
    int[][] Grid,
    Dictionary<string, int> Metadata,
    Dictionary<string, List<int>> Groups,
    [property: ByteSerializableAs<string>] Tag Label,
    Checksum Checksum,
    [property: ByteSerializableIgnore] object Cache,
    SomeRandomEnum RandomEnum);

/// <summary>Immutable value type: same positional-constructor strategy as <see cref="MessageRecord"/>, but a readonly struct.</summary>
[ByteSerializable]
public readonly partial record struct MessageReadonlyStruct(
    string Name,
    Guid Id,
    DateTime CreationTime,
    TimeOnly LastCheck,
    List<string> States,
    int[] Numbers,
    int[][] Grid,
    Dictionary<string, int> Metadata,
    Dictionary<string, List<int>> Groups,
    [property: ByteSerializableAs<string>] Tag Label,
    Checksum Checksum,
    [property: ByteSerializableIgnore] object Cache,
    SomeRandomEnum RandomEnum);
