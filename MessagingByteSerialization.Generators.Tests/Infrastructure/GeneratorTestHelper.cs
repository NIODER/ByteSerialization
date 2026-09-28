using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using MessagingByteSerialization;
using MessagingByteSerialization.Generators;

namespace MessagingByteSerialization.Generators.Tests.Infrastructure;

/// <summary>
/// Drives <see cref="ByteSerializableGenerator"/> against source text the same way MSBuild would, using
/// <c>TRUSTED_PLATFORM_ASSEMBLIES</c> to reference the full BCL without pulling in an extra test-only
/// package. No source (or "golden file") snapshot comparisons here on purpose - <see cref="EmitAndLoad"/>
/// lets tests compile the generator's output straight through to IL and execute it, which is a much
/// stronger correctness signal for a binary wire format than comparing generated text.
/// </summary>
internal static class GeneratorTestHelper
{
    private static readonly ImmutableArray<MetadataReference> BaseReferences = BuildBaseReferences();

    private static ImmutableArray<MetadataReference> BuildBaseReferences()
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();

        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedPlatformAssemblies is not null)
        {
            foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                if (File.Exists(path))
                {
                    builder.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        builder.Add(MetadataReference.CreateFromFile(typeof(ByteSerializableAttribute).Assembly.Location));

        return builder.ToImmutable();
    }

    public static GeneratedCompilation Compile(params string[] sources)
    {
        SyntaxTree[] trees = sources
            .Select(static s => CSharpSyntaxTree.ParseText(s, new CSharpParseOptions(LanguageVersion.Latest)))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: "MessagingByteSerialization.Generators.Tests.Generated_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: trees,
            references: BaseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ByteSerializableGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> _);

        return new GeneratedCompilation((CSharpCompilation)outputCompilation, driver.GetRunResult());
    }
}

internal sealed class GeneratedCompilation(CSharpCompilation compilation, GeneratorDriverRunResult runResult)
{
    public CSharpCompilation Compilation { get; } = compilation;

    public GeneratorDriverRunResult RunResult { get; } = runResult;

    public IEnumerable<Diagnostic> GeneratorDiagnostics => RunResult.Diagnostics;

    public IEnumerable<GeneratedSourceResult> GeneratedSources => RunResult.Results.SelectMany(static r => r.GeneratedSources);

    public string GetGeneratedText(string hintNameContains)
    {
        GeneratedSourceResult match = GeneratedSources.Single(s => s.HintName.Contains(hintNameContains, StringComparison.Ordinal));
        return match.SourceText.ToString();
    }

    public bool HasGeneratedSource(string hintNameContains) =>
        GeneratedSources.Any(s => s.HintName.Contains(hintNameContains, StringComparison.Ordinal));

    /// <summary>Emits the (generator-augmented) compilation to IL and loads it as a runnable in-memory assembly.</summary>
    public Assembly EmitAndLoad()
    {
        using var stream = new MemoryStream();
        EmitResult result = Compilation.Emit(stream);

        if (!result.Success)
        {
            string errors = string.Join(Environment.NewLine, result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException("Generated compilation failed to emit:" + Environment.NewLine + errors);
        }

        return Assembly.Load(stream.ToArray());
    }

    /// <summary>
    /// Emits, loads, and invokes <c>Harness.Verify()</c> (a parameterless static method returning
    /// <see cref="string"/>) that test sources are expected to declare - empty string means success,
    /// anything else is a human-readable failure description.
    /// </summary>
    public string RunHarness(string typeName = "TestNs.Harness", string methodName = "Verify")
    {
        Assembly assembly = EmitAndLoad();
        Type type = assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type '{typeName}' not found in generated assembly.");
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Static method '{methodName}' not found on '{typeName}'.");
        return (string)method.Invoke(null, null)!;
    }
}
