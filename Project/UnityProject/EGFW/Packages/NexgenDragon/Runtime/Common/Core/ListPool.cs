using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
    public static class ListPool<T>
    {
        // Object pool to avoid allocations.
        private static readonly CObjectPool<List<T>> s_ListPool = new CObjectPool<List<T>>();
        static void Clear(List<T> l) { l.Clear(); }

        public static List<T> Get()
        {
            var list = s_ListPool.Get();
            list.Clear();
            return list;
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}
