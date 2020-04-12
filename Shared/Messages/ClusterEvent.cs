using Shared.Enums;

namespace Shared.Messages
{
    internal class ClusterEvent
    {
        public int NodeId { get; set; }

        public ClusterEventType Type { get; set; }

        public ClusterEvent(int nodeId, ClusterEventType type)
        {
            NodeId = nodeId;
            Type = type;
        }

        public ClusterEvent(ClusterEventType type)
            : this(0, type)
        {

        }
    }
}
