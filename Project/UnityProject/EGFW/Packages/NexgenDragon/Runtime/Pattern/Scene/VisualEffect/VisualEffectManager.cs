using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NexgenDragon
{
    public class VisualEffectManager : Singleton<VisualEffectManager>, IManager, ITicker
	{
	    private readonly Dictionary<VisualEffectHandle, VisualEffectHandle> _vfxDict = new Dictionary<VisualEffectHandle, VisualEffectHandle>();
	    private readonly List<VisualEffectHandle> _toBeRemoved = new List<VisualEffectHandle>();
	    
		public void Initialize(NexgenObject configParam)
		{
			GameFacade.Instance.AddTicker(this);
		}

		public void Reset()
		{
            foreach(var pair in _vfxDict)
            {
                pair.Key.Delete();
            }

		    _vfxDict.Clear();
		    _toBeRemoved.Clear();
		}

        public void Clear(string poolName)
        {
            foreach(var pair in _vfxDict)
            {
                var vfx = pair.Key;
                if (vfx.PoolName == poolName)
                {
                    vfx.Delete();
                    _toBeRemoved.Add(vfx);
                }
            }
            
            foreach (var handle in _toBeRemoved)
            {
                _vfxDict.Remove(handle);
            }
            _toBeRemoved.Clear();
        }

		public override void Release()
		{
		    Reset();
		    GameFacade.Instance.RemoveTicker(this);
		}

	    public VisualEffectHandle Create(string uri, string poolName, Transform parent,
	        Action<bool, VisualEffectHandle> callback = null, bool syncCreate = false, int priority = 0, Action<bool, VisualEffectHandle> finishCallback = null)
	    {
	        var pool = GameObjectPoolManager.Instance.GetPool(poolName);
            var effect = new VisualEffectHandle(pool);
            if (string.IsNullOrEmpty(uri))
            {
                return effect;
            }
            
            var vfxName = Path.GetFileNameWithoutExtension(uri);
	        pool.AddRouteMapping(vfxName, uri);
	        effect.Create(vfxName, parent, callback, syncCreate, priority, finishCallback);
	        _vfxDict[effect] = effect;
	        return effect;
	    }

	    public VisualEffectHandle Create(GameObject go, Transform parent,bool keepWorld = false)
	    {
	        var effect = new VisualEffectHandle(go, parent,keepWorld);
	        _vfxDict[effect] = effect;
	        return effect;
	    }

	    public void Tick(float delta)
	    {
		    Perf.BeginSample("VinayGao:VisualEffect.Tick");
	        using (var it = _vfxDict.GetEnumerator())
	        {
	            while (it.MoveNext())
	            {
	                var vfx = it.Current.Key;
	                if (vfx.IsAlive)
	                {
	                    vfx.OnUpdate(delta);
	                }
	                else
	                {
	                    _toBeRemoved.Add(vfx);
	                }
	            }
	        }

	        foreach (var handle in _toBeRemoved)
	        {
	            _vfxDict.Remove(handle);
	        }
	        _toBeRemoved.Clear();
	        Perf.EndSample();
	    }
	}
}