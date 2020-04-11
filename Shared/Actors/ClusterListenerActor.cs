using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Shared.Enums;
using System;
using System.Linq;

namespace Shared.Actors
{
    internal class ClusterListenerActor : UntypedActor
    {
        protected ILoggingAdapter Log = Context.GetLogger();

        protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        protected readonly IObserver<Messages.ClusterEvent> _observer;

        private long CurrentId;

        public ClusterListenerActor(IObserver<Messages.ClusterEvent> observer)
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
                if (mem.Member.HasRole(Roles.Node))
                {
                    if (CurrentId != mem.Member.UniqueAddress.Uid)
                    {
                        _observer?.OnNext(new Messages.ClusterEvent(mem.Member.UniqueAddress.Uid, ClusterEventType.MemberUp));
                        //AssetsCache.Instance.AddNode(mem.Member.UniqueAddress.Uid);
                    }
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
                    .Where(m => m.Roles.Contains(Roles.Seed) && m.Status == MemberStatus.Up)
                    .ToList();
                var unreachableNodes = clusterState.Unreachable
                    .Where(m => m.Roles.Contains(Roles.Seed))
                    .ToList();

                var activeSeeds = seedNodes.Select(s => s.UniqueAddress.Uid)
                    .Except(unreachableNodes.Select(s => s.UniqueAddress.Uid))
                    .ToList();

                if (activeSeeds.Count == 0)
                {
                    Cluster.Unsubscribe(Self);

                    _observer?.OnNext(new Messages.ClusterEvent(ClusterEventType.SeedDown));
                    //AssetsCache.Instance.Stop();
                    //ClusterActorSystemService.OnRestar?.Invoke();
                }
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember)message;
                Log.Info("Member detected as unreachable: {0}", unreachable.Member);
                if (unreachable.Member.HasRole(Roles.Node))
                {
                    Console.WriteLine($"Unreachable: {unreachable.Member.UniqueAddress.Uid}");

                    _observer?.OnNext(new Messages.ClusterEvent(unreachable.Member.UniqueAddress.Uid, ClusterEventType.MemberDown));
                    //AssetsCache.Instance.RemoveNode(unreachable.Member.UniqueAddress.Uid);
                }
                else if (unreachable.Member.HasRole(Roles.Seed))
                {
                    Cluster.SendCurrentClusterState(Self);
                }
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved)message;
                Log.Info("Member is Removed: {0}", removed.Member);
                if (removed.Member.HasRole(Roles.Node))
                {
                    _observer?.OnNext(new Messages.ClusterEvent(removed.Member.UniqueAddress.Uid, ClusterEventType.MemberDown));
                    //AssetsCache.Instance.RemoveNode(removed.Member.UniqueAddress.Uid);
                }
                else if (removed.Member.HasRole(Roles.Seed))
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
                    CurrentId = m.Member.UniqueAddress.Uid;
                    _observer?.OnNext(new Messages.ClusterEvent(CurrentId, ClusterEventType.Up));
                    //AssetsCache.Instance.SetNodeId(m.Member.UniqueAddress.Uid);
                }
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}
