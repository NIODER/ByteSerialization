using System.Linq;
using MessagingByteSerialization.Generators.Tests.Infrastructure;

namespace MessagingByteSerialization.Generators.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void UnsupportedMemberType_ReportsMbs002AndSkipsType()
    {
        const string source = """
            using System.Collections.Generic;
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class BadMessage
            {
                public int Id { get; set; }
                public Dictionary<List<int>, int> Nested { get; set; } = new();
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS002");
        Assert.False(result.HasGeneratedSource("BadMessage"));
    }

    [Fact]
    public void NotPartial_ReportsMbs001()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public class NotPartialMessage
            {
                public int Id { get; set; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS001");
        Assert.False(result.HasGeneratedSource("NotPartialMessage"));
    }

    [Fact]
    public void GenericType_ReportsMbs004()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class GenericMessage<T>
            {
                public int Id { get; set; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS004");
        Assert.False(result.HasGeneratedSource("GenericMessage"));
    }

    [Fact]
    public void NestedType_ReportsMbs005()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            public partial class Container
            {
                [ByteSerializable]
                public partial class NestedMessage
                {
                    public int Id { get; set; }
                }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS005");
        Assert.False(result.HasGeneratedSource("NestedMessage"));
    }

    [Fact]
    public void NoSuitableConstructor_ReportsMbs003()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class ReadOnlyMessage
            {
                public ReadOnlyMessage(int id) => Id = id;

                public int Id { get; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS003");
        Assert.False(result.HasGeneratedSource("ReadOnlyMessage"));
    }

    [Fact]
    public void CircularReference_ReportsMbs006AndSkipsBothTypes()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial class NodeA
            {
                public int X { get; set; }
                public NodeB? Inner { get; set; }
            }

            [ByteSerializable]
            public partial class NodeB
            {
                public int Y { get; set; }
                public NodeA? Inner { get; set; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS006");
        Assert.False(result.HasGeneratedSource("NodeA"));
        Assert.False(result.HasGeneratedSource("NodeB"));
    }

    [Fact]
    public void EmbeddingNonPartialByteSerializableType_SkipsOuterTypeToo()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public class BrokenNested
            {
                public int Id { get; set; }
            }

            [ByteSerializable]
            public partial class Outer
            {
                public BrokenNested? Nested { get; set; }
            }
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS001");
        Assert.Contains(result.GeneratorDiagnostics, d => d.Id == "MBS002");
        Assert.False(result.HasGeneratedSource("Outer.ByteSerializable"));
    }

    [Fact]
    public void ValidType_ProducesNoDiagnostics()
    {
        const string source = """
            using MessagingByteSerialization;

            namespace TestNs;

            [ByteSerializable]
            public partial record struct Point(int X, int Y);
            """;

        GeneratedCompilation result = GeneratorTestHelper.Compile(source);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.HasGeneratedSource("Point"));
    }
}
