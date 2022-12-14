using NP.IoC.CommonImplementations;
using System.Reflection;

namespace NP.DependencyInjection.AutofacAdapter
{
    internal class ResolvingFactoryMethodInfoCell : IResolvingCell
    {
        public bool IsSingleton { get; }

        public MethodBase FactoryMethodInfo { get; }

        public ResolvingFactoryMethodInfoCell(MethodBase factoryMethodInfo, bool isSingleton)
        {
            FactoryMethodInfo = factoryMethodInfo;
            IsSingleton = isSingleton;
        }

        private object? _obj;
        public object? GetObj(AutofacContainerAdapter objectComposer)
        {
            // if singleton - only assign first time
            // otherwise (if not singleton) - every time
            if ((_obj == null) || (!IsSingleton))
            {
                // create object
                _obj = objectComposer.CreateAndComposeObjFromMethod(FactoryMethodInfo);
            }

            return _obj;
        }
    }
}
