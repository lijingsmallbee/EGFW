using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Collections.Generic
{
    public static class LinkedListExtensions
    {
        public static bool Contains<T>(this LinkedList<T> list, T item) where T : IEquatable<T>
        {
            foreach (var i in list)
            {
                if (i.Equals(item))
                    return true;
            }
            return false;
        }

        public static bool Exists<T>(this LinkedList<T> list, Predicate<T> match) => list.FindNode(match) != null;

        public static T Find<T>(this LinkedList<T> list, Predicate<T> match)
        {
            var node = list.FindNode(match);
            return node != null ? node.Value : default;
        }

        public static T FindLast<T>(this LinkedList<T> list, Predicate<T> match)
        {
            var node = list.FindLastNode(match);
            return node != null ? node.Value : default;
        }

        public static LinkedListNode<T> FindNode<T>(this LinkedList<T> list, Predicate<T> match)
        {
            for (var cur = list.First; cur != null; cur = cur.Next)
            {
                if (match(cur.Value))
                    return cur;
            }
            return null;
        }

        public static LinkedListNode<T> FindLastNode<T>(this LinkedList<T> list, Predicate<T> match)
        {
            for (var cur = list.Last; cur != null; cur = cur.Previous)
            {
                if (match(cur.Value))
                    return cur;
            }
            return null;
        }

        public static void InsertionSort<T>(this LinkedList<T> list) where T : IComparable<T> => list.InsertionSort(Comparer<T>.instance);
        public static void InsertionSort<T>(this LinkedList<T> list, IComparer<T> comparer)
        {
            var cur = list.First;
            while (cur != null)
            {
                var node = cur;
                cur = cur.Next;

                for (var iter = list.First; iter != node; iter = iter.Next)
                {
                    if (comparer.Compare(iter.Value, node.Value) > 0)
                    {
                        list.Remove(node);
                        list.AddBefore(iter, node);
                        break;
                    }
                }
            }
        }

        public static bool TrueForAll<T>(this LinkedList<T> list, Predicate<T> match)
        {
            foreach (var i in list)
            {
                if (!match(i))
                    return false;
            }
            return true;
        }

        public static LinkedListNode<T> Mid<T>(this LinkedList<T> list)
        {
            if (list.Count == 0)
                return null;
            var mid = list.Count / 2;
            var cur = list.First;
            for (var i = 0; i < mid; ++i)
                cur = cur.Next;
            return cur;
        }

        class Comparer<T> : IComparer<T> where T : IComparable<T>
        {
            public static Comparer<T> instance = new Comparer<T>();

            int IComparer<T>.Compare(T x, T y) => x.CompareTo(y);
        }

        //-----------------------------------------------------------------------------

        public static LinkedListNode<T> AddLastNonAlloc<T>(this LinkedList<T> list, T value)
        {
            var node = SpawnNode<T>();
            node.Value = value;
            list.AddLast(node);
            return node;
        }

        public static void RemoveNonAlloc<T>(this LinkedList<T> list, LinkedListNode<T> node)
        {
            list.Remove(node);
            DespawnNode(node);
        }

        public static int RemoveAllNonAlloc<T>(this LinkedList<T> list, Predicate<T> match)
        {
            var num = 0;
            var cur = list.First;
            while (cur != null)
            {
                var node = cur;
                cur = cur.Next;

                if (match(node.Value))
                {
                    list.RemoveNonAlloc(node);
                    ++num;
                }
            }
            return num;
        }

        public static void ClearNonAlloc<T>(this LinkedList<T> list)
        {
            while (list.Count > 0)
                list.RemoveNonAlloc(list.Last);
        }

        static Dictionary<Type, Queue> _typeToNodeQueue = new Dictionary<Type, Queue>();

        static LinkedListNode<T> SpawnNode<T>() => _typeToNodeQueue.TryGetValue(typeof(T), out var queue) && queue.Count > 0 ? (LinkedListNode<T>)queue.Dequeue() : new LinkedListNode<T>(default);
        static void DespawnNode<T>(LinkedListNode<T> node)
        {
            node.Value = default;
            var t = typeof(T);
            if (!_typeToNodeQueue.TryGetValue(t, out var queue))
            {
                queue = new Queue();
                _typeToNodeQueue.Add(t, queue);
            }
            queue.Enqueue(node);
        }
    }
}
