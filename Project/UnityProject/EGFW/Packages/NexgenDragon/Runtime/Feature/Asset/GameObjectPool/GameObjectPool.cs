using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace NexgenDragon
{
    public class GameObjectPool : ITimeScaleIgnoredTicker
    {
        private static long _reqId = 0;

        private readonly Dictionary<string, GameObjectCacheInfo> _cacheDict =
            new Dictionary<string, GameObjectCacheInfo>();

        private readonly List<GameObjectRequest> _usedRequests = new List<GameObjectRequest>();
        private readonly ObjectPool<GameObjectRequest> _freeRequests = new ObjectPool<GameObjectRequest>();
        private readonly Comparison<GameObjectRequest> _comparer = Compare;
        private Transform _rootNode;
        private readonly List<GameObjectCacheInfo> _loop = new List<GameObjectCacheInfo>();
        private readonly string _name;
        private string vinayGaoClassType = null;

        public string VinayGaoGetClassType
        {
            get
            {
                if (vinayGaoClassType == null)
                {
                    vinayGaoClassType = "VinayGao:" + GetType().Name;
                }

                return vinayGaoClassType;
            }
        }

        public GameObjectPool(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        // 异步创建的回调持续时间
        public static float AsyncCreateCallbackTime { get; private set; } = 0.003f;

        public void Initialize(Transform rootNode)
        {
            _rootNode = rootNode;
        }

        public void ClearCache()
        {
            CancelAll();

            foreach (var pair in _cacheDict)
            {
                pair.Value.Clear();
            }

            _cacheDict.Clear();
            _usedRequests.Clear();
            _freeRequests.Clear();
        }

        public void WarmUp(string prefabName, string assetPath, int count, Action<GameObject> warmUpSingle = null,
            Action warmUpDone = null, int priority = 0, bool asSoonAsPossible = false)
        {
            var cacheInfo = AddRouteMapping(prefabName, assetPath);
            cacheInfo.WarmUpSingle = warmUpSingle;
            cacheInfo.WarmUpDone = warmUpDone;
            cacheInfo.WarmUpCount = count;
            cacheInfo.WarmUpAsSoonAsPossible = asSoonAsPossible;

            if (cacheInfo.State == GameObjectCacheInfoState.NotReady)
            {
                cacheInfo.StartTime = Time.realtimeSinceStartup;
                cacheInfo.State = GameObjectCacheInfoState.Loading;
                cacheInfo.Handle = AssetUtils.LoadAssetAsync(assetPath, delegate(bool ret, AssetHandle handle)
                {
                    if (cacheInfo.State != GameObjectCacheInfoState.Ready)
                    {
                        OnAssetLoaded(ret, handle);
                    }
                    else
                    {
                        AssetUtils.UnloadAsset(handle);
                    }
                }, priority, false);
            }
        }

        public GameObjectCacheInfo AddRouteMapping(string prefabName, string assetPath)
        {
            if (string.IsNullOrEmpty(prefabName) || string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo == null)
            {
                cacheInfo = new GameObjectCacheInfo { PrefabName = prefabName, AssetPath = assetPath };
                _cacheDict[prefabName] = cacheInfo;
            }

            return cacheInfo;
        }

        public GameObjectRequest Create(string prefabName, Transform parent, GameObjectRequestCallback callback,
            object userData = null, int priority = 0, bool syncCreate = false)
        {
            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo == null)
            {
                NLogger.WarnChannel("GameObjectPool", "Create: Cache for {0} doesn't exist.", prefabName);
                callback(null, userData);
                return null;
            }

            GameObjectRequest request = null;
            if (cacheInfo.HasCachedGameObject)
            {
                var go = InternalSpawnGameObject(cacheInfo, parent);
                callback(go, userData);
            }
            else
            {
                if (syncCreate && cacheInfo.Prefab)
                {
                    var go = InternalSpawnGameObject(cacheInfo, parent);
                    callback(go, userData);
                }
                else
                {
                    request = AllocateRequest();
                    request.Reset();
                    request.cacheInfo = cacheInfo;
                    request.callback = callback;
                    request.userData = userData;
                    request.parent = parent;
                    request.priority = priority;
                    request.syncCreate = syncCreate;
                    _usedRequests.Add(request);
                }
            }

            return request;
        }

        /// <summary>
        /// 不推荐使用 如果必须的话 使用 : public GameObject CreateSync (string prefabName, Transform parent)
        /// </summary>
        [Obsolete("Synchronize create will not be supported soon. ")]
        public GameObject Create(string prefabName, Transform parent)
        {
            return CreateSync(prefabName, parent);
        }

        /// <summary>
        /// 用于代替：public GameObject Create (string prefabName, Transform parent)
        /// 不推荐使用 未来也考虑废弃
        /// </summary>
        public GameObject CreateSync(string prefabName, Transform parent)
        {
            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo == null)
            {
                NLogger.WarnChannel("GameObjectPool", "Create: Cache for {0} doesn't exist.", prefabName);
                return null;
            }

            if (cacheInfo.State == GameObjectCacheInfoState.NotReady)
            {
                cacheInfo.StartTime = Time.realtimeSinceStartup;
                cacheInfo.Handle = AssetUtils.LoadAsset(cacheInfo.AssetPath);
                var prefab = cacheInfo.Handle.asset as GameObject;
                if (prefab)
                {
                    cacheInfo.Initialize(prefab, _rootNode);
                    cacheInfo.State = GameObjectCacheInfoState.Ready;
                    cacheInfo.EndTime = Time.realtimeSinceStartup;
                }
                else
                {
                    cacheInfo.State = GameObjectCacheInfoState.Failure;
                    cacheInfo.EndTime = Time.realtimeSinceStartup;
                }
            }

            return InternalSpawnGameObject(cacheInfo, parent);
        }

        public void Destroy(GameObject go, GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            Destroy(go.name, go, hideMode);
        }

        public void Destroy(string prefabName, GameObject go,
            GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo == null)
            {
                Object.Destroy(go);
                return;
            }

            cacheInfo.Recycle(go, hideMode);
        }

        public void Destroy(GameObject go, float delay,
            GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            Destroy(go.name, go, delay);
        }

        public void Destroy(string prefabName, GameObject go, float delay,
            GameObjectCacheHideMode hideMode = GameObjectCacheHideMode.Active)
        {
            if (!go)
            {
                return;
            }

            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo == null)
            {
                Object.Destroy(go, delay);
                return;
            }

            cacheInfo.Recycle(go, delay, hideMode);
        }

        public void CancelAll()
        {
            foreach (var request in _usedRequests)
            {
                request.Cancel();
                ReleaseRequest(request);
            }

            _usedRequests.Clear();
        }

        public void Tick(float delta)
        {
            Perf.BeginSample("VinayGao:GameObjectPool.Tick");
            ProcessSingleRequest();
            ProcessDelayRecycle(delta);
            Perf.EndSample();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Private Member Methods
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        GameObjectRequest AllocateRequest()
        {
            var req = _freeRequests.Allocate();
            req.reqId = _reqId++;
            return req;
        }

        private void ReleaseRequest(GameObjectRequest request)
        {
            if (request == null)
            {
                return;
            }

            _freeRequests.Release(request);
        }

        private void ProcessDelayRecycle(float dt)
        {
            if (autoClearGoCacheInfo)
            {
                foreach (var usedReq in _usedRequests)
                    usedReq.cacheInfo.notClearThisTime = true;
            }

            foreach (var pair in _cacheDict)
            {
                var goCacheInfo = pair.Value;
                goCacheInfo.Update(dt);

                if (autoClearGoCacheInfo)
                {
                    if (!goCacheInfo.notClearThisTime && goCacheInfo.needClear && goCacheInfo.WarmUpCount == 0)
                    {
                        //Debug.Log($"[GameObjectPool] Clear Cache: {goCacheInfo.PrefabName}");
                        goCacheInfo.Clear();
                        _lastClearGoCacheInfoTime = Time.time;
                    }
                }
            }

            if (autoClearGoCacheInfo)
            {
                foreach (var pair in _cacheDict)
                    pair.Value.notClearThisTime = false;
            }

            if (_lastClearGoCacheInfoTime != 0 && Time.time - _lastClearGoCacheInfoTime > 5)
            {
                _lastClearGoCacheInfoTime = 0;
                AssetManager.Instance.UnloadUnused();
            }
        }

        private void ProcessSingleRequest()
        {
            // 防止Out of Sync
            _loop.Clear();
            foreach (var pair in _cacheDict)
            {
                _loop.Add(pair.Value);
            }

            // Warm Up
            foreach (var item in _loop)
            {
                InternalWarmUp(item);
            }

            _usedRequests.Sort(_comparer);

            // 删除尾部的被取消的请求
            for (int i = _usedRequests.Count - 1; i >= 0; i--)
            {
                var request = _usedRequests[i];
                if (request.cancel)
                {
                    _usedRequests.RemoveAt(i);
                    ReleaseRequest(request);
                }
            }

            // 启动尾部未开始的请求
            for (var i = _usedRequests.Count - 1; i >= 0; --i)
            {
                var request = _usedRequests[i];
                if (request.cacheInfo.State == GameObjectCacheInfoState.NotReady)
                {
                    request.cacheInfo.StartTime = Time.realtimeSinceStartup;
                    request.cacheInfo.State = GameObjectCacheInfoState.Loading;
                    request.cacheInfo.Handle = AssetUtils.LoadAssetAsync(request.cacheInfo.AssetPath, OnAssetLoaded,
                        request.priority, false);
                }
                else
                {
                    break;
                }
            }

            var requestProcessBeginTime = Time.realtimeSinceStartup;

            // 如果头部有已经失败的请求，则先将头部所有失败的请求删掉，然后继续处理后面的请求
            // 如果头部有已经就绪的请求，则处理所有需要立即实例化的请求，然后处理一个分帧加载的请求
            for (int index = 0; index < _usedRequests.Count; ++index)
            {
                var request = _usedRequests[index];
                if (request.cacheInfo.State == GameObjectCacheInfoState.Failure)
                {
                    if (null != request.callback)
                    {
                        request.callback(null, request.userData);
                        request.callback = null; // 防止闭包引起的内存泄露
                    }

                    // 将最后一个请求移到当前位置，删除最后一个元素，回收当前请求
                    var lastIndex = _usedRequests.Count - 1;
                    _usedRequests[index] = _usedRequests[lastIndex];
                    _usedRequests.RemoveAt(lastIndex);
                    ReleaseRequest(request);
                    index--;
                }
                else if (request.cacheInfo.State == GameObjectCacheInfoState.Ready)
                {
                    if (request.cancel)
                    {
                        continue;
                    }

                    var syncCreate = request.syncCreate;
                    var go = InternalSpawnGameObject(request.cacheInfo, request.parent);

                    if (null != request.callback)
                    {
                        request.callback(go, request.userData);
                        request.callback = null; // 防止闭包引起的内存泄露
                    }

                    // 将最后一个请求移到当前位置，删除最后一个元素，回收当前请求
                    var lastIndex = _usedRequests.Count - 1;
                    _usedRequests[index] = _usedRequests[lastIndex];
                    _usedRequests.RemoveAt(lastIndex);
                    ReleaseRequest(request);
                    index--;

                    if (!syncCreate)
                    {
                        if (Time.realtimeSinceStartup - requestProcessBeginTime > AsyncCreateCallbackTime)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public void PrintRequests()
        {
            NLogger.TraceChannel("GameObjectPool", "PrintRequests: Begin");
            foreach (var request in _usedRequests)
            {
                NLogger.TraceChannel("GameObjectPool", "State = {0}, Cancel = {1}, SyncCreate = {2}, Priority = {3}",
                    request.cacheInfo.State, request.cancel, request.syncCreate, request.priority);
            }

            NLogger.TraceChannel("GameObjectPool", "PrintRequests: End");
        }

        public void PrintLoadTimes()
        {
            foreach (var pair in _cacheDict)
            {
                var cacheInfo = pair.Value;
                var duration = cacheInfo.EndTime - cacheInfo.StartTime;
                NLogger.TraceChannel("GameObjectPool", "PrintLoadTimes: {0} = {1}", cacheInfo.PrefabName, duration);
            }
        }

        public Dictionary<string, string> DumpAllAssets()
        {
            var dic = new Dictionary<string, string>();
            foreach (var pair in _cacheDict)
            {
                dic.Add(pair.Key, pair.Value.AssetPath);
            }

            return dic;
        }

        private static int Compare(GameObjectRequest x, GameObjectRequest y)
        {
            // 取消的请求排在最后面
            var xCancel = x.cancel ? 1 : 0;
            var yCancel = y.cancel ? 1 : 0;
            var cancelDiff = xCancel - yCancel;
            if (cancelDiff != 0)
            {
                return cancelDiff;
            }

            // 排列顺序：Failure > Ready > Loading > NotReady
            var xState = (int)x.cacheInfo.State;
            var yState = (int)y.cacheInfo.State;
            var stateDiff = yState - xState;
            if (stateDiff != 0)
            {
                return stateDiff;
            }

            // 同步实例化的排在前面
            var xImmediately = x.syncCreate ? 1 : 0;
            var yImmediately = y.syncCreate ? 1 : 0;
            var immediatelyDiff = yImmediately - xImmediately;
            if (immediatelyDiff != 0)
            {
                return immediatelyDiff;
            }

            // 优先级数值大的排在前面
            return y.priority - x.priority;
        }

        private void OnAssetLoaded(bool ret, AssetHandle handle)
        {
            var prefabName = Path.GetFileNameWithoutExtension(handle.path);
            if (string.IsNullOrEmpty(prefabName))
            {
                NLogger.WarnChannel("GameObjectPool", "OnAssetLoaded: Can not parse prefab name from path {0}.",
                    handle.path);
                return;
            }

            GameObjectCacheInfo cacheInfo;
            _cacheDict.TryGetValue(prefabName, out cacheInfo);
            if (cacheInfo != null)
            {
                var prefab = handle.asset as GameObject;
                if (prefab)
                {
                    cacheInfo.Initialize(prefab, _rootNode);
                    cacheInfo.State = GameObjectCacheInfoState.Ready;
                    cacheInfo.EndTime = Time.realtimeSinceStartup;
                }
                else
                {
                    cacheInfo.State = GameObjectCacheInfoState.Failure;
                    cacheInfo.EndTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                NLogger.WarnChannel("GameObjectPool", "OnAssetLoaded: Cache for {0} doesn't exist.", prefabName);
            }
        }

        private static GameObject InternalSpawnGameObject(GameObjectCacheInfo cacheInfo, Transform parent)
        {
            if (cacheInfo == null || !cacheInfo.Prefab)
            {
                return null;
            }

            // allocate an object in the cache
            var go = cacheInfo.Allocate();
            go.SetActive(true);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static void InternalWarmUp(GameObjectCacheInfo cacheInfo)
        {
            switch (cacheInfo.State)
            {
                case GameObjectCacheInfoState.Ready:
                {
                    if (cacheInfo.WarmUpCount > 0)
                    {
                        while (cacheInfo.WarmUpCount > 0)
                        {
                            var go = cacheInfo.Instantiate();
                            if (go)
                            {
                                if (cacheInfo.WarmUpSingle != null)
                                {
                                    cacheInfo.WarmUpSingle(go);
                                }

                                cacheInfo.Recycle(go);
                            }

                            --cacheInfo.WarmUpCount;

                            if (cacheInfo.WarmUpCount == 0)
                            {
                                if (cacheInfo.WarmUpDone != null)
                                {
                                    cacheInfo.WarmUpDone();
                                    cacheInfo.WarmUpDone = null;
                                }

                                cacheInfo.WarmUpSingle = null;
                                break;
                            }
                            else if (!cacheInfo.WarmUpAsSoonAsPossible)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (cacheInfo.WarmUpDone != null)
                        {
                            cacheInfo.WarmUpDone();
                            cacheInfo.WarmUpDone = null;
                        }

                        cacheInfo.WarmUpSingle = null;
                    }

                    break;
                }

                case GameObjectCacheInfoState.Failure:
                    if (cacheInfo.WarmUpDone != null)
                    {
                        cacheInfo.WarmUpDone();
                    }

                    cacheInfo.WarmUpDone = null;
                    cacheInfo.WarmUpSingle = null;
                    cacheInfo.WarmUpCount = 0;
                    break;
            }
        }
        
        //v1.1.220新添加接口
        public GameObjectCacheInfo GetCacheInfo(string prefabName, string assetPath)
        {
	        return AddRouteMapping(prefabName, assetPath);
        }
        
        //v1.1.220新添加接口
        public GameObjectCacheInfo SetDontClearCache(string prefabName, string assetPath, bool dontClearCache)
        {
	        var cacheInfo = GetCacheInfo(prefabName, assetPath);
			if (cacheInfo != null)
			{
				cacheInfo.dontClearCache = dontClearCache;
			}
			return cacheInfo;
        }

        //-------------------------------------------------------------------------------------------------

        public static bool autoClearGoCacheInfo = true;

        static float _lastClearGoCacheInfoTime;
    }
}