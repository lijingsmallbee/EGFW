using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NexgenDragon
{
	public static class TimeUtils
	{
		public static readonly System.DateTime TIME19700101 = new System.DateTime(1970, 1, 1);
        public static bool EnableRecord = false;

        private static List<string[]> _records = new List<string[]>();

        private static Stopwatch sw = new Stopwatch();

		public static float Time
		{
			get
			{
				return UnityEngine.Time.time;
			}
		}

		public static float RealtimeSinceStartup
		{
			get
			{
				return UnityEngine.Time.realtimeSinceStartup;
			}
		}

        /// <summary>
        /// 获取本机当前时间戳(ms)
        /// </summary>
        /// <value>The local timestamp.</value>
        public static long LocalTimestamp
        {
            get
            {
				TimeSpan timeSpan = System.DateTime.UtcNow - TIME19700101;
				return (long)timeSpan.TotalMilliseconds;
            }
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - TIME19700101;
            return (long)timeSpan.TotalSeconds;
        }
        
        public static long DateTimeToUnixTimestampMilliseconds(DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - TIME19700101;
            return (long)timeSpan.TotalMilliseconds;
        }

        // 时间戳，秒
        public static string UnixTimeStmapToLocalDateTime(long unixTimeStamp)
        {
			System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(TIME19700101); // 当地时区
            DateTime dt = startTime.AddSeconds(unixTimeStamp);
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

		public static string UnixTimeStmapToUtcDateTime(long unixTimeStamp)
		{
			System.DateTime startTime = TIME19700101;
			DateTime dt = startTime.AddSeconds(unixTimeStamp);
			return dt.ToString("yyyy-MM-dd HH:mm:ss"); // Jiacan Li: 这个格式是后端需要的不能随意更改
		}

		public static void StartRecord()
		{
            if(EnableRecord)
            {
    			sw.Reset();
    			sw.Start();
    			NLogger.Log("[StopWatch]Record start, time is: {0}ms", sw.ElapsedMilliseconds);
			}
		}

		public static void UpdateRecord(string logInfo, params object[] param)
		{
            if (EnableRecord)
            {
                var msg = string.Format(logInfo, param);
                NLogger.Log("[StopWatch]Record update, {0}, time is: {1}ms", msg, sw.ElapsedMilliseconds);  
                _records.Add(new string[]{msg, sw.ElapsedMilliseconds.ToString()});
            }
		}
		
		public static void CleanAndSaveRecord(string path)
		{
			if (EnableRecord)
			{
				StringBuilder _stringBuilder = new StringBuilder();
				foreach (var record in _records)
				{
					var line = record[0] + "," + record[1];
					_stringBuilder.AppendLine(line);
				}

				File.WriteAllText(path, _stringBuilder.ToString());
				
				_records.Clear();
			}
		}
	}
}

