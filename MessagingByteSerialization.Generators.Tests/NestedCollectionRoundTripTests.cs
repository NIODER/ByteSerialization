using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class NestedCollectionRoundTripTests
{
    [Fact]
    public void ListOfLists_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<List<int>> Groups);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<List<int>>
                    {
                        new List<int> { 1, 2, 3 },
                        new List<int>(),
                        new List<int> { -1 },
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Groups.Count != original.Groups.Count) return "outer count mismatch";
                    for (int i = 0; i < original.Groups.Count; i++)
                    {
                        if (!restored.Groups[i].SequenceEqual(original.Groups[i])) return $"inner mismatch at {i}";
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
    public void JaggedArray_RoundTrips()
    {
        const string source = """
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Message(int[][] Matrix);

            public static class Harness
            {
                public static string Verify()
                {
                    // Deliberately uneven row lengths - a real 2D rectangular array would forbid this.
                    var original = new Message(new int[][]
                    {
                        new int[] { 1, 2, 3 },
                        new int[] { },
                        new int[] { 42 },
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Matrix.Length != original.Matrix.Length) return "outer length mismatch";
                    for (int i = 0; i < original.Matrix.Length; i++)
                    {
                        if (!restored.Matrix[i].SequenceEqual(original.Matrix[i])) return $"row mismatch at {i}";
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
    public void TripleJaggedArray_RoundTrips()
    {
        const string source = """
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Message(int[][][] Cube);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new int[][][]
                    {
                        new int[][] { new int[] { 1, 2 }, new int[] { 3 } },
                        new int[][] { },
                        new int[][] { new int[] { } },
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Cube.Length != original.Cube.Length) return "outer length mismatch";
                    for (int i = 0; i < original.Cube.Length; i++)
                    {
                        if (restored.Cube[i].Length != original.Cube[i].Length) return $"mid length mismatch at {i}";
                        for (int j = 0; j < original.Cube[i].Length; j++)
                        {
                            if (!restored.Cube[i][j].SequenceEqual(original.Cube[i][j])) return $"leaf mismatch at {i},{j}";
                        }
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
    public void ListOfArrays_And_ArrayOfLists_RoundTrip()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<int[]> ListOfArrays, List<string>[] ArrayOfLists);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(
                        new List<int[]> { new int[] { 1, 2 }, new int[] { }, new int[] { 3, 4, 5 } },
                        new List<string>[] { new List<string> { "a", "b" }, new List<string>() });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";

                    if (restored.ListOfArrays.Count != original.ListOfArrays.Count) return "ListOfArrays count mismatch";
                    for (int i = 0; i < original.ListOfArrays.Count; i++)
                    {
                        if (!restored.ListOfArrays[i].SequenceEqual(original.ListOfArrays[i])) return $"ListOfArrays mismatch at {i}";
                    }

                    if (restored.ArrayOfLists.Length != original.ArrayOfLists.Length) return "ArrayOfLists length mismatch";
                    for (int i = 0; i < original.ArrayOfLists.Length; i++)
                    {
                        if (!restored.ArrayOfLists[i].SequenceEqual(original.ArrayOfLists[i])) return $"ArrayOfLists mismatch at {i}";
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
    public void ListOfListsOfStrings_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<List<string>> Rows);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<List<string>>
                    {
                        new List<string> { "hello", "world éé" },
                        new List<string> { "" },
                        new List<string>(),
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Rows.Count != original.Rows.Count) return "count mismatch";
                    for (int i = 0; i < original.Rows.Count; i++)
                    {
                        if (!restored.Rows[i].SequenceEqual(original.Rows[i])) return $"row mismatch at {i}";
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
    public void ListOfListsOfNestedByteSerializable_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Point(int X, int Y);

            [ByteSerializable]
            public partial record Message(List<List<Point>> Grid);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<List<Point>>
                    {
                        new List<Point> { new Point(1, 1), new Point(2, 2) },
                        new List<Point>(),
                    });

                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Grid.Count != 2) return "outer count mismatch";
                    if (restored.Grid[0].Count != 2) return "inner count mismatch";
                    if (restored.Grid[0][1].X != 2 || restored.Grid[0][1].Y != 2) return "value mismatch";
                    if (restored.Grid[1].Count != 0) return "expected empty inner list";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }

    [Fact]
    public void EmptyOuterList_RoundTrips()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record Message(List<List<int>> Groups);

            public static class Harness
            {
                public static string Verify()
                {
                    var original = new Message(new List<List<int>>());
                    byte[] bytes = original.ToBytes();
                    if (bytes.Length != original.GetByteSize()) return "size mismatch";

                    var restored = Message.FromBytes(bytes, out int read);
                    if (read != bytes.Length) return "bytesRead mismatch";
                    if (restored.Groups.Count != 0) return "expected empty outer list";

                    return "";
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal("", result.RunHarness());
    }
}
