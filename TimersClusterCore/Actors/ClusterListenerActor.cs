using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using System;
using System.Linq;
using TimersCluster.Enums;
using TimersCluster.Messages;

namespace TimersCluster.Actors
{
    internal class ClusterListenerActor : UntypedActor
    {
        protected ILoggingAdapter Log = Context.GetLogger();

        protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        protected readonly IObserver<ClusterEventMessage> _observer;

        public ClusterListenerActor(IObserver<ClusterEventMessage> observer)
        {
            _observer = observer;
        }

        /// <summary>
        /// Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            // subscribe to IMemberEvent and UnreachableMember events
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,
                new[] { typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember) });
        }

        /// <summary>
        /// Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            var up = message as ClusterEvent.MemberUp;
            if (up != null)
            {
                var mem = up;
                Log.Info("Member is Up: {0}", mem.Member);
                if (mem.Member.HasRole(ClusterRole.Node.ToString()))
                {
                    _observer?.OnNext(new ClusterEventMessage(mem.Member.UniqueAddress.Uid, ClusterEventType.MemberUp));
                }
                else
                {
                    Cluster.SendCurrentClusterState(Self);
                }
            }
            else if (message is ClusterEvent.CurrentClusterState)
            {
                var clusterState = (ClusterEvent.CurrentClusterState)message;

                var seedNodes = clusterState.Members
                    .Where(m => m.Roles.Contains(ClusterRole.Seed.ToString()) && m.Status == MemberStatus.Up)
                    .ToList();
                var unreachableNodes = clusterState.Unreachable
                    .Where(m => m.Roles.Contains(ClusterRole.Seed.ToString()))
                    .ToList();

                var activeSeeds = seedNodes.Select(s => s.UniqueAddress.Uid)
                    .Except(unreachableNodes.Select(s => s.UniqueAddress.Uid))
                    .ToList();

                if (activeSeeds.Count == 0)
                {
                    Cluster.Unsubscribe(Self);

                    _observer?.OnNext(new ClusterEventMessage(ClusterEventType.SeedDown));
                }
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember)message;
                Log.Info("Member detected as unreachable: {0}", unreachable.Member);
                if (unreachable.Member.HasRole(ClusterRole.Node.ToString()))
                {
                    _observer?.OnNext(new ClusterEventMessage(unreachable.Member.UniqueAddress.Uid, ClusterEventType.MemberDown));
                }
                else if (unreachable.Member.HasRole(ClusterRole.Seed.ToString()))
                {
                    Cluster.SendCurrentClusterState(Self);
                }
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved)message;
                Log.Info("Member is Removed: {0}", removed.Member);
                if (removed.Member.HasRole(ClusterRole.Node.ToString()))
                {
                    _observer?.OnNext(new ClusterEventMessage(removed.Member.UniqueAddress.Uid, ClusterEventType.MemberDown));
                }
                else if (removed.Member.HasRole(ClusterRole.Seed.ToString()))
                {
                    Cluster.SendCurrentClusterState(Self);
                }
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE
                var m = (ClusterEvent.IMemberEvent)message;
                if (Cluster.SelfMember.UniqueAddress.Uid == m.Member.UniqueAddress.Uid)
                {
                    _observer?.OnNext(new ClusterEventMessage(m.Member.UniqueAddress.Uid, ClusterEventType.Up));
                }
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}
