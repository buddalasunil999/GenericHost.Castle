using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericHost.Castle
{
    public class WindsorHost
    {
        private readonly WindsorContainer windsorContainer = new WindsorContainer();
        private readonly Func<IEnumerable<IWindsorInstaller>> getAssemblies;
        private IHostBuilder hostBuilder;

        public WindsorHost(Func<IEnumerable<IWindsorInstaller>> getAssemblies)
        {
            this.getAssemblies = getAssemblies;
        }

        public IHostBuilder CreateBuilder(string environmentPrefix, string[] args,
            Action<IConfiguration, WindsorContainer> registerDependencies = null)
        {
            hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();

                IHostEnvironment env = hostingContext.HostingEnvironment;
                Console.WriteLine($"Using environment: {env.EnvironmentName}");

                configuration
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
                configuration.AddEnvironmentVariables(environmentPrefix);
                configuration.AddCommandLine(args);

                IConfigurationRoot configurationRoot = configuration.Build();
            })
            .UseServiceProviderFactory(new ServiceContainerFactory(windsorContainer))
            .ConfigureContainer<ServiceContainer>((hostContext, container) =>
            {
                windsorContainer.Kernel.Resolver.AddSubResolver(new ArrayResolver(windsorContainer.Kernel, true));
                windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(windsorContainer.Kernel, true));

                windsorContainer.Register(Component.For<IConfiguration>()
                    .Instance(hostContext.Configuration));
                if (getAssemblies != null)
                {
                    windsorContainer.Install(getAssemblies().ToArray());
                }

                registerDependencies?.Invoke(hostContext.Configuration, windsorContainer);
            })
            .UseConsoleLifetime();

            return hostBuilder;
        }

        public async Task Run()
        {
            try
            {
                using (var host = hostBuilder.Build())
                {
                    try
                    {
                        using (windsorContainer)
                        {
                            using (var scope = windsorContainer.BeginScope())
                            {
                                Console.WriteLine("Started");
                                await host.RunAsync();
                            }
                        }
                    }
                    catch (OperationCanceledException oce)
                    {
                        Console.WriteLine("OperationCanceled", oce);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error occurred", ex);
                    }

                    Console.WriteLine("Finished!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
