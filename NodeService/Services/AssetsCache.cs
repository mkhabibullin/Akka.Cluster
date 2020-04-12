using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeService.Services
{
    internal class AssetsCache
    {
        //public static AssetsCache Instance = new AssetsCache();

        public static List<int> GlobalItems;

        public List<int> Items = new List<int>();

        public List<int> RegisteredNodes = new List<int>();

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
            AddRegisteredNodes(id);
        }

        public void AddNode(int id)
        {
            AddRegisteredNodes(id);
            CurrentNodeIndex = RegisteredNodes.OrderBy(v => v).ToList().FindIndex(v => v == NodeId);

            ReLoad();
        }

        protected void AddRegisteredNodes(int id)
        {
            if (!RegisteredNodes.Contains(id))
            {
                RegisteredNodes.Add(id);
            }
        }

        public void RemoveNode(int id)
        {
            RegisteredNodes.Remove(id);

            CurrentNodeIndex = RegisteredNodes.OrderBy(v => v).ToList().FindIndex(v => v == NodeId);

            ReLoad();
        }

        public void Stop()
        {
            RegisteredNodes.Clear();
            ReLoad();
            IsStopped = true;
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

        public void Print()
        {
            var items = string.Join(",", Items);
            Console.WriteLine(items);
        }
    }
}
