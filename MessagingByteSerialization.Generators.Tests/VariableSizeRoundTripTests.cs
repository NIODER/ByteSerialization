using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class VariableSizeRoundTripTests
{
    [Fact]
    public void ListArrayAndString_RoundTrip()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(int Id, List<int> Numbers, string Text, double[] Values);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(1, new List<int> { 1, 2, 3 }, "hello éè world 😀", new double[] { 1.5, -2.25, 0 });

                    if (original.GetByteSize() <= 0) return "non-positive size";

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";

                    if (restored.Id != 1) return "Id mismatch";
                    if (!restored.Numbers.SequenceEqual(original.Numbers)) return "Numbers mismatch";
                    if (restored.Text != original.Text) return "Text mismatch";
                    if (!restored.Values.SequenceEqual(original.Values)) return "Values mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    /// <summary>
    /// Every primitive kind used as a *collection item*, not just a plain scalar field. Item type codes
    /// (WellKnownTypeCodes.*) and per-item BinaryPrimitives calls are only emitted on this path - a
    /// scalar-only test would miss a mismatch here (as happened with a PrimitiveKind rename that broke
    /// float specifically: BinaryPrimitives has no WriteFloatLittleEndian, and WellKnownTypeCodes has no
    /// Float constant, only Single).
    /// </summary>
    [Fact]
    public void ListOfEveryPrimitiveKind_RoundTrips()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            public enum Status : short { Off = -1, On = 1 }

            [ByteSerializable]
            public partial record AllLists(
                List<bool> Bools,
                List<byte> Bytes,
                List<sbyte> SBytes,
                List<short> Shorts,
                List<ushort> UShorts,
                List<int> Ints,
                List<uint> UInts,
                List<long> Longs,
                List<ulong> ULongs,
                List<float> Floats,
                List<double> Doubles,
                List<char> Chars,
                List<decimal> Decimals,
                List<Guid> Guids,
                List<DateTime> Timestamps,
                List<TimeOnly> Times,
                List<Status> Statuses);

            public static class Harness
            {
                public static string Verify()
                {
                    var guid = Guid.NewGuid();
                    var original = new AllLists(
                        new List<bool> { true, false },
                        new List<byte> { 1, 200 },
                        new List<sbyte> { -1, 100 },
                        new List<short> { -1000, 1000 },
                        new List<ushort> { 1000, 60000 },
                        new List<int> { -1, 2000000000 },
                        new List<uint> { 1, 3000000000 },
                        new List<long> { -1, 1234567890123 },
                        new List<ulong> { 1, 12345678901234567 },
                        new List<float> { 1.5f, -2.25f },
                        new List<double> { 1.5, -2.25 },
                        new List<char> { 'a', 'z' },
                        new List<decimal> { 1.1m, -2.2m },
                        new List<Guid> { guid },
                        new List<DateTime> { new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), DateTime.Now },
                        new List<TimeOnly> { new TimeOnly(1, 2, 3), TimeOnly.MaxValue },
                        new List<Status> { Status.On, Status.Off });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = AllLists.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";

                    if (!restored.Bools.SequenceEqual(original.Bools)) return "Bools mismatch";
                    if (!restored.Bytes.SequenceEqual(original.Bytes)) return "Bytes mismatch";
                    if (!restored.SBytes.SequenceEqual(original.SBytes)) return "SBytes mismatch";
                    if (!restored.Shorts.SequenceEqual(original.Shorts)) return "Shorts mismatch";
                    if (!restored.UShorts.SequenceEqual(original.UShorts)) return "UShorts mismatch";
                    if (!restored.Ints.SequenceEqual(original.Ints)) return "Ints mismatch";
                    if (!restored.UInts.SequenceEqual(original.UInts)) return "UInts mismatch";
                    if (!restored.Longs.SequenceEqual(original.Longs)) return "Longs mismatch";
                    if (!restored.ULongs.SequenceEqual(original.ULongs)) return "ULongs mismatch";
                    if (!restored.Floats.SequenceEqual(original.Floats)) return "Floats mismatch";
                    if (!restored.Doubles.SequenceEqual(original.Doubles)) return "Doubles mismatch";
                    if (!restored.Chars.SequenceEqual(original.Chars)) return "Chars mismatch";
                    if (!restored.Decimals.SequenceEqual(original.Decimals)) return "Decimals mismatch";
                    if (!restored.Guids.SequenceEqual(original.Guids)) return "Guids mismatch";
                    if (!restored.Timestamps.SequenceEqual(original.Timestamps)) return "Timestamps mismatch";
                    if (restored.Timestamps.Zip(original.Timestamps).Any(p => p.First.Kind != p.Second.Kind)) return "Timestamps Kind mismatch";
                    if (!restored.Times.SequenceEqual(original.Times)) return "Times mismatch";
                    if (!restored.Statuses.SequenceEqual(original.Statuses)) return "Statuses mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void EmptyCollectionsAndEmptyString_RoundTrip()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<int> Numbers, string Text, int[] Values);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<int>(), "", new int[0]);
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Numbers.Count != 0) return "Numbers not empty";
                    if (restored.Text.Length != 0) return "Text not empty";
                    if (restored.Values.Length != 0) return "Values not empty";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void NestedFixedSizeType_ComposesIntoFixedSizeOuter()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Inner(int A, double B);

            [ByteSerializable]
            public partial record struct Outer(Inner Nested, int Tag);

            public static class Harness
            {
                public static string Verify()
                {
                    if (Outer.FixedByteSize != Inner.FixedByteSize + 4)
                    {
                        return $"unexpected fixed size {Outer.FixedByteSize}";
                    }

                    var original = new Outer(new Inner(7, 3.5), 99);
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != Outer.FixedByteSize) return "size mismatch";

                    var restored = Outer.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Nested.A != 7 || restored.Nested.B != 3.5 || restored.Tag != 99) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void NestedVariableSizeType_MakesOuterVariableSize()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Inner(List<int> Values, int Count);

            [ByteSerializable]
            public partial record Outer(Inner Nested, string Label);

            public static class Harness
            {
                public static string Verify()
                {
                    var inner = new Inner(new List<int> { 1, 2, 3, 4 }, 4);
                    var original = new Outer(inner, "label");

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Outer.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Nested.Values.SequenceEqual(inner.Values)) return "nested values mismatch";
                    if (restored.Nested.Count != 4) return "nested count mismatch";
                    if (restored.Label != "label") return "label mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ListOfNestedByteSerializableItems_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Item(int Id, double Weight);

            [ByteSerializable]
            public partial record Basket(List<Item> Items, string Owner);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Basket(new List<Item> { new Item(1, 1.1), new Item(2, 2.2), new Item(3, 3.3) }, "carrot");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Basket.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Items.Count != 3) return "count mismatch";
                    if (restored.Items[0].Id != 1 || restored.Items[2].Weight != 3.3) return "item value mismatch";
                    if (restored.Owner != "carrot") return "owner mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ListOfStrings_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Command4(List<string> Names);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Command4(new List<string> { "alpha", "", "béta 😀", "gamma" });
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Command4.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Names.SequenceEqual(original.Names)) return "Names mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ArrayOfStrings_RoundTrips()
    {
        const string source = """
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Labels(string[] Values, int Count);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Labels(new[] { "one", "two", "three" }, 3);
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Labels.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Values.SequenceEqual(original.Values)) return "Values mismatch";
                    if (restored.Count != 3) return "Count mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }
}
