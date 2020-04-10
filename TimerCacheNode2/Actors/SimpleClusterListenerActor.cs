using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using System;
using System.Linq;
using TimerCacheNode.Services;

namespace TimerCacheNode.Actors
{
    internal class SimpleClusterListenerActor : UntypedActor
    {
        protected ILoggingAdapter Log = Context.GetLogger();
        protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

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
                if (mem.Member.HasRole("node"))
                {
                    AssetsCache.Instance.AddNode(mem.Member.UniqueAddress.Uid);
                }
                else
                {
                }
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember)message;
                Log.Info("Member detected as unreachable: {0}", unreachable.Member);
                if (unreachable.Member.HasRole("node"))
                {
                    Console.WriteLine($"Unreachable: {unreachable.Member.UniqueAddress.Uid}");
                    AssetsCache.Instance.RemoveNode(unreachable.Member.UniqueAddress.Uid);
                }
                else if (unreachable.Member.HasRole("seed"))
                {
                    AssetsCache.Instance.RemoveSeedNode(unreachable.Member.UniqueAddress.Uid);
                    //AssetsCache.Instance.RemoveNode(Cluster.SelfMember.UniqueAddress.Uid);
                }
                var seedNodes = Cluster.State.Members
                    .Where(m => m.Roles.Contains("seed"))
                    .ToList();
                var states = seedNodes
                    .Select(s => s.Address + " - " + s.Status)
                    .ToList();

                Console.WriteLine($"State: {string.Join(",", states)}");
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved)message;
                Log.Info("Member is Removed: {0}", removed.Member);
                if (removed.Member.HasRole("node"))
                {
                    AssetsCache.Instance.RemoveNode(removed.Member.UniqueAddress.Uid);
                }
                else if (removed.Member.HasRole("seed"))
                {
                    //AssetsCache.Instance.RemoveNode(Cluster.SelfMember.UniqueAddress.Uid);
                }
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE
                var m = (ClusterEvent.IMemberEvent)message;
                if (Cluster.SelfMember.UniqueAddress.Uid == m.Member.UniqueAddress.Uid)
                {
                    Console.WriteLine($"My id: {m.Member.UniqueAddress.Uid}");
                    AssetsCache.Instance.SetNodeId(m.Member.UniqueAddress.Uid);
                }
                else if (m.Member.HasRole("seed"))
                {
                    AssetsCache.Instance.AddSeedNode(m.Member.UniqueAddress.Uid);
                    Console.WriteLine($"New seed: {m.Member.UniqueAddress.Uid}");
                }
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}
