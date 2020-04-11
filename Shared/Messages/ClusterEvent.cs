using Shared.Enums;

namespace Shared.Messages
{
    internal class ClusterEvent
    {
        public long NodeId { get; set; }

        public ClusterEventType Type { get; set; }

        public ClusterEvent(long nodeId, ClusterEventType type)
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
