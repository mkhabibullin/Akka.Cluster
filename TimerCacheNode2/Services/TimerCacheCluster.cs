using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using System;
using System.Configuration;
using TimerCacheNode.Actors;

namespace TimerCacheNode.Services
{
    internal class TimerCacheCluster   
    {
        private ActorSystem clusterActorSystem;

        public ActorSystem GetClusterNode() => clusterActorSystem;

        public void StartClusterNode()
        {
            var timerCacheNodePortSettingName = "TimerCacheNodePort";
            var nodePortRaw = ConfigurationManager.AppSettings[timerCacheNodePortSettingName];
            int nodePort;
            if (string.IsNullOrWhiteSpace(nodePortRaw) || !int.TryParse(nodePortRaw, out nodePort))
            {
                throw new Exception($"{nameof(TimerCacheCluster)} is not started: {timerCacheNodePortSettingName} is not defined or not correct");
            }

            var timerCacheSeedNodesSettingName = "TimerCacheSeedNodes";
            var seedNodesRaw = ConfigurationManager.AppSettings[timerCacheSeedNodesSettingName];
            if (string.IsNullOrWhiteSpace(seedNodesRaw))
            {
                throw new Exception($"{nameof(TimerCacheCluster)} is not started: {timerCacheSeedNodesSettingName} is not defined");
            }

            var seedNodes = "";
            foreach (var seedNode in seedNodesRaw.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(seedNodes))
                {
                    seedNodes += ",";
                }
                seedNodes += $"\"akka.tcp://ClusterSystem@{seedNode}\"";
            }

            var config = ConfigurationFactory.ParseString($@"
				akka {{
                    loglevel = INFO
					actor {{
						provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
						serializers {{
                            akka-pubsub = ""Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools""
                        }}
                        serialization-bindings {{
                            ""Akka.Cluster.Tools.PublishSubscribe.IDistributedPubSubMessage, Akka.Cluster.Tools"" = akka-pubsub
                            ""Akka.Cluster.Tools.PublishSubscribe.Internal.SendToOneSubscriber, Akka.Cluster.Tools"" = akka-pubsub
                        }}
                        serialization-identifiers {{
                            ""Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools"" = 9
                        }}
                    }}
					remote {{
						log-remote-lifecycle-events = on
						helios.tcp {{
							transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
                            applied-adapters = []
                            transport-protocol = tcp
                            hostname = ""0.0.0.0""
                            port = {nodePort}
                        }}
					}}
					cluster {{
						seed-nodes = [
                            {seedNodes}
                        ]
                        roles = [node]
                        auto-down-unreachable-after = off
                        allow-weakly-up-members = on
					}}
				}}
            ");

            clusterActorSystem = ActorSystem.Create("ClusterSystem", config);

            clusterActorSystem.ActorOf(Props.Create<SimpleClusterListenerActor>());
            clusterActorSystem.ActorOf(Props.Create<ChildActor>(), "child");
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
}
