using Autofac;
using NP.DependencyInjection.Interfaces;
using NP.IoC.CommonImplementations;

namespace NP.DependencyInjection.AutofacAdapter
{
    public class AutofacContainerAdapter :
        AbstractContainer<object?>, 
        IDependencyInjectionContainer<object?>,
        IObjComposer
    {
        public Autofac.Core.Container AutofacContainer { get; }

        private object? ResolveObj(FullContainerItemResolvingKey<object?> fullResolvingKey)
        {
            if (fullResolvingKey.KeyObject == null)
            {
                return AutofacContainer.ResolveOptional(fullResolvingKey.ResolvingType);
            }
            else
            {
                if (!AutofacContainer.IsRegisteredWithKey(fullResolvingKey.KeyObject, fullResolvingKey.ResolvingType))
                {
                    if (AutofacContainer.IsRegisteredWithKey((fullResolvingKey), typeof(IResolvingCell)))
                    {
                        return AutofacContainer.ResolveKeyed(fullResolvingKey, typeof(IResolvingCell));
                    }
                    else
                    {
                        return null;
                    }
                }
                return AutofacContainer.ResolveKeyed(fullResolvingKey.KeyObject, fullResolvingKey.ResolvingType);
            }
        }

        public AutofacContainerAdapter(Autofac.Core.Container container)
        {
            AutofacContainer = container;
        }

        protected override object? ResolveKey(FullContainerItemResolvingKey<object?> fullResolvingKey)
        {
            object? obj = ResolveObj(fullResolvingKey);

            if (obj is IResolvingCell cell)
            {
                return cell.GetObj(this);
            }
            else if (obj != null)
            {
                ComposeObject(obj);
            }

            return obj; 
        }
    }
}
