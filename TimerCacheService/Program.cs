using System;
using System.Timers;
using TimerCacheService.Services;

namespace TimerCacheService
{
    class Program
    {
        static void Main(string[] args)
        {
            var cluster = new TimerCacheCluster();

            cluster.Start();

            var timer = new Timer(2000);
            timer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                cluster.SendToAll($"Test child msg from {cluster.Id.ToString()}");
            };
            timer.Start();

            Console.WriteLine("Any key to exit");
            Console.Read();

            cluster.Stop();
        }
    }
}
