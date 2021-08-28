using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KubernetesStats.Extension;
using KubernetesStats.Models;
using KubernetesStats.Models.Service;
using MediatR;
using Spectre.Console;

namespace KubernetesStats.Queries
{
    public class GetServicesQuery : K8, IRequest<Unit>
    {
    }

    public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, Unit>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GetServicesQueryHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Unit> Handle(GetServicesQuery request, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("K8sClient");
            var k8Namespace = request.Namespace;

            var response =
                await httpClient.GetAsync($"api/v1/namespaces/{k8Namespace}/services", cancellationToken);
            var serverInfo =
                await response.Content.ReadFromJsonAsync<ServiceInfo>(cancellationToken: cancellationToken);

            var grid = new Grid { Expand = false }
                .GenerateColumns(7)
                .AddRow("NAMESPACE", "NAME", "TYPE", "CLUSTER-IP", "EXTERNAL-IP", "PORT(S)", "AGE");

            if (serverInfo?.Items == null)
            {
                AnsiConsole.WriteLine("Server did not return a response");
                return Unit.Value;
            }

            foreach (var resultItem in serverInfo.Items)
            {
                var metadata = resultItem.Metadata;
                var spec = resultItem.Spec;
                var age = (DateTime.UtcNow - metadata.ManagedFields[0].Time).Hours.ToString();

                var ports = new StringBuilder();
                foreach (var port in spec.Ports)
                {
                    // TODO use PortPort
                    ports.Append($"{port.TargetPort.ToString()}/{port.Protocol},");
                }

                grid.AddRow(metadata.Namespace, metadata.Name, spec.Type, spec.ClusterIp, "<none>",
                    string.Join(",", spec.Ports.Select(p => $"{p.TargetPort.ToString()}/{p.Protocol}")),
                    $"{age}h");
            }

            var output = new Panel(grid)
                .Header("---K8s Cluster, Service Info---", Justify.Center)
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