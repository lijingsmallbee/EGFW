using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.Profiling;

namespace NexgenDragon
{
    public static class Perf
    {
        public const string MO_DEBUG = "MO_PROFILER";

        public static bool isProfiler = true;
        //private static StringBuilder _stringBuilder = new StringBuilder();

        //[Conditional(MO_DEBUG)]
        //public static void BeginSample(string className, string functionName, string log)
        //{
        //    if (isProfiler)
        //    {
        //        _stringBuilder.Clear();
        //        _stringBuilder.Append(className);
        //        _stringBuilder.Append(functionName);
        //        _stringBuilder.Append(log);
        //        Profiler.BeginSample(_stringBuilder.ToString());
        //    }
        //}
        [Conditional(MO_DEBUG)]
        public static void BeginSample(string key)
        {
            if (isProfiler)
            {
                Profiler.BeginSample(key);
            }
        }

        [Conditional(MO_DEBUG)]
        public static void EndSample()
        {
            if (isProfiler)
            {
                Profiler.EndSample();
            }
        }
    }
}