using System;

namespace NexgenDragon
{
	public class Singleton<T> : NexgenObject where T : class ,new()
	{
        private static readonly object _locker = new object();

		private static T _instance;
		public static T Instance
		{
			get
			{
                lock(_locker)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                    return _instance;
                }
			}
		}

		public static void DestroyInstance()
		{
			lock (_locker)
			{
				_instance = null;
			}
		}
	}
}