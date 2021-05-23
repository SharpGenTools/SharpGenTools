using System;

namespace SharpGen.Runtime
{
    public interface IExceptionCallback
    {
        void RaiseException(Exception e);
    }
}