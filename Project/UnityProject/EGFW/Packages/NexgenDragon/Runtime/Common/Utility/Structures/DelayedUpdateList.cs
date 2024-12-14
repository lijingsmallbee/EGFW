using System.Collections.Generic;

namespace NexgenDragon
{
	/// <summary>
	/// Delayed update list.
	/// Every Add or Remove call will delay to the end of that frame
	/// </summary>
	public class DelayedUpdateList<T> : NexgenObject
	{
		protected LinkedList<T> _list = new LinkedList<T>();
		private List<T> _addCache = new List<T>();
		private List<T> _removeCache = new List<T>();

		public void Add(T item)
		{
            NLogger.Assert((!_addCache.Contains(item) && !_list.Contains(item)) || _removeCache.Contains(item),"Item " + item + " already in this DelayedUpdateLsit.");

			if(!_removeCache.Remove(item))
			{
				_addCache.Add(item);
			}
		}

		public void Remove(T item)
		{
			if(!_addCache.Remove(item))
			{
				_removeCache.Add(item);
			}
		}

		public void FastAdd(T item)
		{
			_list.AddLast(item);
		}

		public bool FastRemove(T item)
		{
			return _list.Remove(item);
		}

		public virtual void Tick(float delta)
		{
			T item = default(T);

			for(int i = 0; i < _addCache.Count; ++i)
			{
				item = _addCache[i];
				_list.AddLast(item);
			}

			_addCache.Clear();


			for(int i = 0; i < _removeCache.Count; ++ i)
			{
				item = _removeCache[i];
				_list.Remove(item);
			}

			_removeCache.Clear();
		}

		public virtual void Clear()
		{
			_addCache.Clear();
			_removeCache.Clear();

			_list.Clear();
		}
	}
}


