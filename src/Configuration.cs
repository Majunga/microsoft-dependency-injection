﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Lifetime;
using Unity.Microsoft.DependencyInjection.Lifetime;

namespace Unity.Microsoft.DependencyInjection
{
    internal static class Configuration
    {
        internal static IUnityContainer AddServices(this IUnityContainer container, IServiceCollection services)
        {
            var lifetime = ((UnityContainer)container).Configure<MdiExtension>().Lifetime;
            var registerFunc = ((UnityContainer)container).Register;

            ((UnityContainer)container).Register = ((UnityContainer)container).AppendNew;

            foreach (var descriptor in services) container.Register(descriptor, lifetime);

            ((UnityContainer)container).Register = registerFunc;

            return container;
        }


        internal static void Register(this IUnityContainer container,
            ServiceDescriptor serviceDescriptor, ILifetimeContainer lifetime)
        {
            if (serviceDescriptor.IsKeyedService)
            {
                if (serviceDescriptor.KeyedImplementationType != null)
                {
                    var name = (string)serviceDescriptor.ServiceKey;
                    container.RegisterType(serviceDescriptor.ServiceType,
                                           serviceDescriptor.KeyedImplementationType,
                                           name,
                                           (ITypeLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else if (serviceDescriptor.KeyedImplementationFactory != null)
                {
                    var name = (string)serviceDescriptor.ServiceKey;
                    container.RegisterFactory(serviceDescriptor.ServiceType,
                            name,
                            scope =>
                            {
                                var serviceProvider = scope.Resolve<IServiceProvider>();
                                var instance = serviceDescriptor.KeyedImplementationFactory(serviceProvider, name);
                                return instance;
                            },
                           (IFactoryLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else if (serviceDescriptor.KeyedImplementationInstance != null)
                {
                    var name = (string)serviceDescriptor.ServiceKey;
                    container.RegisterInstance(serviceDescriptor.ServiceType,
                               name,
                               serviceDescriptor.KeyedImplementationInstance,
                               (IInstanceLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else
                {
                    throw new InvalidOperationException("Unsupported keyed registration type");
                }
            }
            else
            {
                if (serviceDescriptor.ImplementationType != null)
                {
                    var name = serviceDescriptor.ServiceType.IsGenericTypeDefinition ? UnityContainer.All : null;
                    container.RegisterType(serviceDescriptor.ServiceType,
                                           serviceDescriptor.ImplementationType,
                                           name,
                                           (ITypeLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else if (serviceDescriptor.ImplementationFactory != null)
                {
                    container.RegisterFactory(serviceDescriptor.ServiceType,
                                            null,
                                            scope =>
                                            {
                                                var serviceProvider = scope.Resolve<IServiceProvider>();
                                                var instance = serviceDescriptor.ImplementationFactory(serviceProvider);
                                                return instance;
                                            },
                                           (IFactoryLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else if (serviceDescriptor.ImplementationInstance != null)
                {
                    container.RegisterInstance(serviceDescriptor.ServiceType,
                                               null,
                                               serviceDescriptor.ImplementationInstance,
                                               (IInstanceLifetimeManager)serviceDescriptor.GetLifetime(lifetime));
                }
                else
                {
                    throw new InvalidOperationException("Unsupported registration type");
                }
            }
        }


        internal static LifetimeManager GetLifetime(this ServiceDescriptor serviceDescriptor, ILifetimeContainer lifetime)
        {
            switch (serviceDescriptor.Lifetime)
            {
                case ServiceLifetime.Scoped:
                    return new HierarchicalLifetimeManager();
                case ServiceLifetime.Singleton:
                    return new InjectionSingletonLifetimeManager(lifetime);
                case ServiceLifetime.Transient:
                    return new InjectionTransientLifetimeManager();
                default:
                    throw new NotImplementedException(
                        $"Unsupported lifetime manager type '{serviceDescriptor.Lifetime}'");
            }
        }


        internal static bool CanResolve(this IUnityContainer container, Type type)
        {
            var info = type.GetTypeInfo();

            if (info.IsClass && !info.IsAbstract)
            {
                if (typeof(Delegate).GetTypeInfo().IsAssignableFrom(info) || typeof(string) == type || info.IsEnum
                    || type.IsArray || info.IsPrimitive)
                {
                    return container.IsRegistered(type);
                }
                return true;
            }

            if (info.IsGenericType)
            {
                var gerericType = type.GetGenericTypeDefinition();
                if ((gerericType == typeof(IEnumerable<>)) ||
                    container.IsRegistered(gerericType))
                {
                    return true;
                }
            }

            return container.IsRegistered(type);
        }
    }
}