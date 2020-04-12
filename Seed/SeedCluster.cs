using Akka.Actor;
using Akka.Configuration;
using Seed.Actors;
using Shared;

namespace Seed
{
    internal class SeedCluster : ClusterNode
    {
        public SeedCluster(Config config)
            : base(config)
        {
        }

        protected override void BuildActorSystem()
        {
            base.BuildActorSystem();

            _actorSystem.ActorOf(Props.Create<DestinationActor>(), "seed");
        }
    }
}
