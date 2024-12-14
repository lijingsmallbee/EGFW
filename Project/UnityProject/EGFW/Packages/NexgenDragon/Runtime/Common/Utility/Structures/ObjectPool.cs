using System.Collections.Generic;

namespace NexgenDragon
{
	public class ObjectPool<T> where T:class, new()
	{
		Stack<T> m_Pool = new Stack<T>();

		// allocate an object
		public T Allocate()
		{
			if (m_Pool.Count > 0)
			{
				return m_Pool.Pop ();
			}
			return new T();
		}

		// release an object
		public void Release(T o)
		{
			m_Pool.Push (o);
		}

		public void Clear()
		{
			m_Pool.Clear();	
		}

        public int CacheSize
        {
            get
            {
                return m_Pool.Count;
            }
        }
	}
}