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
        private readonly IEnumerable<IWindsorInstaller> assemblies;
        private readonly string[] args;
        private IHostBuilder hostBuilder;

        public WindsorHost(IEnumerable<IWindsorInstaller> assemblies, string[] args)
        {
            this.assemblies = assemblies;
            this.args = args;
        }

        public IHostBuilder CreateBuilder(string environmentPrefix, Action<IConfiguration, WindsorContainer> registerDependencies = null)
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
                this.windsorContainer.Kernel.Resolver.AddSubResolver(new ArrayResolver(this.windsorContainer.Kernel, true));
                this.windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(this.windsorContainer.Kernel, true));

                this.windsorContainer.Register(Component.For<IConfiguration>()
                    .Instance(hostContext.Configuration));
                this.windsorContainer.Install(assemblies.ToArray());

                if (registerDependencies != null)
                {
                    registerDependencies(hostContext.Configuration, this.windsorContainer);
                }
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
