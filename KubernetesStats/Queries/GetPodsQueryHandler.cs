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
    public class GetPodsQuery : K8, IRequest<Unit>
    {
    }

    public class GetPodsQueryHandler : IRequestHandler<GetPodsQuery, Unit>
    {
        public async Task<Unit> Handle(GetPodsQuery request, CancellationToken cancellationToken)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var client = new Kubernetes(config);
            var k8Namespace = request.Namespace;

            var pods = await client.ListNamespacedPodAsync(k8Namespace, cancellationToken: cancellationToken);
            var podItems = pods.Items;

            if (!podItems.Any())
            {
                AnsiConsole.WriteLine($"No pods found in {k8Namespace} namespace");
                return Unit.Value;
            }

            var grid = new Grid { Expand = false }
                .GenerateColumns(6)
                .AddRow("NAMESPACE", "NAME", "READY", "STATUS", "RESTARTS", "AGE");

            foreach (var resultItem in podItems)
            {
                var metadata = resultItem.Metadata;
                var status = resultItem.Status;

                var age = (DateTime.UtcNow - metadata!.ManagedFields?[0]?.Time)?.Hours.ToString();

                grid.AddRow(metadata!.NamespaceProperty, metadata.Name, "1/1", status.Phase,
                    status.ContainerStatuses[0].RestartCount.ToString(),
                    $"{(age != null ? $"{age}h" : "-")}");
            }

            var output = new Panel(grid)
                .Header("---K8s Cluster, Pods Info---", Justify.Center)
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