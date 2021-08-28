using System.Linq;
using Spectre.Console;

namespace KubernetesStats.Extension
{
    public static class GridExtension
    {
        public static Grid GenerateColumns(this Grid grid, int columnCount)
        {
            var columns = Enumerable.Range(0, columnCount)
                .Select(c => new GridColumn().LeftAligned())
                .ToArray();

            grid.AddColumns(columns);

            return grid;
        }
    }
}