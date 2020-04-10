using Akka.Cluster;
using System;
using System.Collections.Generic;

namespace TimerCacheSeedNode.Services
{
    internal class NodeService
    {
        public static IList<UniqueAddress> Nodes = new List<UniqueAddress>();

        public static Action<IList<UniqueAddress>> OnAdd = null;

        public static void Add(UniqueAddress id)
        {
            Nodes.Add(id);
            OnAdd?.Invoke(Nodes);
        }
    }
}
