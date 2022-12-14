namespace AutofacAdapter
{
    internal interface IResolvingCell
    {
        object? GetObj(AutofacContainerAdapter objectComposer);
    }
}
