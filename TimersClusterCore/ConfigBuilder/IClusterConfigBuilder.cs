using Akka.Configuration;

namespace TimersCluster.ConfigBuilder
{
    internal interface IClusterConfigBuilder
    {
        Config Build(string roles, int port, string seed);

        Config Build(string roles, string portConfigName, string seedConfigName);
    }
}
