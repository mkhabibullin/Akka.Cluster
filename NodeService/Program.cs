using Node;
using Shared;
using Shared.ConfigBuilder;
using System;
using System.Reactive.Subjects;

namespace NodeService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Node";

            var configBuilder = new ClusterConfigBuilder();
            var seeds = "0.0.0.0:7001,0.0.0.0:7002";

            var clusterEvents = new Subject<Shared.Messages.ClusterEvent>();
            clusterEvents.Subscribe((m) =>
            {
                Console.WriteLine($"1\t{m.Type.ToString()}\t{m.NodeId}");
            });
            var cluster = new NodeCluster(clusterEvents, configBuilder.Build(Roles.Node, 6001, seeds));

            var clusterEvents2 = new Subject<Shared.Messages.ClusterEvent>();
            clusterEvents2.Subscribe((m) =>
            {
                Console.WriteLine($"2\t{m.Type.ToString()}\t{m.NodeId}");
            });
            var cluster2 = new NodeCluster(clusterEvents2, configBuilder.Build(Roles.Node, 6002, seeds));

            cluster.Start();
            var firstStarted = true;
            cluster2.Start();
            var secondStarted = true;

            Console.WriteLine("Press 0 to exit");
            Console.WriteLine("Press 1 to start\\stop first node");
            Console.WriteLine("Press 2 to start\\stop first node");

            while (true)
            {
                var num = Console.ReadKey();
                var exit = false;
                switch (num.KeyChar)
                {
                    case '1':
                        if (firstStarted)
                        {
                            cluster.Stop();
                            firstStarted = false;
                            Console.WriteLine("\tEND");
                        }
                        else
                        {
                            cluster.Start();
                            firstStarted = true;
                            Console.WriteLine("\tSTART");
                        }
                        break;
                    case '2':
                        if (secondStarted)
                        {
                            cluster2.Stop();
                            secondStarted = false;
                            Console.WriteLine("\tEND");
                        }
                        else
                        {
                            cluster2.Start();
                            secondStarted = true;
                            Console.WriteLine("\tSTART");
                        }
                        break;
                    case '0':
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("\tNoop");
                        break;
                }

                if (exit) break;
            }

            cluster.Stop();
            cluster2.Stop();
        }
    }
}
