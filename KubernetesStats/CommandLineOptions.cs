using CommandLine;
using KubernetesStats.Models;

namespace KubernetesStats
{
    internal class CommandLineOptions
    {
        [Option("resource",
            Default = ResourceType.Pods,
            HelpText = "K8s resource type")]
        public ResourceType ResourceFlag { get; set; }

        [Option("namespace",
            Default = "default",
            HelpText = "K8s namespace")]
        public string NamespaceFlag { get; set; }
    }
}