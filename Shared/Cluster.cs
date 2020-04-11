using Akka.Actor;
using Akka.Configuration;
using Shared.Actors;
using Shared.Enums;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static Shared.Consts;

namespace Shared
{
    internal abstract class Cluster
    {
        protected Akka.Cluster.Cluster _cluster;
        protected ActorSystem _actorSystem;
        protected readonly Config _config;
        protected readonly ISubject<Messages.ClusterEvent> _clusterEvents;
        private bool isRestarting;
        public virtual string Name => ClusterNameDefault;

        public Cluster(Config config)
        {
            if (config == null)
            {
                throw new Exception($"{nameof(Cluster)} failed to start: Config is not defined");
            }

            _config = config;
        }

        public Cluster(ISubject<Messages.ClusterEvent> clusterEvents, Config config)
            : this(config)
        {
            _clusterEvents = clusterEvents;
        }

        public virtual void Start()
        {
            _actorSystem = ActorSystem.Create(Name, _config);

            _cluster = Akka.Cluster.Cluster.Get(_actorSystem);

            BuildActorSystem();
        }

        public virtual void Stop()
        {
            _cluster?.RegisterOnMemberRemoved(MemberRemoved);

            _cluster?.Leave(_cluster.SelfAddress);
        }

        protected virtual void BuildActorSystem()
        {
            _clusterEvents?
                .Subscribe(ProcessClusterEvent);

            _actorSystem?.ActorOf(Props.Create<ClusterListenerActor>(_clusterEvents));
        }

        protected virtual async void MemberRemoved()
        {
            await _actorSystem.Terminate();

            if (isRestarting) Start();
            isRestarting = false;
        }

        public void ProcessClusterEvent(Messages.ClusterEvent @event)
        {
            if (@event.Type == ClusterEventType.SeedDown)
            {
                Stop();
                isRestarting = true;
            }
        }
    }
}
