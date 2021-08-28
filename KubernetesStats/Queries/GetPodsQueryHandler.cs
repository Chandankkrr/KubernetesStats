using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using KubernetesStats.Extension;
using KubernetesStats.Models;
using KubernetesStats.Models.Pod;
// using KubernetesStats.Models1;
using MediatR;
using Spectre.Console;

namespace KubernetesStats.Queries
{
    public class GetPodsQuery : K8, IRequest<Unit>
    {
    }

    public class GetPodsQueryHandler : IRequestHandler<GetPodsQuery, Unit>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GetPodsQueryHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Unit> Handle(GetPodsQuery request, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("K8sClient");
            var k8Namespace = request.Namespace;

            var response = await httpClient.GetAsync($"api/v1/namespaces/{k8Namespace}/pods", cancellationToken);
            var podInfo =
                await response.Content.ReadFromJsonAsync<PodInfo>(cancellationToken: cancellationToken);

            var grid = new Grid { Expand = false }
                .GenerateColumns(6)
                .AddRow("NAMESPACE", "NAME", "READY", "STATUS", "RESTARTS", "AGE");

            if (podInfo?.Items == null)
            {
                AnsiConsole.WriteLine("Server did not return a response");
                return Unit.Value;
            }

            foreach (var resultItem in podInfo.Items)
            {
                var metadata = resultItem.Metadata;
                var age = (DateTime.UtcNow - metadata.ManagedFields[0].Time).Hours;

                grid.AddRow(metadata.Namespace, metadata.Name, "1/1", resultItem.Status.Phase,
                    resultItem.Status.ContainerStatuses[0].RestartCount.ToString(),
                    $"{age}h");
            }

            var output = new Panel(grid)
                .Header(
                    "---K8s Cluster Pods, Info---",
                    Justify.Center
                )
                .BorderStyle(new Style(
                    Color.NavajoWhite1,
                    decoration: Decoration.Italic)
                )
                .BorderColor(Color.Orange3)
                .Padding(1, 1, 1, 1)
                .RoundedBorder();

            AnsiConsole.WriteLine();
            AnsiConsole.Render(output);

            return Unit.Value;
        }
    }
}