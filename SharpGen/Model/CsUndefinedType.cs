namespace SharpGen.Model
{
    public sealed class CsUndefinedType : CsTypeBase
    {
        public CsUndefinedType(string name) : base(null, name)
        {
        }

        public override bool IsBlittable => false;
    }
}
