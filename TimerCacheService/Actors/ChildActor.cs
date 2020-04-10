using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Event;

namespace TimerCacheService.Actors
{
    internal class ChildActor : ReceiveActor
    {
        protected ILoggingAdapter Log = Context.GetLogger();

        public ChildActor()
        {
            // activate the extension
            var mediator = DistributedPubSub.Get(Context.System).Mediator;

            // register to the path
            mediator.Tell(new Put(Self));

            Receive<string>(message => HandleMessage(message));
        }

        private void HandleMessage(string message)
            {
            Log.Info($"Child got msg: {message}");
            Sender.Tell($"Сообщение '{message}' успешно обработано в Child");
        }
    }
}
