using SharpGen.Model;

namespace SharpGen.Generator
{
    internal abstract class StatementCodeGeneratorBase<T> : CodeGeneratorBase, IStatementCodeGenerator<T> where T : CsBase
    {
        protected StatementCodeGeneratorBase(Ioc ioc) : base(ioc)
        {
        }

        protected StatementSyntaxList NewStatementList => new(Ioc);
        protected MemberSyntaxList NewMemberList => new(Ioc);
    }
}