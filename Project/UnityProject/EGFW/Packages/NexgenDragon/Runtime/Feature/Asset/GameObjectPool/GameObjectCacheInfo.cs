using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NexgenDragon
{
    public enum GameObjectCacheInfoState
    {
        NotReady = 0,
        Loading = 1,
        Ready = 2,
        Failure = 3,
    }

    public enum GameObjectCacheHideMode
    {
        Active = 0,
        Position = 1,
        Nothing = 2,    // 什么都不做: 不隐藏, 不移动, 不SetParent
    }
    
    public struct GameObjectCacheDelayRecycle
    {
        public GameObject Instance;
        public float Remaining;
        public GameObjectCacheHideMode hideMode;

        public void Destroy()
        {
            if (!Instance)
            {
                return;
            }
            
            Object.Destroy(Instance);
        }
    }

    public class GameObjectCacheInfo
    {
        public string PrefabName;
        public string AssetPath;
        public AssetHandle Handle;
        public GameObjectCacheInfoState State = GameObjectCacheInfoState.NotReady;
        public float StartTime;
        public float EndTime;
        public int WarmUpCount;
        public bool WarmUpAsSoonAsPossible;
        public Action<GameObject> WarmUpSingle;
        public Action WarmUpDone;
        public static Action<string> onCreate;

        private GameObject _prefab;
        private Transform _root;
        private readonly Stack<GameObject> _stack = new Stack<GameObject>();
        
        private readonly List<GameObjectCacheDelayRecycle> _recycles =
            new List<GameObjectCacheDelayRecycle>();

        private readonly Comparison<GameObjectCacheDelayRecycle> _comparer = Compare;
        
        public static Vector3 HidePosition = new Vector3(1000000, 1000000, 1000000);

        public void Initialize(GameObject prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
        }

        public GameObject Prefab
        {
            get { return _prefab; }
        }

        public GameObject Allocate()
        {
            _lastAllocateTime = Time.time;

            if (!_prefab)
            {
                NLogger.Warn(string.Format("[CacheInfo]corrupted prefab: {0}", PrefabName));
                return null;
            }

            GameObject go = null;

            while (HasCachedGameObject)
            {
                go = _stack.Pop();
                if (!go)
                {
                    NLogger.Warn(string.Format("[CacheInfo]game object was externally deleted: {0}", PrefabName));
                }
                else
                {
                    break;
                }
            }

            if (!go)
            {
                go = Instantiate();
            }

            return go;
        }
        
        public GameObject Instantiate()
        {
            if (!_prefab)
            {
                return null;
            }
            
            var go = Object.Instantiate(_prefab);
            ++_instNum;
            onCreate?.Invoke(_prefab.name);
            // #if UNITY_DEBUG
            // NLogger.Log("pool create game object {0}",_prefab);
            // #endif
            go.name = PrefabName;
            return go;
        }

        public bool HasCachedGameObject
        {
            get { return _stack.Count > 0; }
        }

        public void Recycle(GameObject go, GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            if (_root)
            {
                if (hideMode == GameObjectCacheHideMode.Nothing)
                {
                    // do nothing
                }
                else
                {
                    //go.transform.SetParent(_root,false);
                    if (hideMode == GameObjectCacheHideMode.Active)
                    {
                        go.SetActive(false);
                        go.transform.SetParent(_root, false);
                    }
                    else if (hideMode == GameObjectCacheHideMode.Position)
                    {
                        go.transform.SetParent(_root, false);
                        go.transform.position = HidePosition;
                    }
                }
                _stack.Push(go);
            }
            else
            {
                Object.Destroy(go);
                --_instNum;
            }
        }
        
        public void Recycle(GameObject go, float delay, GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            if (_root)
            {
                var reclaim = new GameObjectCacheDelayRecycle
                {
                    Instance = go,
                    Remaining = delay
                };

                _recycles.Add(reclaim);
            }
            else
            {
                Object.Destroy(go);
                --_instNum;
            }
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var go = _stack.Pop();
                if (go)
                {
                    Object.Destroy(go);
                }
            }

            foreach (var recycle in _recycles)
            {
                recycle.Destroy();
            }
            _recycles.Clear();

            _prefab = null;
            _root = null;
            State = GameObjectCacheInfoState.NotReady;
            if (Handle != null)
            {
                AssetUtils.UnloadAsset(Handle);
                Handle = null;
            }
            _instNum = 0;
        }

        public void Update(float dt)
        {
            if (!GameObjectPool.autoClearGoCacheInfo)
                CheckToFreeMem();

            // 更新剩余时间
            var loops = _recycles.Count;
            for (var i = 0; i < loops; i++)
            {
                var recycle = _recycles[i];
                recycle.Remaining -= dt;
                _recycles[i] = recycle;
            }
            
            // 根据剩余时间降序排序
            _recycles.Sort(_comparer);

            // 回收过期的GameObject
            while (_recycles.Count > 0)
            {
                var lastIndex = _recycles.Count - 1;
                var lastRecycle = _recycles[lastIndex];
                if (lastRecycle.Remaining > 0)
                {
                    break;
                }
                
                Recycle(lastRecycle.Instance, lastRecycle.hideMode);
                _recycles.RemoveAt(lastIndex);
            }
        }
        
        private static int Compare(GameObjectCacheDelayRecycle x, GameObjectCacheDelayRecycle y)
        {
            return Math.Sign(y.Remaining - x.Remaining);
        }

        //-------------------------------------------------------------------------------------------------

        public static float freeMemDuration = 60 * 5;

        public bool notClearThisTime;
        public bool dontClearCache; //v1.1.220新添加接口, 不再通过freeMemDuration机制清理对象池

        public bool needClear => _instNum > 0 && _instNum == _stack.Count && Time.time - _lastAllocateTime > freeMemDuration && !dontClearCache;

        int _instNum;
        float _lastAllocateTime;

        void CheckToFreeMem()
        {
            if ((_stack.Count == 0 && _recycles.Count == 0) || Time.time - _lastAllocateTime < freeMemDuration)
                return;

            if (dontClearCache)
                return;
            
            while (_stack.Count > 0)
            {
                var go = _stack.Pop();
                if (go)
                    Object.Destroy(go);
            }
            foreach (var recycle in _recycles)
                recycle.Destroy();
            _recycles.Clear();
        }
    }
}