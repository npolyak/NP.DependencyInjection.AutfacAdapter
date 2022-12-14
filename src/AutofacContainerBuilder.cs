using Autofac;
using Autofac.Builder;
using Autofac.Core.Activators.ProvidedInstance;
using Autofac.Core.Lifetime;
using NP.DependencyInjection.Attributes;
using NP.DependencyInjection.Interfaces;
using NP.IoC.CommonImplementations;
using System.Reflection;

namespace NP.DependencyInjection.AutofacAdapter
{
    public class AutofacContainerBuilder : AbstractContainerBuilder, IContainerBuilder
    {
        ContainerBuilder _builder = new ContainerBuilder();
        Autofac.Core.Container? _container = null;

        Dictionary<FullContainerItemResolvingKey, object> _instances { get; } = new Dictionary<FullContainerItemResolvingKey, object>();

        List<FullContainerItemResolvingKey> _unresgisteredResolutionKeys =
            new List<FullContainerItemResolvingKey>();

        private void Preregister(Type resolvingType, object? resolutionKey = null)
        {
            FullContainerItemResolvingKey key = new FullContainerItemResolvingKey(resolvingType, resolutionKey);

            _unresgisteredResolutionKeys.RemoveAll(k => k == key);
        }

        private IRegistrationBuilder<object, IConcreteActivatorData, SingleRegistrationStyle> RegTypeImpl(Type resolvingType, Type typeToResolve, object? resolutionKey = null)
        {
            Preregister(resolvingType, resolutionKey);
            return _builder.Reg(resolvingType, typeToResolve, resolutionKey);
        }

        public override void RegisterType(Type resolvingType, Type typeToResolve, object? resolutionKey = null)
        {
            RegTypeImpl(resolvingType, typeToResolve, resolutionKey);
        }

        public override void RegisterSingletonType(Type resolvingType, Type typeToResolve, object? resolutionKey = null)
        {
            RegTypeImpl(resolvingType, typeToResolve, resolutionKey).SingleInstance();
        }

        private void RegisterMethodInfoCell
        (
            MethodBase factoryMethodInfo,
            bool isSingleton, 
            Type? resolvingType = null, 
            object? resolutionKey = null)
        {
            var cell = new ResolvingFactoryMethodInfoCell(factoryMethodInfo, isSingleton);
            resolvingType = factoryMethodInfo.GetAndCheckResolvingType(resolvingType);
            FullContainerItemResolvingKey key = new FullContainerItemResolvingKey(resolvingType, resolutionKey);
            RegisterSingletonInstance(typeof(IResolvingCell), cell, key);
        }

        protected override void RegisterAttributedType
        (
            Type resolvingType, 
            Type typeToResolve, 
            object? resolutionKey = null)
        {
            Preregister(resolvingType, resolutionKey);

            ConstructorInfo constructorInfo =
                typeToResolve
                    .GetConstructors()
                    .First(c => (c.GetCustomAttribute<CompositeConstructorAttribute>() != null) || (c.GetParameters().Count() == 0));

            if (constructorInfo.GetParameters().Length > 0)
            {
                RegisterMethodInfoCell(constructorInfo, false, resolvingType, resolutionKey);;
            }
            else
            {
                (_builder.Reg(resolvingType, typeToResolve, resolutionKey) as IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>)
                    .FindConstructorsWith(type => new[] { constructorInfo });
            }
        }

        protected override void RegisterAttributedSingletonType(Type resolvingType, Type typeToResolve, object? resolutionKey = null)
        {
            Preregister(resolvingType, resolutionKey);

            ConstructorInfo constructorInfo =
                typeToResolve
                    .GetConstructors()
                    .First(c => (c.GetCustomAttribute<CompositeConstructorAttribute>() != null) || (c.GetParameters().Count() == 0));


            if (constructorInfo.GetParameters().Length > 0)
            {
                RegisterMethodInfoCell(constructorInfo, true, resolvingType, resolutionKey); ;
            }
            else
            {
                (_builder.Reg(resolvingType, typeToResolve, resolutionKey) as IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>)
                .FindConstructorsWith(type =>
                {
                    return new[] { constructorInfo };
                }).SingleInstance();
            }
        }

        public override void RegisterSingletonInstance
        (
            Type resolvingType,
            object instance,
            object? resolutionKey = null)
        {
            Preregister(resolvingType, resolutionKey);

            FullContainerItemResolvingKey key = new FullContainerItemResolvingKey(resolvingType, resolutionKey);

            _instances[key] = instance;

            _builder.RegisterInstance(instance).RegImpl(resolvingType, resolutionKey);
        }

        public void RegisterSingletonFactoryMethod<TResolving>(Func<TResolving> resolvingFunc, object? resolutionKey = null)
        { 
            Preregister(typeof(TResolving), resolutionKey);
            _builder.Register(c => resolvingFunc()).RegImpl(typeof(TResolving), typeof(TResolving), resolutionKey).SingleInstance();
        }

        public void RegisterFactoryMethod<TResolving>(Func<TResolving> resolvingFunc, object? resolutionKey = null)
        {
            Preregister(typeof(TResolving), resolutionKey);
            _builder.Register(c => resolvingFunc()).RegImpl(typeof(TResolving), typeof(TResolving), resolutionKey);
        }

        public override void RegisterSingletonFactoryMethodInfo(MethodBase factoryMethodInfo, Type? resolvingType = null, object? resolutionKey = null)
        {
            Preregister(resolvingType!, resolutionKey);
            RegisterMethodInfoCell(factoryMethodInfo, true, resolvingType, resolutionKey);
        }

        public override void RegisterFactoryMethodInfo(MethodBase factoryMethodInfo, Type? resolvingType = null, object? resolutionKey = null)
        {

            Preregister(resolvingType!, resolutionKey);
            RegisterMethodInfoCell(factoryMethodInfo, false, resolvingType, resolutionKey);
        }

        public void UnRegister(Type resolvingType, object? resolutionKey)
        {
            FullContainerItemResolvingKey key = new FullContainerItemResolvingKey(resolvingType, resolutionKey);

            _instances.Remove(key);

            _unresgisteredResolutionKeys.Add(key);
        }

        public AutofacContainerBuilder()
        {
            SetAssemblyResolver();
        }

        private ContainerBuilder CreateBuilderFromContainer()
        {
            ContainerBuilder result = new ContainerBuilder();

            var components =
                _container
                    .ComponentRegistry
                        .Registrations
                            .Where(cr => cr.Activator.LimitType != typeof(LifetimeScope))
                            .Where(cr => _unresgisteredResolutionKeys
                                            .All
                                            (
                                                unreg => !unreg.MatchesService(cr.Services.FirstOrDefault())));

            foreach (var c in components)
            {
                if (c.Activator is ProvidedInstanceActivator activator)
                {
                    continue;
                }

                result.RegisterComponent(c);
            }

            foreach (var (key, instance) in _instances)
            {
                result.RegisterInstance(instance).RegImpl(key.ResolvingType, key.KeyObject);
            }

            foreach (var source in _container.ComponentRegistry.Sources)
            {
                result.RegisterSource(source);
            }

            return result;
        }

        public virtual IDependencyInjectionContainer Build()
        {
            _container = (Autofac.Core.Container)_builder.Build();
            _builder = CreateBuilderFromContainer();

            if (_unresgisteredResolutionKeys.Count > 0)
            {
                _container = (Autofac.Core.Container)_builder.Build();
                _builder = CreateBuilderFromContainer();
            }

            return new AutofacContainerAdapter(_container);
        }
    }
}
