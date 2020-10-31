using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SharpGen.CppModel
{
    [XmlType("callable")]
    public class CppCallable : CppElement
    {
        protected virtual CppCallingConvention DefaultCallingConvention => CppCallingConvention.CDecl;

        /// <summary>
        /// Gets or sets the type of the return.
        /// </summary>
        /// <value>The type of the return.</value>
        [XmlElement("return")]
        public CppReturnValue ReturnValue { get; set; }

        private CppCallingConvention callingConvention;
        /// <summary>
        /// Gets or sets the calling convention.
        /// </summary>
        /// <value>The calling convention.</value>
        [XmlAttribute("call-conv")]
        public CppCallingConvention CallingConvention
        {
            get
            {
                return (callingConvention == CppCallingConvention.Unknown)
                    ? (callingConvention = DefaultCallingConvention)
                    : callingConvention;
            }
            set
            {
                callingConvention = value;
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        [XmlIgnore]
        public IEnumerable<CppParameter> Parameters
        {
            get { return Iterate<CppParameter>(); }
        }

        protected internal override IEnumerable<CppElement> AllItems
        {
            get
            {
                var allElements = new List<CppElement>(Iterate<CppElement>());
                allElements.Add(ReturnValue);
                return allElements;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        [ExcludeFromCodeCoverage]
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

        public override string ToShortString()
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
    }
}
