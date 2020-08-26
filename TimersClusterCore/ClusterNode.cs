using Akka.Actor;
using Akka.Configuration;
using System;
using System.Reactive.Subjects;
using TimersCluster.Actors;
using TimersCluster.Enums;
using TimersCluster.Messages;
using static TimersCluster.Consts;

namespace TimersCluster
{
    internal class ClusterNode
    {
        protected Akka.Cluster.Cluster _cluster;
        protected ActorSystem _actorSystem;
        protected readonly Config _config;
        private bool _isRestarting;
        public virtual string Name => ClusterNameDefault;
        public readonly ISubject<ClusterEventMessage> ClusterEvents;

        public ClusterNode(Config config)
        {
            if (config == null)
            {
                throw new Exception($"{nameof(ClusterNode)} failed to start: Config is not defined");
            }

            _config = config;

            ClusterEvents = new Subject<ClusterEventMessage>();
        }

        public virtual void Start()
        {
            _actorSystem = ActorSystem.Create(Name, _config);

            _cluster = Akka.Cluster.Cluster.Get(_actorSystem);

            BuildActorSystem();

            Console.WriteLine("Started");
        }

        public virtual void Stop()
        {
            _cluster?.RegisterOnMemberRemoved(MemberRemoved);

            _cluster?.Leave(_cluster.SelfAddress);
        }

        protected virtual void BuildActorSystem()
        {
            ClusterEvents?.Subscribe(ProcessClusterEvent);

            _actorSystem?.ActorOf(Props.Create<ClusterListenerActor>(ClusterEvents));
        }

        protected virtual async void MemberRemoved()
        {
            await _actorSystem.Terminate();

            Console.WriteLine("Finished");

            if (_isRestarting) Start();
            _isRestarting = false;
        }

        public void ProcessClusterEvent(ClusterEventMessage msgEvent)
        {
            if (msgEvent.Type == ClusterEventType.SeedDown)
            {
                Stop();
                _isRestarting = true;
            }
        }
    }
}
