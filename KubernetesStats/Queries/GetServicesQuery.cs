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

            var data = serviceItems.Select(p => new { p.Metadata, p.Spec })
                .Select(d => new[]
                {
                    d.Metadata.NamespaceProperty,
                    d.Metadata.Name,
                    d.Spec.Type,
                    d.Spec.ClusterIP,
                    string.Join(",", d.Spec.ExternalIPs?.Select(e => e) ?? new []{"<none>"}),
                    string.Join(",", d.Spec.Ports.Select(p => $"{p.TargetPort.Value}/{p.Protocol}")),
                    $"{(DateTime.UtcNow - d.Metadata.ManagedFields?[0]?.Time)?.Hours.ToString() ?? "0"}h"
                }).ToList();

            var columnHeaders = new[] { "NAMESPACE", "NAME", "TYPE", "CLUSTER-IP", "EXTERNAL-IP", "PORT(S)", "AGE" };
            ConsoleRenderer.Render(columnHeaders, data, "Services Info");

            return Unit.Value;
        }
    }
}