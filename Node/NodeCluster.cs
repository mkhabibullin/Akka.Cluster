using Akka.Configuration;
using Shared;
using System.Reactive.Subjects;

namespace Node
{
    internal class NodeCluster : ClusterNode
    {
        public NodeCluster(ISubject<Shared.Messages.ClusterEvent> clusterEvents, Config config)
            : base(clusterEvents, config)
        {
        }
    }
}
