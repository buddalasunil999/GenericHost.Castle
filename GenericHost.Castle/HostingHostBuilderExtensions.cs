using log4net;
using log4net.Config;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;

namespace GenericHost.Castle
{
    public static class HostingHostBuilderExtensions
    {
        public static IHostBuilder ConfigureLog4Net(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, collection) =>
            {
                var fileName = context.HostingEnvironment.IsDevelopment() ? "log4net.config" :
                $"log4net.{context.HostingEnvironment.EnvironmentName}.config";

                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo(fileName));
            });
        }
    }
}
