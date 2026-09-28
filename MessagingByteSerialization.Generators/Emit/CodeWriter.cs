using System;
using System.Text;

namespace MessagingByteSerialization.Generators.Emit;

/// <summary>Minimal indentation-tracking text builder used to keep the emitter methods readable.</summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder builder = new();
    private int indent;

    public void Line(string text = "")
    {
        if (text.Length == 0)
        {
            builder.Append('\n');
            return;
        }

        builder.Append(' ', indent * 4).Append(text).Append('\n');
    }

    public void Indent() => indent++;

    public void Unindent() => indent--;

    /// <summary>Writes <paramref name="header"/>, then an indented <c>{ ... }</c> block.</summary>
    public IDisposable Block(string header)
    {
        Line(header);
        return Scope();
    }

    /// <summary>Writes an indented <c>{ ... }</c> block with no header line.</summary>
    public IDisposable Scope()
    {
        Line("{");
        Indent();
        return new BlockScope(this);
    }

    public override string ToString() => builder.ToString();

    private sealed class BlockScope(CodeWriter writer) : IDisposable
    {
        public void Dispose()
        {
            writer.Unindent();
            writer.Line("}");
        }
    }
}
