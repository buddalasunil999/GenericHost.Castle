using Microsoft.Extensions.DependencyInjection;

namespace GenericHost.Castle
{
    public class ServiceContainer
    {
        public IServiceCollection Services { get; }

        public ServiceContainer(IServiceCollection services)
        {
            Services = services;
        }
    }
}
