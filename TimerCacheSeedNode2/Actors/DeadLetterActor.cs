using Akka.Event;

namespace TimerCacheSeedNode.Actors
{
    internal class DeadLetterActor : DeadLetterListener
    {
        protected override bool Receive(object message)
        {
            return base.Receive(message);
        }
    }
}
