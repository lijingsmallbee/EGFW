using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
	public delegate void GameObjectRequestCallback(GameObject go, object userData);

	public class GameObjectRequest
	{
		public GameObjectCacheInfo cacheInfo;
		public GameObjectRequestCallback callback;
		private bool _needCancel;
		public object userData;
		public Transform parent;
		public int priority;
		public bool syncCreate;
		public long reqId;
		public bool logCancel;

		public bool cancel => _needCancel;

		public void Reset()
		{
			cacheInfo = null;
			callback = null;
			_needCancel = false;
			userData = null;
			parent = null;
			priority = 0;
			syncCreate = false;
			logCancel = false;
		}
		
		public void Cancel()
		{
			_needCancel = true;
			if (logCancel)
			{
				NLogger.Error($"[GameObjectPool]create canceled {reqId}");
			}
		}
	}
}