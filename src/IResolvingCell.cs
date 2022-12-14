namespace NP.DependencyInjection.AutofacAdapter
{
    internal interface IResolvingCell
    {
        object? GetObj(AutofacContainerAdapter objectComposer);
    }
}
