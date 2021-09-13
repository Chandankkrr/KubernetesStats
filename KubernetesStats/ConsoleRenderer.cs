using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace KubernetesStats.Extension
{
    public static class ConsoleRenderer
    {
        public static void Render(string[] columnHeader, List<string[]> data, string resourceName)
        {
            var columns = Enumerable.Range(0, columnHeader.Length)
                .Select(_ => new GridColumn().LeftAligned())
                .ToArray();
                
            var grid = new Grid()
                .AddColumns(columns)
                .AddRow(columnHeader);
            
            foreach (var strings in data)
            {
                grid.AddRow(strings);
            }
            
            var output = new Panel(grid)
                .Header($"---K8s :spouting_whale: Cluster, {resourceName}---", Justify.Center)
                .BorderColor(Color.Green)
                .Padding(1, 1, 1, 1)
                .RoundedBorder();

            AnsiConsole.WriteLine();
            AnsiConsole.Render(output);
        }
    }
}