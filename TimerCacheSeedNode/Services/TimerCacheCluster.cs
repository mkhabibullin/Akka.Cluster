using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using TimerCacheSeedNode.Actors;

namespace TimerCacheSeedNode.Services
{
    internal class TimerCacheCluster
    {
        private Cluster _cluster;

        private ActorSystem _nodeOneActorSystem;

        private IActorRef _nodeRef;
        private IActorRef _nodeRefNode;

        private bool isStarted;

        public void Start()
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
                if(!string.IsNullOrEmpty(seedNodes))
                {
                    seedNodes += ",";
                }
                seedNodes += $"\"akka.tcp://ClusterSystem@{seedNode}\"";
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
                        roles = [seed]
                        auto-down-unreachable-after = off
                        allow-weakly-up-members = on
					}}
				}}
            ");

            _nodeOneActorSystem = ActorSystem.Create("ClusterSystem", config);
            _cluster = Cluster.Get(_nodeOneActorSystem);

            _nodeOneActorSystem.ActorOf(Props.Create<DestinationActor>(), "seed");
            _nodeOneActorSystem.ActorOf(Props.Create<DeadLetterActor>());
            _nodeOneActorSystem.ActorOf(Props.Create<SimpleClusterListenerActor>());

            isStarted = true;
        }

        public void Stop()
        {
            _cluster?.Leave(_cluster.SelfAddress);
        }

        public void SendToAll(string message)
        {
            if (_nodeRef == null)
            {
                _nodeRef = _nodeOneActorSystem.ActorOf(Props.Create<NodesAskActor>(), "ClusterNodeAskActor");
            }

            _nodeRef.Tell(message);
        }
        
        public void OnNodeAdded(IList<UniqueAddress> nodes)
        {
            Console.WriteLine($"-------------------------------------------------");
            Console.WriteLine($"Joined");
            if (_nodeRefNode == null)
            {
                _nodeRefNode = _nodeOneActorSystem.ActorOf(Props.Create<NodesAskActor>(), "ClusterNodeAskActor2");
            }

            _nodeRefNode.Tell("nodes");
        }
    }
}
