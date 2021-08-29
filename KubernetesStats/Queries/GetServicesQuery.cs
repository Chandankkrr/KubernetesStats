using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using KubernetesStats.Extension;
using KubernetesStats.Models;
using MediatR;
using Spectre.Console;

namespace KubernetesStats.Queries
{
    public class GetServicesQuery : K8, IRequest<Unit>
    {
    }

    public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, Unit>
    {
        public async Task<Unit> Handle(GetServicesQuery request, CancellationToken cancellationToken)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var client = new Kubernetes(config);
            var k8Namespace = request.Namespace;

            var services = await client.ListNamespacedServiceAsync(
                k8Namespace, cancellationToken: cancellationToken
            );

            var serviceItems = services.Items;
            
            if (!serviceItems.Any())
            {
                AnsiConsole.WriteLine($"No services found in {k8Namespace} namespace");
                return Unit.Value;
            }

            var grid = new Grid { Expand = false }
                .GenerateColumns(7)
                .AddRow("NAMESPACE", "NAME", "TYPE", "CLUSTER-IP", "EXTERNAL-IP", "PORT(S)", "AGE");

            foreach (var resultItem in serviceItems)
            {
                var metadata = resultItem.Metadata;
                var spec = resultItem.Spec;
                var age = (DateTime.UtcNow - metadata!.ManagedFields?[0]?.Time)?.Hours.ToString();

                grid.AddRow(metadata.NamespaceProperty, metadata.Name, spec.Type, spec.ClusterIP, "<none>",
                    string.Join(",", spec.Ports.Select(p => $"{p.TargetPort.Value}/{p.Protocol}")),
                    $"{(age != null ? $"{age}h" : "-")}");
            }

            var output = new Panel(grid)
                .Header("---K8s Cluster, Services Info---", Justify.Center)
                .BorderStyle(new Style(Color.NavajoWhite1, decoration: Decoration.Italic))
                .BorderColor(Color.Orange3)
                .Padding(1, 1, 1, 1)
                .RoundedBorder();

            AnsiConsole.WriteLine();
            AnsiConsole.Render(output);

            return Unit.Value;
        }
    }
}