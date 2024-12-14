using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NexgenDragon;
using System.IO;
using Object = UnityEngine.Object;

//代表一个assetbundle以及他的依赖加载项
enum AssetBundleLoadingState
{
    none,
    loading,
    complete,
}

enum AssetBundleLoadingType
{
    single,
    dependence,
}

internal class AssetBundleCache : AssetReferenceCountBase
{
    public const string demoHash = "41023040a8f754657299da31350b250f.unity3d";
    protected AssetBundleCacheManager _manager = null;

    private List<string> _allReasons = new List<string>(4);

    //用来进行bundle异步加载结果查询的类
    public AssetBundleCache(string bundleName, AssetBundleCacheManager manager)
    {
        _manager = manager;
        _bundleName = bundleName;
        var hashLen = demoHash.Length;
        if (_bundleName.Length > hashLen)
        {
            _bundleNameNoHash = BundleDependence.Instance.GetOriginalBundleName(_bundleName);
            if (string.IsNullOrEmpty(_bundleNameNoHash))
            {
                _bundleNameNoHash = _bundleName.Substring(0, _bundleName.Length - demoHash.Length - 1);    
            }
        }
    }

    public void Initialize()
    {
        if (AssetBundleConfig.Instance.IsConstBundle(_bundleName))
        {
            _constBundle = true;
        }

        if (AssetBundleConfig.Instance.IsLuaBundle(_bundleName))
        {
            _constBundle = true;
        }
        
        if (AssetBundleConfig.Instance.IsShaderBundle(_bundleName))
        {
            _constBundle = true;
        }

        _dependenceData = BundleDependence.Instance.GetBundleDependence(_bundleName);
        if (_dependenceData.dependences.Count > 0)
        {
            _bundleType = AssetBundleLoadingType.dependence;
            //增加对于依赖bundle的引用计数
            var dependenceIt = _dependenceData.dependences.GetEnumerator();
            while (dependenceIt.MoveNext())
            {
                _manager.GetAssetBundleCache(dependenceIt.Current).Increase(_bundleName);
            }

            dependenceIt.Dispose();
        }
        else
        {
            _bundleType = AssetBundleLoadingType.single;
        }
    }

    private AssetBundle _assetBundle = null;
    private List<AssetBundleRequest> assetBundleRequests = new List<AssetBundleRequest>(4);
    private string _bundleName;
    private string _bundleNameNoHash;
    private AssetBundleLoadingType _bundleType;
    private BundleDependenceData _dependenceData;
    private AssetBundleLoadingState _state = AssetBundleLoadingState.none;
    private event Action<bool> _completeEvent;
    //解决如果prefab相互依赖，被依赖的prefab 的bundle可能当做被依赖项加载之后无法加载自己的依赖项的问题
    private bool _allDependenceLoaded = false;
    private bool _constBundle = false;
    private bool _loadDependence = false;
    private AssetBundleCreateRequest _req;

    public AssetBundleLoadingState LoadingState
    {
        get { return _state; }
    }

    //release没有考虑_assetBundle为空的情况，因为release因为引用计数减少导致引用计数减少因为资源销毁导致
    //资源销毁逻辑加了限制，只有加载完毕的资源才可以销毁，加载完毕的条件就是所在的以及依赖的bundle都加载完毕
    //所以bundle被卸载的时候，一定是资源加载完毕，也就是bundle加载完毕的时候
    public override void Release()
    {
        if (_assetBundle != null)
        {
            if(assetBundleRequests.Count > 0)
            {
                //这种情况不能卸载，可以清空req，bundle不卸载再次加载有容错逻辑
                assetBundleRequests.Clear();
                NLogger.Error($"bundle {_bundleName} release but req > 0");
            }
            else
            {
                _assetBundle.Unload(true);
                _assetBundle = null;
            }            
        }

        //减少依赖的asset bundle的引用计数
        var dependenceIt = _dependenceData.dependences.GetEnumerator();
        while (dependenceIt.MoveNext())
        {
            _manager.GetAssetBundleCache(dependenceIt.Current).Decrease(_bundleName);
        }

        dependenceIt.Dispose();
    }

    public override bool Decrease()
    {
        if (_constBundle)
            return false;
        var needRemove = base.Decrease();
        if (_referenceCount <= 0)
        {
            Release();
            _manager.CheckZeorBundle();
        }

        return needRemove;
    }

    public bool Decrease(string reason)
    {
        if (_constBundle)
            return false;
        _allReasons.Remove(reason);
        var needRemove = base.Decrease();
        if (_referenceCount <= 0)
        {
            Release();
            _manager.CheckZeorBundle();
        }

        return needRemove;
    }

    public void Increase(string reason)
    {
        _allReasons.Add(reason);
        Increase();
    }

    public UnityEngine.Object LoadAsset(string assetName, bool isSprite = false)
    {
        if (_assetBundle == null)
        {
#if ASSET_DEBUG
            NLogger.LogChannel("Asset", $"Start LoadAsset: AssetBundle null {assetName}");
#endif 
            if(AssetManager.Instance.OptimizeError)
            {
                var msg = $"[AssetError] Sync load bundle asset {assetName} from {_bundleName} faild ,bundle null";
                //Firebase.Crashlytics.Crashlytics.LogException(new Exception(msg));
                
                //同步加载资源失败不再弹窗，改为删除bundle上报同时下载资源
                var bundleLocalPath = AssetBundleSynchro.Instance.GetBundleLocalPath(_bundleName);
                AssetManager.Instance.IOtool.DeleteGameAsset(bundleLocalPath);
                Download();
            }
            else
            {
                EventManager.Instance.TriggerEvent(new LoadExistBundleFail(_bundleName, assetName));
            }

            return null;
        }

        /*
        if (AssetManager.Instance.EnvironmentVariable.USE_SBP())
        {
            assetName = BundleAssetDataManager.Instance.GetAssetPath(assetName);
        }
        */
        
        if (string.IsNullOrEmpty(assetName))
        {
#if ASSET_DEBUG
            NLogger.LogChannel("Asset", $"Start LoadAsset: assetName null");
#endif            
            return null;
        }
        
        if (isSprite)
        {
            return _assetBundle.LoadAsset(assetName, typeof(Sprite));
        }
        else
        {
            return _assetBundle.LoadAsset(assetName);
        }
    }

    //这里实现没有缓存callback是因为在最外层已经保证了同一个资源的请求不会在加载中再次请求
    //详细见AssetManager和AssetCache的实现
    public bool LoadAsset(string assetName, Action<UnityEngine.Object> callback, bool isSprite, int priority = 999,
        bool asap = false)
    {
        if (_assetBundle == null)
        {
            callback(null);
            return false;
        }

        /*
        if (AssetManager.Instance.EnvironmentVariable.USE_SBP())
        {
            assetName = BundleAssetDataManager.Instance.GetAssetPath(assetName);
        }
        */

        if (string.IsNullOrEmpty(assetName))
        {
            // 如果资源名是""或者是null，AssetBundle.LoadAssetAsync和AssetBundle.LoadAsset会抛异常
            callback(null);
            return false;
        }

        if (!asap)
        {
            AssetBundleRequest request = null;
            if (isSprite)
            {
                request = _assetBundle.LoadAssetAsync(assetName, typeof(Sprite));
            }
            else
            {
                request = _assetBundle.LoadAssetAsync(assetName);
            }
            assetBundleRequests.Add(request);
            var task = AsyncRequest<AssetBundleRequest>.Create(request, OnRequestDone, callback);
            AsyncRequestManager.Instance.AddTask(task);
            return request != null;
        }
        else
        {
            Object asset;

            if (isSprite)
            {
                asset = _assetBundle.LoadAsset(assetName, typeof(Sprite));
            }
            else
            {
                asset = _assetBundle.LoadAsset(assetName);
            }

            callback(asset);

            return asset != null;
        }
    }

    void OnRequestDone(AssetBundleRequest req, object userData)
    {
        assetBundleRequests.Remove(req);
        Action<UnityEngine.Object> callback = userData as Action<UnityEngine.Object>;
        callback(req.asset);
    }

    void Download()
    {
        if(!string.IsNullOrEmpty(_bundleName))
        {
            var list = new List<string> { _bundleName };
            var deps = BundleDependence.Instance.GetBundleDependence(_bundleName);
            if (deps != null)
            {
                list.AddRange(deps.dependences);
                AssetBundleSynchro.Instance.SyncAssetBundles(list, null);
            }
        }
    }

    public void Load(bool loadDepence = true)
    {
        if (_state == AssetBundleLoadingState.none)
        {
            if (_bundleType == AssetBundleLoadingType.dependence && loadDepence)
            {
                var dependenceIt = _dependenceData.dependences.GetEnumerator();
                while (dependenceIt.MoveNext())
                {
                    var bundleCache = _manager.GetAssetBundleCache(dependenceIt.Current);
                    bundleCache.Load(false);
                    if (bundleCache.LoadingState != AssetBundleLoadingState.complete)
                    {
                        _allDependenceLoaded = false;
                    }
                }

                dependenceIt.Dispose();
            }

            if (AssetBundleSynchro.Instance.IsDecompressing(_bundleName))
            {
                NLogger.Error("bundle {0} is decompressing,error happened",_bundleName);
                return;
            }

            string fullPath = AssetManager.Instance.AssetPathProvider.GetBundlePath(_bundleName);
            
            #if ASSET_DEBUG
            NLogger.LogChannel("Asset", $"Load Bundle: {_bundleName} fullPath {fullPath}");
            #endif

            if (!string.IsNullOrEmpty(fullPath))
            {
                try
                {
                    _assetBundle = AssetBundle.LoadFromFile(fullPath);
                    if (_assetBundle == null)
                    {
                        var bundles = AssetBundle.GetAllLoadedAssetBundles();
                        foreach (var bundle in bundles)
                        {
                            if (_bundleNameNoHash == bundle.name)
                            {
                                bundle.Unload(false);
                                _assetBundle = AssetBundle.LoadFromFile(fullPath);
                                break;
                                //    NLogger.ErrorChannel("Assetbundle", "asset bundle {0} already loaded remove and destroy it", _bundleName);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    NLogger.ErrorChannel("Assetbundle", "asset bundle {0} load error message {1}", _bundleName,
                        e.Message);
                }

                if (_assetBundle != null)
                {
                    _state = AssetBundleLoadingState.complete;
                }
                else
                {
                    string bundlePath = AssetBundleSynchro.Instance.GetBundleLocalPath(_bundleName);
                    AssetManager.Instance.IOtool.DeleteGameAsset(bundlePath);
                    NLogger.ErrorChannel("Assetbundle", "asset bundle full path {0} create failed will delete damaged",
                        fullPath);
                    AssetManager.Instance.ForceResetAllCache = true;
                }
            }
            else
            {
                NLogger.ErrorChannel("Assetbundle", "asset bundle {0} file not exist", _bundleName);
                if(AssetManager.Instance.OptimizeError)
                {
                    var msg = $"[AssetError] Sync load bundle {_bundleName} faild ,not exist";
                    //Firebase.Crashlytics.Crashlytics.LogException(new Exception(msg));
                    Download();
                }
                else
                {
                    AssetManager.Instance.ForceResetAllCache = true;
                }                              
            }
        }
        //如果自身加载完毕，还需要检查依赖项是否完全加载完毕
        else if (_state == AssetBundleLoadingState.complete)
        {
            if (_bundleType == AssetBundleLoadingType.dependence && loadDepence && !_allDependenceLoaded)
            {
                var tempFlag = true;
                var dependenceIt = _dependenceData.dependences.GetEnumerator();
                while (dependenceIt.MoveNext())
                {
                    var bundleCache = _manager.GetAssetBundleCache(dependenceIt.Current);
                    bundleCache.Load(false);
                    if (bundleCache.LoadingState != AssetBundleLoadingState.complete)
                    {
                        _allDependenceLoaded = false;
                        tempFlag = false;
                    }
                }

                
                dependenceIt.Dispose();
                _allDependenceLoaded = tempFlag;
            }
        }
        else if (AssetBundleLoader.loadAssetBundleAsync && _state == AssetBundleLoadingState.loading)
        {
            if (_bundleType == AssetBundleLoadingType.dependence && loadDepence)
            {
                var dependenceIt = _dependenceData.dependences.GetEnumerator();
                while (dependenceIt.MoveNext())
                {
                    var bundleCache = _manager.GetAssetBundleCache(dependenceIt.Current);
                    bundleCache.Load(false);
                }
                dependenceIt.Dispose();
                _allDependenceLoaded = true;
            }
            _assetBundle = _req.assetBundle;
            if (_assetBundle != null)
                _state = AssetBundleLoadingState.complete;
            else
            {
                var bundlePath = AssetBundleSynchro.Instance.GetBundleLocalPath(_bundleName);
                AssetManager.Instance.IOtool.DeleteGameAsset(bundlePath);
                var fullPath = AssetManager.Instance.AssetPathProvider.GetBundlePath(_bundleName);
                NLogger.ErrorChannel("Assetbundle", "asset bundle full path {0} create failed will delete damaged", fullPath);
                AssetManager.Instance.ForceResetAllCache = true;
            }
        }
    }

    public T[] LoadAllAssets<T>() where T:Object
    {
        if (_assetBundle)
        {
            return _assetBundle.LoadAllAssets<T>();
        }
        return default;
    }

    public bool Load(Action<bool> callback, bool loadDepence = true)
    {
        var success = false;
        _loadDependence = loadDepence;
        //如果自己已经完成了
        if (_state == AssetBundleLoadingState.complete)
        {
            //如果加载依赖，需要check依赖的bundle
            if (loadDepence && _bundleType == AssetBundleLoadingType.dependence)
            {
                _completeEvent += callback;
                var dependenceIt = _dependenceData.dependences.GetEnumerator();
                while (dependenceIt.MoveNext())
                {
                    _manager.GetAssetBundleCache(dependenceIt.Current).Load(OnDependenceComplete, false);
                }

                dependenceIt.Dispose();
            }
            else
            {
                callback(true);
            }

            success = true;
        }
        //如果没有开始加载，开始加载，触发
        else if (_state == AssetBundleLoadingState.none)
        {
            _state = AssetBundleLoadingState.loading;
            _completeEvent += callback;
            string fullPath = AssetManager.Instance.AssetPathProvider.GetBundlePath(_bundleName);
            if (_bundleName.StartsWith("static+"))
            {
                try
                {
                    _assetBundle = AssetBundle.LoadFromFile(fullPath);
                    if (_assetBundle == null)
                    {
                        var bundles = AssetBundle.GetAllLoadedAssetBundles();
                        foreach (var bundle in bundles)
                        {
                            if (bundle.name == _bundleName)
                            {
                                _assetBundle = bundle;
                                break;
                            }
                        }
                    }
                    if (_assetBundle == null)
                    {
                        NLogger.Error(fullPath + " LoadFromFile fail");
                    }
                }
                catch (Exception e)
                {
                    NLogger.ErrorChannel("Assetbundle", "asset bundle {0} load error message {1}", _bundleName,
                        e.Message);
                }

                OnSelfCompleteSync(_assetBundle, null);
                success = true;
            }
            else
            {
                _req = AssetBundle.LoadFromFileAsync(fullPath);
                if (_req == null)
                {
                    NLogger.Warn("asset bundle {0} load failed", _bundleName);
                }

                AsyncRequestManager.Instance.AddTask(
                    AsyncRequest<AssetBundleCreateRequest>.Create(_req, OnSelfComplete, null));
                success = true;
            }

            //加载依赖
            if (_bundleType == AssetBundleLoadingType.dependence && loadDepence)
            {
                var dependenceIt = _dependenceData.dependences.GetEnumerator();
                while (dependenceIt.MoveNext())
                {
                    _manager.GetAssetBundleCache(dependenceIt.Current).Load(OnDependenceComplete, false);
                }

                dependenceIt.Dispose();
            }
        }
        else if (_state == AssetBundleLoadingState.loading)
        {
            _completeEvent += callback;
            success = true;
        }

        return success;
    }

    public void UnLoadFalse()
    {
        if (_assetBundle != null)
        {
            _assetBundle.Unload(false);
            _assetBundle = null;
        }
    }

    public void UnLoadTrue()
    {
        if (_assetBundle != null)
        {
#if UNITY_DEBUG
            NLogger.LogChannel("AssetManager",$"unload bundle {_assetBundle.name}");
#endif
            _assetBundle.Unload(true);
            _assetBundle = null;
        }
    }

    //当自己完成当时候，如果是依赖加载，检测被依赖的bundle，如果不是，直接回调
    void OnSelfComplete(AssetBundleCreateRequest request, object userData)
    {
        _assetBundle = request.assetBundle;

        _state = AssetBundleLoadingState.complete;

        if (_loadDependence)
        {
            CheckAllComplete();
        }
        else
        {
            if (_completeEvent != null)
            {
                _completeEvent(_assetBundle != null);
                _completeEvent = null;
            }
        }
    }

    void OnSelfCompleteSync(AssetBundle bundle, object userData)
    {
        _assetBundle = bundle;
        _state = AssetBundleLoadingState.complete;

        if (_loadDependence && _bundleType == AssetBundleLoadingType.dependence)
        {
            CheckAllComplete();
        }
        else
        {
            if (_completeEvent != null)
            {
                _completeEvent(_assetBundle != null);
                _completeEvent = null;
            }
        }
    }

    void OnDependenceComplete(bool success)
    {
        CheckAllComplete();
    }

    public List<string> Reasons
    {
        get { return _allReasons; }
    }

    void CheckAllComplete()
    {
        //自己先完成了
        if (_assetBundle != null)
        {
            bool hasNotCompleteCache = false;
            var dependenceIt = _dependenceData.dependences.GetEnumerator();
            while (dependenceIt.MoveNext())
            {
                AssetBundleCache cache = _manager.GetAssetBundleCache(dependenceIt.Current);
                if (cache._state != AssetBundleLoadingState.complete)
                {
                    hasNotCompleteCache = true;
                }
            }

            dependenceIt.Dispose();
            if (!hasNotCompleteCache)
            {
                _state = AssetBundleLoadingState.complete;
                if (_completeEvent != null)
                {
                    _completeEvent(true);
                    _completeEvent = null;
                }
            }
        }
    }
}
