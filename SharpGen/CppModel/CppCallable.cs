using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpGen.CppModel
{
    public abstract class CppCallable : CppContainer
    {
        public CppReturnValue ReturnValue { get; set; }

        public CallingConvention CallingConvention { get; set; } = CallingConvention.Cdecl;

        public MethodDeclarationSyntax Roslyn { get; set; }

        public IEnumerable<CppParameter> Parameters => Iterate<CppParameter>();

        protected internal override IEnumerable<CppElement> AllItems => Iterate<CppElement>().Append(ReturnValue);

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(ReturnValue);
            builder.Append(" ");
            if (Parent is CppInterface)
            {
                builder.Append(Parent.Name);
                builder.Append("::");
            }

            builder.Append(Name);
            builder.Append("(");

            uint i = 0;
            foreach (var cppParameter in Parameters)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }

                builder.Append(cppParameter);
                i++;
            }

            builder.Append(")");
            return builder.ToString();
        }

        public string ToShortString()
        {
            var builder = new StringBuilder();
            if (Parent is CppInterface)
            {
                builder.Append(Parent.Name);
                builder.Append("::");
            }

            builder.Append(Name);
            return builder.ToString();
        }

        protected CppCallable(string name) : base(name)
        {
        }
    }
}