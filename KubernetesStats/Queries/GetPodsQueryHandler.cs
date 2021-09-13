using System;
using System.Collections.Generic;
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

            var pods = await client.ListNamespacedPodAsync(
                k8Namespace,
                cancellationToken: cancellationToken
            );
            var podItems = pods.Items;

            if (!podItems.Any())
            {
                AnsiConsole.WriteLine($"No pods found in {k8Namespace} namespace");
                return Unit.Value;
            }

            var data = podItems.Select(p => new { p.Metadata, p.Spec, p.Status })
                .Select(d => new[]
                {
                    $"[dodgerblue1]{d.Metadata.NamespaceProperty}[/]",
                    $"[aquamarine1]{d.Metadata.Name}[/]",
                    $"[yellow3_1]1/1[/]",
                    $"[deeppink2]{d.Status.Phase}[/]",
                    $"[orangered1]{d.Status.ContainerStatuses[0].RestartCount.ToString()}[/]",
                    $"[teal]{(DateTime.UtcNow - d.Metadata!.ManagedFields?[0]?.Time)?.Hours.ToString() ?? "0"}h[/]"
                }).ToList();

            var columnHeaders = new[] { "NAMESPACE", "NAME", "READY", "STATUS", "RESTARTS", "AGE" };
            ConsoleRenderer.Render(columnHeaders, data, "Pods Info");

            return Unit.Value;
        }
    }
}