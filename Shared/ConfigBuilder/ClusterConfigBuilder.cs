using Akka.Configuration;
using System;
using System.Configuration;

namespace Shared.ConfigBuilder
{
    internal sealed class ClusterConfigBuilder : IClusterConfigBuilder
    {
        public Config Build(string roles, string portConfigName = "TimerCacheNodePort", string seedConfigName = "TimerCacheSeedNodes")
        {
            var nodePortRaw = ConfigurationManager.AppSettings[portConfigName];
            int nodePort;

            if (string.IsNullOrWhiteSpace(nodePortRaw) || !int.TryParse(nodePortRaw, out nodePort))
            {
                return null;
            }

            var seed = ConfigurationManager.AppSettings[seedConfigName];
            if (string.IsNullOrWhiteSpace(seed))
            {
                return null;
            }

            return Build(roles, nodePort, seed);
        }

        public Config Build(string roles, int port, string seed)
        {
            var seedNodes = "";
            foreach (var seedNode in seed.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(seedNodes))
                {
                    seedNodes += ",";
                }
                seedNodes += $"\"akka.tcp://ClusterSystem@{seedNode}\"";
            }

            var config = ConfigurationFactory.ParseString($@"
				akka {{ 
                    loglevel = ERROR
                    log-dead-letters = 0    
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
                            port = {port.ToString()}
                        }}
					}}
					cluster {{
						seed-nodes = [
                            {seedNodes}
                        ]
                        roles = [{roles}]
					}}
				}}
            ");

            return config;
        }
    }
}
