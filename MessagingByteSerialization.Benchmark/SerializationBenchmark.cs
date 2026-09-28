using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MessagingByteSerialization.Benchmark;

/// <summary>
/// Compares generated ToBytes/FromBytes across the four shapes [ByteSerializable] supports - mutable
/// class, mutable struct, positional record and readonly record struct - all four declaring the exact
/// same members, populated with the exact same values, so the numbers are comparable across shapes.
///
/// *_ToBytes is the allocating convenience overload; *_ToBytesSpan writes into a single pre-rented buffer
/// reused across iterations, which is the intended zero-allocation hot-path call. *_FromBytes necessarily
/// allocates (Name/States/Numbers are all reference types), regardless of shape.
/// </summary>
[MemoryDiagnoser]
[ExceptionDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SerializationBenchmark
{
    private MessageClass classMessage = null!;
    private MessageStruct structMessage;
    private MessageRecord recordMessage = null!;
    private MessageReadonlyStruct readonlyStructMessage;

    private byte[] classBytes = null!;
    private byte[] structBytes = null!;
    private byte[] recordBytes = null!;
    private byte[] readonlyStructBytes = null!;

    private byte[] scratch = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(12345);

        string name = "carrot-message";
        Guid id = Guid.NewGuid();
        DateTime creationTime = DateTime.UtcNow;
        TimeOnly lastCheck = TimeOnly.FromDateTime(creationTime);
        List<string> states = new() { "Created", "Queued", "Running", "Completed", "Archived" };
        int[] numbers = new int[32];
        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = random.Next(int.MinValue, int.MaxValue);
        }

        Dictionary<string, int> metadata = new()
        {
            ["priority"] = random.Next(0, 10),
            ["retryCount"] = random.Next(0, 5),
            ["shardId"] = random.Next(0, 64),
            ["region"] = random.Next(0, 8),
        };

        int[][] grid = new int[8][];
        for (int row = 0; row < grid.Length; row++)
        {
            grid[row] = new int[8];
            for (int col = 0; col < grid[row].Length; col++)
            {
                grid[row][col] = random.Next(int.MinValue, int.MaxValue);
            }
        }

        Dictionary<string, List<int>> groups = new()
        {
            ["primary"] = new List<int> { 1, 2, 3, 4 },
            ["secondary"] = new List<int> { 5, 6 },
            ["empty"] = new List<int>(),
        };

        Tag label = new("benchmark-tag");
        Checksum checksum = new(0xDEADBEEF);

        // Ignored ([ByteSerializableIgnore]) - set to a non-null value on purpose so FromBytes producing
        // null back (default(object)) is visibly a real round-trip effect, not just an unset property.
        object cache = new();

        SomeRandomEnum randomEnum = (SomeRandomEnum)random.Next(0, Enum.GetValues<SomeRandomEnum>().Length);

        classMessage = new MessageClass
        {
            Name = name,
            Id = id,
            CreationTime = creationTime,
            LastCheck = lastCheck,
            States = new List<string>(states),
            Numbers = (int[])numbers.Clone(),
            Grid = CloneGrid(grid),
            Metadata = new Dictionary<string, int>(metadata),
            Groups = CloneGroups(groups),
            Label = label,
            Checksum = checksum,
            Cache = cache,
            RandomEnum = randomEnum,
        };

        structMessage = new MessageStruct
        {
            Name = name,
            Id = id,
            CreationTime = creationTime,
            LastCheck = lastCheck,
            States = new List<string>(states),
            Numbers = (int[])numbers.Clone(),
            Grid = CloneGrid(grid),
            Metadata = new Dictionary<string, int>(metadata),
            Groups = CloneGroups(groups),
            Label = label,
            Checksum = checksum,
            Cache = cache,
            RandomEnum = randomEnum,
        };

        recordMessage = new MessageRecord(name, id, creationTime, lastCheck, new List<string>(states), (int[])numbers.Clone(), CloneGrid(grid), new Dictionary<string, int>(metadata), CloneGroups(groups), label, checksum, cache, randomEnum);

        readonlyStructMessage = new MessageReadonlyStruct(name, id, creationTime, lastCheck, new List<string>(states), (int[])numbers.Clone(), CloneGrid(grid), new Dictionary<string, int>(metadata), CloneGroups(groups), label, checksum, cache, randomEnum);

        classBytes = classMessage.ToBytes();
        structBytes = structMessage.ToBytes();
        recordBytes = recordMessage.ToBytes();
        readonlyStructBytes = readonlyStructMessage.ToBytes();

        int maxSize = Math.Max(Math.Max(classBytes.Length, structBytes.Length), Math.Max(recordBytes.Length, readonlyStructBytes.Length));
        scratch = new byte[maxSize];
    }

    private static int[][] CloneGrid(int[][] grid)
    {
        var clone = new int[grid.Length][];
        for (int i = 0; i < grid.Length; i++)
        {
            clone[i] = (int[])grid[i].Clone();
        }

        return clone;
    }

    private static Dictionary<string, List<int>> CloneGroups(Dictionary<string, List<int>> groups)
    {
        var clone = new Dictionary<string, List<int>>(groups.Count);
        foreach (KeyValuePair<string, List<int>> entry in groups)
        {
            clone[entry.Key] = new List<int>(entry.Value);
        }

        return clone;
    }

    // ----------------------------------------------------------------- class

    [Benchmark(Baseline = true)]
    public byte[] Class_ToBytes() => classMessage.ToBytes();

    [Benchmark]
    public int Class_ToBytesSpan()
    {
        classMessage.ToBytes(scratch);
        return classMessage.GetByteSize();
    }

    [Benchmark]
    public MessageClass Class_FromBytes() => MessageClass.FromBytes(classBytes);

    // ----------------------------------------------------------------- struct

    [Benchmark]
    public byte[] Struct_ToBytes() => structMessage.ToBytes();

    [Benchmark]
    public int Struct_ToBytesSpan()
    {
        structMessage.ToBytes(scratch);
        return structMessage.GetByteSize();
    }

    [Benchmark]
    public MessageStruct Struct_FromBytes() => MessageStruct.FromBytes(structBytes);

    // ----------------------------------------------------------------- record

    [Benchmark]
    public byte[] Record_ToBytes() => recordMessage.ToBytes();

    [Benchmark]
    public int Record_ToBytesSpan()
    {
        recordMessage.ToBytes(scratch);
        return recordMessage.GetByteSize();
    }

    [Benchmark]
    public MessageRecord Record_FromBytes() => MessageRecord.FromBytes(recordBytes);

    // ----------------------------------------------------------------- readonly record struct

    [Benchmark]
    public byte[] ReadonlyStruct_ToBytes() => readonlyStructMessage.ToBytes();

    [Benchmark]
    public int ReadonlyStruct_ToBytesSpan()
    {
        readonlyStructMessage.ToBytes(scratch);
        return readonlyStructMessage.GetByteSize();
    }

    [Benchmark]
    public MessageReadonlyStruct ReadonlyStruct_FromBytes() => MessageReadonlyStruct.FromBytes(readonlyStructBytes);
}
