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
        private readonly WindsorContainer container = new WindsorContainer();
        private readonly IEnumerable<IWindsorInstaller> assemblies;
        private readonly string[] args;
        private IHostBuilder hostBuilder;

        public WindsorHost(IEnumerable<IWindsorInstaller> assemblies, string[] args)
        {
            this.assemblies = assemblies;
            this.args = args;
        }

        public IHostBuilder CreateBuilder(string environmentPrefix, Action<IConfiguration, WindsorContainer> registerDependencies)
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
            .UseServiceProviderFactory(new ServiceContainerFactory(container))
            .ConfigureContainer<ServiceContainer>((hostContext, container) =>
            {
                this.container.Kernel.Resolver.AddSubResolver(new ArrayResolver(this.container.Kernel, true));
                this.container.Kernel.Resolver.AddSubResolver(new CollectionResolver(this.container.Kernel, true));

                this.container.Install(assemblies.ToArray());
                registerDependencies(hostContext.Configuration, this.container);
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
                        using (container)
                        {
                            using (var scope = container.BeginScope())
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
