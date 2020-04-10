using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Event;

namespace WebNode.Services
{
    public interface IClusterBuilder
    {
        ActorSystem GetClusterNode();
        void StartClusterNode();
        void StopClusterNode();
    }

    public class ClusterBuilder : IClusterBuilder
    {
        private ActorSystem clusterActorSystem;

        public ActorSystem GetClusterNode() => clusterActorSystem;

        public void StartClusterNode()
        {
            var config = ConfigurationFactory.ParseString(@"
				akka {
					actor {
						provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
						serializers {
                            akka-pubsub = ""Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools""
                        }
                        serialization-bindings {
                            ""Akka.Cluster.Tools.PublishSubscribe.IDistributedPubSubMessage, Akka.Cluster.Tools"" = akka-pubsub
                            ""Akka.Cluster.Tools.PublishSubscribe.Internal.SendToOneSubscriber, Akka.Cluster.Tools"" = akka-pubsub
                        }
                        serialization-identifiers {
                            ""Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools"" = 9
                        }
                    }
					remote {
						log-remote-lifecycle-events = on
						helios.tcp {
							transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                            applied-adapters = []
                            transport-protocol = tcp
                            hostname = ""0.0.0.0""
                            port = 0
                        }
					}
					cluster {
						seed-nodes = [
                            ""akka.tcp://ClusterSystem@0.0.0.0:7002""
                        ]
                        roles = [client-web-node]
					}
				}
            ");

            clusterActorSystem = ActorSystem.Create("ClusterSystem", config);

            clusterActorSystem.ActorOf(Props.Create<SimpleClusterListener>());
        }

        public void StopClusterNode()
        {
            var cluster = Cluster.Get(clusterActorSystem);
            cluster.RegisterOnMemberRemoved(MemberRemoved);
            cluster.Leave(cluster.SelfAddress);
        }

        private async void MemberRemoved()
        {
            await clusterActorSystem.Terminate();
        }
    }


    public class SimpleClusterListener : UntypedActor
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
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember)message;
                Log.Info("Member detected as unreachable: {0}", unreachable.Member);
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved)message;
                Log.Info("Member is Removed: {0}", removed.Member);
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}
