using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGen.CppModel
{
    public abstract class CppCallable : CppContainer
    {
        protected virtual CppCallingConvention DefaultCallingConvention => CppCallingConvention.CDecl;

        public CppReturnValue ReturnValue { get; set; }

        private CppCallingConvention callingConvention;

        public CppCallingConvention CallingConvention
        {
            get => callingConvention == CppCallingConvention.Unknown
                       ? callingConvention = DefaultCallingConvention
                       : callingConvention;
            set => callingConvention = value;
        }

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