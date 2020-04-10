using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Event;

namespace ClusterSeedNodeTwo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ClusterSeedNodeTwo";

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
                            port = 7002
                        }
					}
					cluster {
						seed-nodes = [
                            ""akka.tcp://ClusterSystem@0.0.0.0:7002""
                        ]
                        roles = [seed-node-2]
					}
				}
            ");

            var nodeOneActorSystem = ActorSystem.Create("ClusterSystem", config);
            var cluster = Cluster.Get(nodeOneActorSystem);
            
            Console.WriteLine("NodeOne: Actor system created");
            nodeOneActorSystem.ActorOf(Props.Create<DestinationActor>(), "destination");
            nodeOneActorSystem.ActorOf(Props.Create<Dead>());
            nodeOneActorSystem.ActorOf(Props.Create<SimpleClusterListener>());            

            Console.ReadLine();

            cluster.Leave(cluster.SelfAddress);
        }
    }

    public class Dead : DeadLetterListener
    {
        protected override bool Receive(object message)
        {
            return base.Receive(message);
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
