using System;
using System.Timers;
using TimerCacheNode.Services;

namespace TimerCacheNode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Node 2";

            var cluster = new TimerCacheCluster();
            cluster.StartClusterNode();

            var system = new ClusterActorSystemService(cluster);

            var timer = new Timer(2000);
            timer.Start();
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                //var response = system.GetAnswerFromCluster("Test request").Result;

                //Console.WriteLine($"Response is: {response}");

                Console.WriteLine($"My assests are: {string.Join(",", AssetsCache.Instance.Items)}");
            };

            Console.WriteLine("Any key to exit");

            Console.Read();

            cluster.StopClusterNode();
        }
    }
}
