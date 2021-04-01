using System.IO;
using ClangSharp;
using ClangSharp.Interop;

namespace SharpGen.Platform
{
    public interface IClangSharpHost
    {
        Stream GetOutputStream(bool isTestOutput, string name, string extension);
        bool IsIncludedFileOrLocation(Cursor cursor, CXFile file, CXSourceLocation location);
    }
}