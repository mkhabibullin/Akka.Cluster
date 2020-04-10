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
                    Cluster.SendCurrentClusterState(Self);
                }
            }
            else if (message is ClusterEvent.CurrentClusterState)
            {
                var clusterState = (ClusterEvent.CurrentClusterState)message;

                var seedNodes = clusterState.Members
                    .Where(m => m.Roles.Contains("seed") && m.Status == MemberStatus.Up)
                    .ToList();
                var unreachableNodes = clusterState.Unreachable
                    .Where(m => m.Roles.Contains("seed"))
                    .ToList();

                var activeSeeds = seedNodes.Select(s => s.UniqueAddress.Uid)
                    .Except(unreachableNodes.Select(s => s.UniqueAddress.Uid))
                    .ToList();

                if (activeSeeds.Count == 0)
                {
                    Cluster.Unsubscribe(Self);
                    //this.Self.GracefulStop(TimeSpan.FromSeconds(5)).Wait();
                    AssetsCache.Instance.Stop();
                    //this.Self.GracefulStop(TimeSpan.FromMinutes(1));
                    //this.Cluster.System.Terminate().Wait();
                    ClusterActorSystemService.OnRestar?.Invoke();
                    //Cluster.SendCurrentClusterState(Self);
                }
                //else if (AssetsCache.Instance.IsStopped)
                //{
                //    var nodes = clusterState.Members
                //    .Where(m => m.Roles.Contains("node") && m.Status == MemberStatus.Up)
                //    .ToList();

                //    foreach(var n in nodes)
                //    {
                //        if (n.UniqueAddress.Uid != AssetsCache.Instance.CurrentNodeIndex)
                //        {
                //            AssetsCache.Instance.AddNode(n.UniqueAddress.Uid);
                //        }
                //    }
                //}
            }
            //else if (message is ClusterEvent.MemberWeaklyUp)
            //{
            //    var weaklyUpped = (ClusterEvent.MemberWeaklyUp)message;
            //}
            //else if (message is ClusterEvent.LeaderChanged)
            //{
            //    var leader = (ClusterEvent.LeaderChanged)message;
            //    Console.WriteLine($"Leader: {leader.Leader.Host}");
            //}
            //else if (message is ClusterEvent.MemberJoined)
            //{
            //    var joined = (ClusterEvent.MemberJoined)message;
            //}
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
                    Cluster.SendCurrentClusterState(Self);
                    //AssetsCache.Instance.RemoveNode(Cluster.SelfMember.UniqueAddress.Uid);
                }
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
