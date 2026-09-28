// netstandard2.0 has no built-in support for `init` accessors; the compiler only needs this marker
// type to exist somewhere in the compilation to allow the syntax. Internal, generator-only polyfill.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
