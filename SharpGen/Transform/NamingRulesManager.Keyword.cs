using System.Linq;

namespace SharpGen.Transform;

public sealed partial class NamingRulesManager
{
    /// <summary>
    /// Protect the name from all C# reserved words.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    private static string UnKeyword(string name)
    {
        if (!IsKeyword(name))
            return name;

        if (name == "string")
            return "text";

        return '@' + name;
    }

    /// <summary>
    /// Checks if a given string is a C# keyword. 
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>true if the name is a C# keyword; false otherwise.</returns>
    private static bool IsKeyword(string name) => CSharpKeywords.Contains(name);

    /// <summary>
    /// Reserved C# keywords.
    /// </summary>
    private static readonly string[] CSharpKeywords =
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "volatile",
        "void",
        "while",
    };
}