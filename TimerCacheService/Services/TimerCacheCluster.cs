using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using System;
using System.Configuration;
using TimerCacheService.Actors;

namespace TimerCacheService.Services
{
    internal class TimerCacheCluster
    {
        public int Id { get; set; }

        private Cluster _cluster;

        private ActorSystem _nodeOneActorSystem;

        private IActorRef _nodeRef;

        public void Start()
        {
            var timerCacheNodePortSettingName = "TimerCacheNodePort";
            var nodePortRaw = ConfigurationManager.AppSettings[timerCacheNodePortSettingName];
            int nodePort;
            if (string.IsNullOrWhiteSpace(nodePortRaw) || !int.TryParse(nodePortRaw, out nodePort))
            {
                throw new Exception($"{nameof(TimerCacheCluster)} is not started: {timerCacheNodePortSettingName} is not defined or not correct");
            }

            Id = nodePort;

            var timerCacheSeedNodesSettingName = "TimerCacheSeedNodes";
            var seedNodesRaw = ConfigurationManager.AppSettings[timerCacheSeedNodesSettingName];
            if (string.IsNullOrWhiteSpace(seedNodesRaw))
            {
                throw new Exception($"{nameof(TimerCacheCluster)} is not started: {timerCacheSeedNodesSettingName} is not defined");
            }
                
            var seedNodes = "";
            foreach (var seedNode in seedNodesRaw.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                if(!string.IsNullOrEmpty(seedNodes))
                {
                    seedNodes += ",";
                }
                seedNodes += $"\"akka.tcp://ClusterSystem@{seedNode}\"";
            }

            var nodeRoleSettingName = "TimerCacheClusterRoles";
            var nodeRoles = ConfigurationManager.AppSettings[nodeRoleSettingName];
            if (string.IsNullOrWhiteSpace(nodeRoles))
            {
                throw new Exception($"{nameof(TimerCacheCluster)} is not started: {nodeRoleSettingName} is not defined");
            }

            var config = ConfigurationFactory.ParseString($@"
				akka {{
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
                        roles = [{nodeRoles}]
					}}
				}}
            ");

            _nodeOneActorSystem = ActorSystem.Create("ClusterSystem", config);
            _cluster = Cluster.Get(_nodeOneActorSystem);

            _nodeOneActorSystem.ActorOf(Props.Create<DestinationActor>(), "destination");
            _nodeOneActorSystem.ActorOf(Props.Create<DeadLetterActor>());
            _nodeOneActorSystem.ActorOf(Props.Create<SimpleClusterListenerActor>());
            _nodeOneActorSystem.ActorOf(Props.Create<ChildActor>(), "child1");
        }

        public void Stop()
        {
            _cluster?.Leave(_cluster.SelfAddress);
        }

        public void SendToAll(string message)
        {
            if (_nodeRef == null)
            {
                _nodeRef = _nodeOneActorSystem.ActorOf(Props.Create<ClusterAskActor>(), "ClusterNodeAskActor");
            }

            _nodeRef.Tell(message);
        }
    }
}
