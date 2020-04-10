using System;
using System.Timers;
using TimerCacheNode.Services;

namespace TimerCacheNode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Node 1";

            var cluster = new TimerCacheCluster();

            var system = new ClusterActorSystemService(cluster);

            //system.Restart();

            var timer = new Timer(2000);
            timer.Start();
            var count = 0;
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                //var response = system.GetAnswerFromCluster("Test request").Result;

                //Console.WriteLine($"Response is: {response}");

                Console.WriteLine($"My assests are: {string.Join(",", AssetsCache.Instance.Items)}");
                //count++;
                //if(count == 10)
                //{
                //    system.Restart();
                //}
            };

            //system.Restart();

            Console.WriteLine("Any key to exit");

            Console.Read();

            cluster.StopClusterNode();
        }
    }
}
