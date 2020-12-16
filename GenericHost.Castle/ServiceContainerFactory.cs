using Castle.Windsor;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GenericHost.Castle
{
    public class ServiceContainerFactory :
        IServiceProviderFactory<ServiceContainer>
    {
        private WindsorContainer container;

        public ServiceContainerFactory(WindsorContainer container)
        {
            this.container = container;
        }

        public ServiceContainer CreateBuilder(
            IServiceCollection services)
        {
            return new ServiceContainer(services);
        }

        public IServiceProvider CreateServiceProvider(
            ServiceContainer containerBuilder)
        {
            return WindsorRegistrationHelper.CreateServiceProvider(container, containerBuilder.Services);
        }
    }
}
