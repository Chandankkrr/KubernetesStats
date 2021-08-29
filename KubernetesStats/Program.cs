using System;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using KubernetesStats.Models;
using KubernetesStats.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace KubernetesStats
{
    class Program
    {
        private static ServiceProvider _serviceProvider;

        static void Main(string[] args)
        {
            ConfigureServices();

            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(RunOptions);
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddMediatR(Assembly.GetExecutingAssembly());

            _serviceProvider = services.BuildServiceProvider();
        }

        private static void RunOptions(CommandLineOptions opts)
        {
            var mediator = _serviceProvider.GetService<IMediator>();

            var resourceType = opts.ResourceFlag;

            object query = resourceType switch
            {
                ResourceType.Services => new GetServicesQuery
                {
                    Namespace = opts.NamespaceFlag
                },
                ResourceType.Pods => new GetPodsQuery
                {
                    Namespace = opts.NamespaceFlag
                },
                _ => throw new ArgumentException("Invalid enum value for command", nameof(resourceType))
            };

            try
            {
                Task.Run(async () => await mediator!.Send(query)).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}