using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class ByteSerializableAsTests
{
    [Fact]
    public void CastToString_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public sealed class Tag
            {
                public string Value { get; }
                public Tag(string value) => Value = value;
                public static implicit operator string(Tag t) => t.Value;
                public static implicit operator Tag(string s) => new Tag(s);
            }

            [ByteSerializable]
            public partial record Message(int Id, [property: ByteSerializableAs<string>] Tag Label);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(1, new Tag("hello world"));
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Id != 1) return "Id mismatch";
                    if (restored.Label.Value != "hello world") return "Label mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void CastToInt_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public readonly struct Meters
            {
                public int Value { get; }
                public Meters(int value) => Value = value;
                public static implicit operator int(Meters m) => m.Value;
                public static implicit operator Meters(int v) => new Meters(v);
            }

            [ByteSerializable]
            public partial record struct Message([property: ByteSerializableAs<int>] Meters Distance, string Name);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Meters(42), "track");
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Distance.Value != 42) return "Distance mismatch";
                    if (restored.Name != "track") return "Name mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void CastToFixedSizeByteSerializableType_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Point(int X, int Y);

            public readonly struct Location
            {
                public Point AsPoint { get; }
                public Location(Point p) => AsPoint = p;
                public static implicit operator Point(Location l) => l.AsPoint;
                public static implicit operator Location(Point p) => new Location(p);
            }

            [ByteSerializable]
            public partial record Message([property: ByteSerializableAs<Point>] Location Loc);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Location(new Point(3, 4)));
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != Message.FixedByteSize) return "expected fixed-size type";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Loc.AsPoint.X != 3 || restored.Loc.AsPoint.Y != 4) return "value mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void CastToList_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            public sealed class IntBag
            {
                private readonly List<int> items;
                public IntBag(List<int> items) => this.items = items;
                public IReadOnlyList<int> Items => items;
                public static implicit operator List<int>(IntBag b) => b.items;
                public static implicit operator IntBag(List<int> l) => new IntBag(l);
            }

            [ByteSerializable]
            public partial record Message([property: ByteSerializableAs<List<int>>] IntBag Bag);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new IntBag(new List<int> { 1, 2, 3 }));
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!restored.Bag.Items.SequenceEqual(original.Bag.Items)) return "Bag mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void MissingConversion_ReportsMbs009()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public sealed class NoConversions
            {
                public string Value { get; set; } = "";
            }

            [ByteSerializable]
            public partial record BadMessage([property: ByteSerializableAs<string>] NoConversions Data);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS009");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void OneWayConversionOnly_ReportsMbs009()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public sealed class OneWay
            {
                public string Value { get; set; } = "";
                public static implicit operator string(OneWay o) => o.Value;
                // No conversion back from string to OneWay.
            }

            [ByteSerializable]
            public partial record BadMessage([property: ByteSerializableAs<string>] OneWay Data);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS009");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void UnsupportedTargetType_ReportsMbs010()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public sealed class Boxed
            {
                public object Value { get; set; } = new object();
                public static implicit operator object(Boxed b) => b.Value;
                public static implicit operator Boxed(object o) => new Boxed { Value = o };
            }

            [ByteSerializable]
            public partial record BadMessage([property: ByteSerializableAs<object>] Boxed Data);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS010");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void CastOnFieldMember_RoundTrips()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public readonly struct Meters
            {
                public int Value { get; }
                public Meters(int value) => Value = value;
                public static implicit operator int(Meters m) => m.Value;
                public static implicit operator Meters(int v) => new Meters(v);
            }

            [ByteSerializable]
            public partial class MessageClass
            {
                [ByteSerializableAs<int>]
                public Meters Distance;
            }

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new MessageClass { Distance = new Meters(7) };
                    byte[] bytes = original.ToBytes();
                    var restored = MessageClass.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Distance.Value != 7) return "Distance mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }
}
