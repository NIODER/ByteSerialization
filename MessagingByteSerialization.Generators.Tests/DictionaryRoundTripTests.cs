using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class DictionaryRoundTripTests
{
    [Fact]
    public void StringKeyIntValue_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(string Name, Dictionary<string, int> Scores);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message("scoreboard", new Dictionary<string, int>
                    {
                        ["alice"] = 10,
                        ["bob"] = -5,
                        ["carol"] = 0,
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Name != "scoreboard") return "Name mismatch";
                    if (restored.Scores.Count != original.Scores.Count) return "count mismatch";
                    if (!original.Scores.All(kvp => restored.Scores.TryGetValue(kvp.Key, out int v) && v == kvp.Value)) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void IntKeyStringValue_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Message(Dictionary<int, string> Names);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<int, string>
                    {
                        [1] = "one",
                        [2] = "two éé",
                        [3] = "",
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!original.Names.All(kvp => restored.Names.TryGetValue(kvp.Key, out string? v) && v == kvp.Value)) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void EmptyDictionary_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(Dictionary<string, int> Scores);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<string, int>());
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Scores.Count != 0) return "expected empty dictionary";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void NestedByteSerializableValue_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Point(int X, int Y);

            [ByteSerializable]
            public partial record Message(Dictionary<string, Point> Points);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<string, Point>
                    {
                        ["origin"] = new Point(0, 0),
                        ["corner"] = new Point(10, 20),
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!original.Points.All(kvp => restored.Points.TryGetValue(kvp.Key, out Point v) && v.X == kvp.Value.X && v.Y == kvp.Value.Y)) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void EnumKey_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            public enum Priority { Low, Medium, High }

            [ByteSerializable]
            public partial record Message(Dictionary<Priority, int> Counts);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<Priority, int>
                    {
                        [Priority.Low] = 1,
                        [Priority.High] = 3,
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!original.Counts.All(kvp => restored.Counts.TryGetValue(kvp.Key, out int v) && v == kvp.Value)) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ListValue_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(Dictionary<string, List<int>> Groups);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<string, List<int>>
                    {
                        ["evens"] = new List<int> { 2, 4, 6 },
                        ["empty"] = new List<int>(),
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Groups.Count != original.Groups.Count) return "count mismatch";
                    if (!original.Groups.All(kvp => restored.Groups.TryGetValue(kvp.Key, out List<int>? v) && v.SequenceEqual(kvp.Value))) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void ArrayValue_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Message(Dictionary<int, string[]> Tags);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<int, string[]>
                    {
                        [1] = new[] { "a", "b" },
                        [2] = new string[0],
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!original.Tags.All(kvp => restored.Tags.TryGetValue(kvp.Key, out string[]? v) && v.SequenceEqual(kvp.Value))) return "entries mismatch";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void FixedSizeCustomTypeKey_IsSupported()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Coordinate(int X, int Y);

            [ByteSerializable]
            public partial record Message(Dictionary<Coordinate, string> Labels);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new Dictionary<Coordinate, string>
                    {
                        [new Coordinate(0, 0)] = "origin",
                        [new Coordinate(1, 2)] = "corner",
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (!original.Labels.All(kvp => restored.Labels.TryGetValue(kvp.Key, out string? v) && v == kvp.Value))
                    {
                        return "entries mismatch";
                    }

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void VariableSizeCustomTypeKey_ReportsMbs008()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record VariableKey(string Name, int Value);

            [ByteSerializable]
            public partial record BadMessage(Dictionary<VariableKey, int> Counts);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS008");
        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS002");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void ListKey_IsUnsupported()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record BadMessage(Dictionary<List<int>, int> Counts);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS002");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }
}
