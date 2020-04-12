using Node;
using NodeService.Services;
using Shared;
using Shared.ConfigBuilder;
using Shared.Enums;
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
            var cache = new AssetsCache();
            clusterEvents.Subscribe((m) =>
            {
                Console.WriteLine($"1\t{m.Type.ToString()}\t{m.NodeId}");
                if (m.Type == ClusterEventType.Up)
                {
                    cache.SetNodeId(m.NodeId);
                }
                else if (m.Type == ClusterEventType.MemberUp)
                {
                    cache.AddNode(m.NodeId);
                }
                else if (m.Type == ClusterEventType.MemberDown)
                {
                    cache.RemoveNode(m.NodeId);
                }
                else if (m.Type == ClusterEventType.SeedDown)
                {
                    cache.Stop();
                }
            });
            var cluster = new NodeCluster(clusterEvents, configBuilder.Build(Roles.Node, 6001, seeds));

            var clusterEvents2 = new Subject<Shared.Messages.ClusterEvent>();
            var cache2 = new AssetsCache();
            clusterEvents2.Subscribe((m) =>
            {
                Console.WriteLine($"2\t{m.Type.ToString()}\t{m.NodeId}");
                if (m.Type == ClusterEventType.Up)
                {
                    cache2.SetNodeId(m.NodeId);
                }
                else if (m.Type == ClusterEventType.MemberUp)
                {
                    cache2.AddNode(m.NodeId);
                }
                else if (m.Type == ClusterEventType.MemberDown)
                {
                    cache2.RemoveNode(m.NodeId);
                }
                else if (m.Type == ClusterEventType.SeedDown)
                {
                    cache2.Stop();
                }
            });
            var cluster2 = new NodeCluster(clusterEvents2, configBuilder.Build(Roles.Node, 6002, seeds));

            cluster.Start();
            var firstStarted = true;
            cluster2.Start();
            var secondStarted = true;

            Console.WriteLine("Press 0 to exit");
            Console.WriteLine("Press 1 to start\\stop first node");
            Console.WriteLine("Press 2 to start\\stop first node");
            Console.WriteLine("Press a to print first cache");
            Console.WriteLine("Press b to print second cache");

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
                            cache.Stop();
                        }
                        else
                        {
                            cache.Stop();
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
                            cache2.Stop();
                        }
                        else
                        {
                            cache2.Stop();
                            cluster2.Start();
                            secondStarted = true;
                            Console.WriteLine("\tSTART");
                        }
                        break;
                    case '0':
                        exit = true;
                        break;
                    case 'a':
                        cache.Print();
                        break;
                    case 'b':
                        cache2.Print();
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
