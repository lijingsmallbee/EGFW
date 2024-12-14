using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Text;
namespace NexgenDragon
{
	public class EventManager: Singleton<EventManager>, IManager
	{
		private readonly Dictionary<string, LinkedList<Action<IEvent>>> _eventDict =
			new Dictionary<string, LinkedList<Action<IEvent>>>();

		public void Initialize(NexgenObject configParam)
		{
		}

		public void Reset() 
		{
            Release();
		}

		public void AddListener (string eventType, Action<IEvent> listener)
		{
			if (listener == null)
			{
				return;
			}

			_eventDict.TryGetValue (eventType, out var listeners);
			if (listeners == null)
			{
				listeners = new LinkedList<Action<IEvent>> ();
				_eventDict [eventType] = listeners;
			}

			if (!listeners.Contains (listener))
			{
				listeners.AddLast (listener);
			}
		}

		public void RemoveListener (string eventType, Action<IEvent> listener)
		{
			if (listener == null)
			{
				return;
			}

			_eventDict.TryGetValue (eventType, out var listeners);
			listeners?.Remove (listener);
		}

		public void TriggerEvent (IEvent evt)
		{
			Perf.BeginSample("VinayGao:EventManager.TriggerEvent");
			if (evt == null)
			{
				Perf.EndSample();
				return;
			}

			_eventDict.TryGetValue (evt.GetEventType(), out var listeners);
			if (listeners == null)
			{
				Perf.EndSample();
				return;
			}
			
			var record = listeners.First;
			while (record != null)
			{
				var listener = record.Value;
				record = record.Next;
				try
				{
					listener.Invoke (evt);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
			Perf.EndSample();
		}

		public override void Release()
		{
			_eventDict.Clear ();
		}

        public string DumpAllListeners()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var listeners in _eventDict)
            {
                sb.AppendFormat("Event Name: {0} ({1}) >>>>>\n", listeners.Key, listeners.Value.Count);

                foreach (var listener in listeners.Value)
                {
                    if (listener.Target != null)
                    {
                        sb.AppendFormat("Member Function: {0}.{1}\n", listener.Target.GetType(), listener.Method.Name);
                    }
                    else
                    {
                        sb.AppendFormat("Static Function: {0}\n", listener.Method.Name);
                    }
                }
            }

            return sb.ToString();
        }
	}
}