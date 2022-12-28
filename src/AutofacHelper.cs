using Autofac.Builder;
using Autofac;
using NP.IoC.CommonImplementations;
using Autofac.Core;

namespace NP.DependencyInjection.AutofacAdapter
{
    internal static class AutofacHelper
    {
        internal static IRegistrationBuilder<TImplement, IConcreteActivatorData, SingleRegistrationStyle> RegImpl<TImplement>
        (
            this IRegistrationBuilder<TImplement, IConcreteActivatorData, SingleRegistrationStyle> intermediate,
            Type resolvingType,
            Type typeToResolve,
            object? resolutionKey = null)
        {
            if (resolutionKey == null)
            {
                return (resolvingType == typeToResolve) ? intermediate.AsSelf() : intermediate.As(resolvingType);
            }
            else
            {
                return intermediate.Keyed(resolutionKey, resolvingType);
            }
        }

        internal static IRegistrationBuilder<TImplement, IConcreteActivatorData, SingleRegistrationStyle> RegImpl<TImplement>
        (
            this IRegistrationBuilder<TImplement, IConcreteActivatorData, SingleRegistrationStyle> intermediate,
            Type resolvingType,
            object? resolutionKey = null)
        {
            return intermediate.RegImpl(resolvingType, typeof(TImplement), resolutionKey);
        }

        internal static IRegistrationBuilder<TImplement, IConcreteActivatorData, SingleRegistrationStyle> Reg<TImplement>(this ContainerBuilder builder, Type resolvingType, object? resolutionKey = null)
        {
            return builder.RegisterType<TImplement>().RegImpl(resolvingType, resolutionKey);
        }

        internal static IRegistrationBuilder<object, IConcreteActivatorData, SingleRegistrationStyle> Reg(this ContainerBuilder builder, Type resolvingType, Type typeToResolve, object? resolutionKey = null)
        {
            return builder.RegisterType(typeToResolve).RegImpl(resolvingType, typeToResolve, resolutionKey);
        }

        public static bool MatchesService<TKey>(this FullContainerItemResolvingKey<TKey> key, Service? service)
        {
            if (service is KeyedService keyedService)
            {
                return key.KeyObject.ObjEquals(keyedService.ServiceKey) && key.ResolvingType == keyedService.ServiceType;
            }
            else if (service is TypedService typedService)
            {
                return key.KeyObject == null && key.ResolvingType == typedService.ServiceType;
            }

            return false;   
        }
    }
}
