using Akka.Actor;
using System;
using System.Threading.Tasks;
using TimerCacheNode.Actors;

namespace TimerCacheNode.Services
{
    internal class ClusterActorSystemService
    {
        private readonly TimerCacheCluster clusterBuilder;
        private IActorRef askActor;

        private ActorSystem actorSystem;

        public static Action OnRestar = null;

        public ClusterActorSystemService(TimerCacheCluster clusterBuilder)
        {
            this.clusterBuilder = clusterBuilder;

            OnRestar = () => Task.Run(Restart);

            Create();
        }

        public void Create()
        {
            clusterBuilder.StartClusterNode();

            actorSystem = clusterBuilder.GetClusterNode();
            //askActor = actorSystem.ActorOf(Props.Create<ClusterAskActor>(), "ClusterAskActor");
        }

        public Task<string> GetAnswerFromCluster(string message)
        {
            return askActor.Ask<string>(message);
        }

        public void Restart()
        {
            clusterBuilder.StopClusterNode();
            //actorSystem.Stop(askActor);
            ////clusterBuilder.StopClusterNode();
            //actorSystem.Terminate().Wait();
            ////actorSystem?.Terminate().ConfigureAwait(false).GetAwaiter().GetResult();
            actorSystem?.Dispose();

            Console.WriteLine("Restarted");

            Create();
        }
    }
}
