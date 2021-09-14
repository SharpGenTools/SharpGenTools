namespace SharpGen.Runtime.Win32
{
    public partial struct ExceptionInfo
    {
        public unsafe void DoDeferredFillIn()
        {
            if (DeferredFillIn == default)
                return;

            __Native holder = default;
            holder.Code = Code;
            holder.Scode = Scode;
            Result result = ((delegate* unmanaged[Stdcall]<__Native*, int>) DeferredFillIn)(&holder);
            result.CheckError();
            __MarshalFrom(ref holder);
        }
    }
}