using Seed;
using Shared;
using Shared.ConfigBuilder;
using System;

namespace SeedService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Seed";

            var configBuilder = new ClusterConfigBuilder();
            var seeds = "0.0.0.0:7001,0.0.0.0:7002";

            var cluster = new SeedCluster(configBuilder.Build(Roles.Seed, 7001, seeds));
            var cluster2 = new SeedCluster(configBuilder.Build(Roles.Seed, 7002, seeds));

            cluster.Start();
            var firstStarted = true;
            cluster2.Start();
            var secondStarted = true;

            Console.WriteLine("Press 0 to exit");
            Console.WriteLine("Press 1 to start\\stop first seed node");
            Console.WriteLine("Press 2 to start\\stop first seed node");

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
