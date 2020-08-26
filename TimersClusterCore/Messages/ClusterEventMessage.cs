using TimersCluster.Enums;

namespace TimersCluster.Messages
{
    internal class ClusterEventMessage
    {
        public int NodeId { get; set; }

        public ClusterEventType Type { get; set; }

        public ClusterEventMessage(int nodeId, ClusterEventType type)
        {
            NodeId = nodeId;
            Type = type;
        }

        public ClusterEventMessage(ClusterEventType type)
            : this(0, type)
        {
        }
    }
}
