using NP.IoC.CommonImplementations;

namespace NP.DependencyInjection.AutofacAdapter
{
    internal interface IResolvingCell
    {
        object? GetObj(IObjComposer objectComposer);
    }
}
