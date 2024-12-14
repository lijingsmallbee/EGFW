using UnityEngine;
using System.Collections;
using System.Diagnostics;
using NexgenDragon;

namespace NexgenDragon
{
    public static class NLogger
    {
        private const string LOG_COLOR_FORMAT = "<color=#{0:X2}{1:X2}{2:X2}>{3}</color>";

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void Trace(string message, params object[] param)
        {
            LoggerHandler.Log("", null, LogSeverity.Trace, message, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void Trace(Color32 color, string message, params object[] param)
        {
            string formated = string.Format(LOG_COLOR_FORMAT, color.r, color.g, color.b, message);
            LoggerHandler.Log("", null, LogSeverity.Trace, formated, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void TraceChannel(string channel, string message, params object[] param)
        {
            LoggerHandler.Log(channel, null, LogSeverity.Trace, message, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void Log(string message, params object[] param)
        {
            LoggerHandler.Log("", null, LogSeverity.Message, message, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void Log(Color32 color, string message, params object[] param)
        {
            string formated = string.Format(LOG_COLOR_FORMAT, color.r, color.g, color.b, message);
            Log(formated, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void LogChannel(string channel, string message, params object[] param)
        {
            LoggerHandler.Log(channel, null, LogSeverity.Message, message, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void LogChannel(Color32 color, string channel, string message, params object[] param)
        {
            string formated = string.Format(LOG_COLOR_FORMAT, color.r, color.g, color.b, message);
            LogChannel(channel, formated, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void Warn(object message, params object[] param)
        {
            LoggerHandler.Log("", null, LogSeverity.Warning, message, param);
        }

        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void WarnChannel(string channel, string message, params object[] param)
        {
            LoggerHandler.Log(channel, null, LogSeverity.Warning, message, param);
        }

        [StackTraceIgnore]
        public static void Error(object message, params object[] param)
        {
            LoggerHandler.Log("", null, LogSeverity.Error, message, param);
        }

        [StackTraceIgnore]
        public static void ErrorChannel(string channel, string message, params object[] param)
        {
            LoggerHandler.Log(channel, null, LogSeverity.Error, message, param);
        }
        
        [StackTraceIgnore][Conditional("UNITY_EDITOR")][Conditional("MO_LOG")]
        public static void FrameLogChannel(string channel, string message, params object[] param)
        {
            LogChannel(channel, $"[{Time.frameCount}][{Time.realtimeSinceStartup}]{string.Format(message, param)}");
        }

        [StackTraceIgnore]
        public static void Assert(bool condition, string message, params object[] param)
        {
            if (!condition)
            {
                LoggerHandler.Log("", null, LogSeverity.Error, message, param);
            }
        }

        public static void SetSeverity(int severity)
        {
            LoggerHandler.SetSeverity(severity);
        }
        
        public static int GetSeverity()
        {
            return LoggerHandler.GetSeverity();
        }
        
        public static void Exception(System.Exception e)
        {
            LoggerHandler.Exception(e);
        }
        
        public static void Exception(string message)
        {
            LoggerHandler.Exception(message);
        }

        public static void EnableExceptionEvent(bool v)
        {
            NexgenDragon.LoggerHandler.EnableExceptionEvent(v);
        }

        public static void AddExceptionCallback(System.Action<System.Exception> callback)
        {
            LoggerHandler.AddExceptionCallback(callback);
        }
        public static void RemoveExceptionCallback(System.Action<System.Exception> callback)
        {
            LoggerHandler.RemoveExceptionCallback(callback);
        }
    }
}
