using Akka.Configuration;
using Shared;
using System.Reactive.Subjects;

namespace Node
{
    internal class NodeCluster : Cluster
    {
        public NodeCluster(ISubject<Shared.Messages.ClusterEvent> clusterEvents, Config config)
            : base(clusterEvents, config)
        {
        }
    }
}
