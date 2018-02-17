using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Runtime
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeFromTypeListAttribute : Attribute
    {
        public ExcludeFromTypeListAttribute()
        {

        }
    }
}
