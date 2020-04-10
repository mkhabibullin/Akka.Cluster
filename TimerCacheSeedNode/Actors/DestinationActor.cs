using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;
using TimerCacheSeedNode.Services;

namespace TimerCacheSeedNode.Actors
{
    internal class DestinationActor : ReceiveActor
    {
        protected ILoggingAdapter Log = Context.GetLogger();

        public DestinationActor()
       {
            // activate the extension
            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            // register to the path
            mediator.Tell(new Put(Self));

            Receive<string>(message => HandleMessage(message));
        }

        private void HandleMessage(string message)
        {
            //NodeService.Nodes.Add(0);

            Log.Info($"Обработка сообщения '{message}'");
            Sender.Tell($"Сообщение '{message}' успешно обработано в кластере, узел ClusterSeedNodeTwo");
        }
    }
}
