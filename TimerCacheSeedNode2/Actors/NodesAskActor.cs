using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace TimerCacheSeedNode.Actors
{
    internal class NodesAskActor : ReceiveActor
    {
        private readonly IActorRef mediator;

        public NodesAskActor()
        {
            // activate the extension
            mediator = DistributedPubSub.Get(Context.System).Mediator;
            Receive<string>(message => GetResponseFromClusterMembers(message));
        }

        private async void GetResponseFromClusterMembers(string message)
        {
            Sender.Tell(await mediator.Ask<string>(new Send("/user/child", message, localAffinity: true)));
        }
    }
}
