using Akka.Actor;
using System.Threading.Tasks;
using TimerCacheNode.Actors;

namespace TimerCacheNode.Services
{
    internal class ClusterActorSystemService
    {
        private readonly IActorRef askActor;

        public ClusterActorSystemService(TimerCacheCluster clusterBuilder)
        {
            var actorSystem = clusterBuilder.GetClusterNode();
            askActor = actorSystem.ActorOf(Props.Create<ClusterAskActor>(), "ClusterAskActor");
        }

        public Task<string> GetAnswerFromCluster(string message)
        {
            return askActor.Ask<string>(message);
        }
    }
}
