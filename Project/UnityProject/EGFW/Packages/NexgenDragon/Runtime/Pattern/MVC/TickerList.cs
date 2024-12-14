using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
	public class TickerList
	{
		private readonly LinkedList<ITicker> _updateList = new LinkedList<ITicker>();
		private readonly HashSet<ITicker> _removeList = new HashSet<ITicker>();
        private readonly bool _ignoreTimeScale;
		private float _lastTickTime = -1.0f;

        public TickerList(bool ignoreTimeScale)
        {
            _ignoreTimeScale = ignoreTimeScale;
        }

		public float LastTickTime
		{
			get
			{
				return _lastTickTime;
			}
		}

		public void Add(ITicker ticker)
		{
			if (!_updateList.Contains (ticker))
			{
				_updateList.AddLastNonAlloc (ticker);
			}
			_removeList.Remove(ticker);
		}

		public void Remove(ITicker ticker)
		{
			_removeList.Add(ticker);
		}

		public void Tick()
        {
            var currentTime = GetCurrentTime();
			var deltaTime = _lastTickTime > 0 ? currentTime - _lastTickTime : 0;
			_lastTickTime = currentTime;

			UpdateTickers(deltaTime);
			RemoveTickers();
		}

        private float GetCurrentTime()
        {
            return _ignoreTimeScale ? TimeUtils.RealtimeSinceStartup : TimeUtils.Time;
        }

		private void UpdateTickers(float deltaTime)
		{
			var node = _updateList.First;
			while (node != null)
			{
				var ticker = node.Value;
				node = node.Next;

				if (!_removeList.Contains(ticker))
				{
					try
					{
						ticker.Tick(deltaTime);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}

		private void RemoveTickers()
		{
			var node = _updateList.First;
			while (node != null)
			{
				var candidate = node;
				node = node.Next;

				if (_removeList.Contains(candidate.Value))
				{
					_updateList.RemoveNonAlloc(candidate);
				}
			}
			_removeList.Clear();
		}

		public void Clear ()
		{
			_lastTickTime = -1.0f;
			_updateList.ClearNonAlloc();
			_removeList.Clear();
		}

        public void DumpList()
        {
            foreach (var del in _updateList)
            {
                NLogger.ErrorChannel("Ticker Dump", "ticker type {0}", del.GetType().FullName);
            }
        }

        public LinkedList<ITicker> GetAllUpdateList()
        {
	        return _updateList;
        }
	}
}

