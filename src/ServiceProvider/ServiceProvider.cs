﻿using Microsoft.Extensions.DependencyInjection;
using System;
using Unity.Lifetime;
using Unity.Microsoft.DependencyInjection.Lifetime;

namespace Unity.Microsoft.DependencyInjection
{
    public class ServiceProvider : IServiceProvider,
                                   ISupportRequiredService,
                                   IKeyedServiceProvider,
                                   IServiceScopeFactory,
                                   IServiceScope,
                                   IDisposable
    {
        private IUnityContainer _container;

#if DEBUG
        private string id = Guid.NewGuid().ToString();
#endif


        internal ServiceProvider(IUnityContainer container)
        {
            _container = container;
            _container.RegisterInstance<IServiceScope>(this, new ExternallyControlledLifetimeManager());
            _container.RegisterInstance<IServiceProvider>(this, new ServiceProviderLifetimeManager(this));
            _container.RegisterInstance<ISupportRequiredService>(this, new ExternallyControlledLifetimeManager());
            _container.RegisterInstance<IKeyedServiceProvider>(this, new ExternallyControlledLifetimeManager());

            if (container.IsRegistered<IServiceScopeFactory>() == false)
                _container.RegisterInstance<IServiceScopeFactory>(this, new ExternallyControlledLifetimeManager());
        }

        #region IServiceProvider

        public object GetService(Type serviceType)
        {
            if (null == _container)
                throw new ObjectDisposedException(nameof(IServiceProvider));

            try
            {
                return _container.Resolve(serviceType, null);
            }
            catch { /* Ignore */}

            return null;
        }

        public object GetRequiredService(Type serviceType)
        {
            if (null == _container)
                throw new ObjectDisposedException(nameof(IServiceProvider));

            return _container.Resolve(serviceType, null);
        }

        #endregion


        #region IServiceScopeFactory

        public IServiceScope CreateScope()
        {
            return new ServiceProvider(_container.CreateChildContainer());
        }

        #endregion

        #region IKeyedServiceProvider

        public object GetKeyedService(Type serviceType, object serviceKey)
        {
            if (null == _container)
                throw new ObjectDisposedException(nameof(IServiceProvider));

            return _container.Resolve(serviceType, (string)serviceKey, null);
        }

        public object GetRequiredKeyedService(Type serviceType, object serviceKey)
        {
            if (null == _container)
                throw new ObjectDisposedException(nameof(IServiceProvider));

            try
            {
                return _container.Resolve(serviceType, (string)serviceKey, null);
            }
            catch { /* Ignore */}

            return null;
        }

        #endregion


        #region IServiceScope

        IServiceProvider IServiceScope.ServiceProvider => this;

        #endregion


        #region Public Members

        public static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return new ServiceProvider(new UnityContainer()
                .AddExtension(new MdiExtension())
                .AddServices(services));
        }

        public static explicit operator UnityContainer(ServiceProvider c)
        {
            return (UnityContainer)c._container;
        }

        #endregion


        #region Disposable

        protected virtual void Dispose(bool disposing)
        {
            IDisposable disposable = _container;
            _container = null;
            disposable?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        #endregion
    }
}
