using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using KubernetesStats.Models;
using KubernetesStats.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace KubernetesStats
{
    class Options
    {
        [Option("resource",
            Default = ResourceType.Services,
            HelpText = "K8s resource type")]
        public ResourceType ResourceFlag { get; set; }
    }

    class Program
    {
        private static ServiceProvider _serviceProvider;

        static void Main(string[] args)
        {
            ConfigureServices();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddHttpClient("K8sClient", client =>
            {
                client.BaseAddress = new Uri("https://kubernetes.docker.internal:6443/");
                client.DefaultRequestHeaders.Add("Authorization", $"bearer {Constants.Token}");
            }).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                // TODO remove
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            });
            
            services.AddMediatR(Assembly.GetExecutingAssembly());
            
            _serviceProvider = services.BuildServiceProvider();
        }

        private static void RunOptions(Options opts)
        {
            var mediator = _serviceProvider.GetService<IMediator>();

            var getServicesQuery = new GetServicesQuery
            {
                ResourceType = opts.ResourceFlag
            };

            Task.Run(async () => await mediator!.Send(getServicesQuery)).Wait();
        }
    }
}