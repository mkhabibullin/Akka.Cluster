using System;
using System.Timers;
using TimerCacheSeedNode.Services;

namespace TimerCacheSeedNode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Seed 1";

            var cluster = new TimerCacheCluster();
            NodeService.OnAdd = cluster.OnNodeAdded;

            cluster.Start();

            var timer = new Timer(2000);
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                //cluster.OnNodeAdded(null);
                //cluster.SendToAll("Test child msg");
            };
            timer.Start();

            Console.WriteLine("Any key to exit");
            Console.Read();

            cluster.Stop();
        }
    }
}
