using System;
using System.Collections.Generic;
using System.Linq;

namespace Node.Services
{
    internal class AssetsCache
    {
        public static AssetsCache Instance = new AssetsCache();

        public static List<int> GlobalItems;

        public List<int> Items = new List<int>();

        public List<int> RegisteredNodes = new List<int>();

        public List<int> RegisteredSeeds = new List<int>();

        public int NodeId { get; private set; }

        public int CurrentNodeIndex { get; private set; }
        public bool IsStopped { get; private set; }

        static AssetsCache()
        {
            GlobalItems = new List<int>();
            for (var i = 1; i <= 10; i++)
            {
                GlobalItems.Add(i);
            }
        }

        public void SetNodeId(int id)
        {
            NodeId = id;
        }

        public void AddNode(int id)
        {
            RegisteredNodes.Add(id);
            Console.WriteLine($"Registered nodes = {string.Join(",", RegisteredNodes)}");

            CurrentNodeIndex = RegisteredNodes.OrderBy(v => v).ToList().FindIndex(v => v == NodeId);

            ReLoad();
        }

        public void RemoveNode(int id)
        {
            RegisteredNodes.Remove(id);

            CurrentNodeIndex = RegisteredNodes.OrderBy(v => v).ToList().FindIndex(v => v == NodeId);

            Console.WriteLine($"The left nodes = {string.Join(",", RegisteredNodes)}");

            ReLoad();
        }

        public void Stop()
        {
            RegisteredNodes.Clear();
            ReLoad();
            IsStopped = true;
        }

        public void AddSeedNode(int id)
        {
            RegisteredSeeds.Add(id);

            Console.WriteLine($"Registered seeds = {string.Join(",", RegisteredNodes)}");
        }

        public void RemoveSeedNode(int id)
        {
            RegisteredSeeds.Remove(id);

            Console.WriteLine($"The left seeds = {string.Join(",", RegisteredNodes)}");
        }

        public void ReLoad()
        {
            Items = new List<int>();

            foreach (var i in GlobalItems)
            {
                if (RegisteredNodes.Count > 0)
                {
                    if (i % RegisteredNodes.Count == CurrentNodeIndex)
                    {
                        Items.Add(i);
                    }
                }
            }
        }
    }
}
