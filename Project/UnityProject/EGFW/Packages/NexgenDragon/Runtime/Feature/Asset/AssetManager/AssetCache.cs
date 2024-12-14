//#define ASSET_DEBUG
using System.Collections.Generic;
using UnityEngine;
using System;

namespace NexgenDragon
{
    public class AssetCache:AssetReferenceCountBase
    {
        public enum AssetCacheState
        {
            loading,
            complete,
        }
        public enum AssetLoadType
        {
            //没有资源对应，空的
            none,
            bundle,
            resources,
        }
        public readonly float AssetTimeOut = 10f;
        //这个类存在就是防止cache里每个cache都保存两个loader的实例，也不想loader被外界随便使用，那样会导致引用计数错误
        
        private List<string> _checkResult = new List<string>(2);
        
        public class AssetLoaderManager
        {       
            static AssetLoaderManager _Instance = null;

#if UNITY_EDITOR
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void ReLoadInit()
            {
                _Instance = null;
            }
#endif
            static public AssetLoaderManager Instance
            {
                get
                {
                    if (_Instance == null)
                    {
                        _Instance = new AssetLoaderManager();
                        _Instance.Init();
                    }
                    return _Instance;
                }
            }

            static public void DestroyInstance()
            {
                _Instance = null;
            }

            private AssetBundleLoader _bundleLoader;
            private ResourcesLoader _resourcesLoader;

            // 使用bundle时，才使用这个加载方法
            private LoadingDataLoader _loadingAssetLoader;
            public void Init()
            {
                _bundleLoader = new AssetBundleLoader();
                _bundleLoader.Init();
                _resourcesLoader = new ResourcesLoader();
                _resourcesLoader.Init();

                // 使用bundle时，才使用这个加载方法
                if (UsingBundle)
                {
                    _loadingAssetLoader = new LoadingDataLoader();
                    _loadingAssetLoader.Init();    
                }

                AssetDatabaseLoader.Init();
            }

            public void Reset()
            {
                _bundleLoader.Reset();
                _resourcesLoader.Reset();

                // 使用bundle时，才使用这个加载方法
                if (UsingBundle)
                {
                    _loadingAssetLoader.Reset();
                }
                
                AssetDatabaseLoader.Reset();
            }

            public void Clear()
            {
                BundleLoader.Clear();
                ResourcesLoader.Clear();
                AssetDatabaseLoader.Clear();
            }

            public AssetBundleLoader BundleLoader
            {
                get{ return _bundleLoader;}
            }

            public ResourcesLoader ResourcesLoader
            {
                get{ return _resourcesLoader;}
            }

            public LoadingDataLoader LoadingAssetLoader => _loadingAssetLoader;

            public BaseAssetDatabaseLoader AssetDatabaseLoader
            {
                get { return AssetManager.Instance.AssetDatabaseLoader; }
            }
        }
        private UnityEngine.Object _unityAsset = null;
        private BundleAssetData _assetData = null;
        private AssetCacheState _state = AssetCacheState.loading;
        //默认是从bundle加载的
        private AssetLoadType _loadType = AssetLoadType.bundle;
        // Use this for initialization
        public UnityEngine.Object Asset
        {
            get
            {
                _unityAsset = FixPrefabShader(_unityAsset);
                return _unityAsset;
            }
        }

        private bool _isSprite = false;
        public bool IsSprite
        {
            get { return _isSprite; }
            set { _isSprite = value; }
        }

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private UnityEngine.Object FixPrefabShader(UnityEngine.Object asset)
        {
            if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR() && UsingBundle)
            {
                var go = asset as GameObject;
                if (go)
                {
                    go.GetComponentsInChildren(true, _renderers);
                    foreach (var renderer in _renderers)
                    {
                        var sharedMaterials = renderer.sharedMaterials;
                        foreach (var material in sharedMaterials)
                        {
                            if (material)
                            {
                                var shader = material.shader;
                                if (shader)
                                {
                                    var editorShader = Shader.Find(shader.name);
                                    if (editorShader)
                                    {
                                        material.hideFlags = HideFlags.DontSave;
                                        material.shader = editorShader;
                                    }
                                    else
                                    {
                                        NLogger.Error("Failed to fix shader for editor: {0}", shader.name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return asset;
        }

        private static bool UsingBundle
        {
            get
            {
                // 真机必然是bundle
                if (!AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                {
                    return true;
                }
                
                return AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_IOS() ||
                       AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_ANDROID() ||
                       AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_STANDALONE();
            }
        }

        public BundleAssetData Data
        {
            get{ return _assetData;}
        }

        public AssetCacheState CacheState
        {
            get{return _state;}
        }
        public AssetCache(string assetPath)
        {                        
            _loadType = AssetLoadType.bundle;

#if ASSET_DEBUG
            NLogger.LogChannel("Asset", string.Format("begin create AssetCache {0}", assetPath));
#endif      
            if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR() && !AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_IOS() &&
                !AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_ANDROID() && !AssetManager.Instance.EnvironmentVariable.USE_BUNDLE_STANDALONE())
            {                               
                _loadType = AssetLoadType.resources;
            }                              

            if(_loadType == AssetLoadType.bundle)
            {
                _assetData = BundleAssetDataManager.Instance.GetAssetData(assetPath);

                //说明bundle中不存在，直接用resources加载
                if (_assetData != null)
                {                                        
                    //bundle一定会创建的，只是可能会是空bundle没有资源
                    AssetLoaderManager.Instance.BundleLoader.OnAssetCacheCreate(_assetData);
                }
                else
                {
                    //当使用bundle模式加载，但是bundle中没有，resources中也没有的时候，
                    //说明发生了加载bundle中的资源但是bundle数据还没有准备好的情况，会引起错误
                    if (!BundleAssetDataManager.Instance.DataAllInit())
                    {
                        bool resourcesContains = AssetLoaderManager.Instance.ResourcesLoader.ContainsAsset(assetPath);
                        if (!resourcesContains)
                        {
                            NLogger.Error("asset {0} can not be loaded before asset bundle all init,maybe r or maybe g",assetPath);
                        }
                    }
                    
                    #if ASSET_DEBUG
                    NLogger.Error("asset {0} use resource mode to load",assetPath);
                    #endif
                    _loadType = AssetLoadType.resources;
                }
            }

            if(_loadType == AssetLoadType.resources)  //资源cache创建的时候，增加对引用的bundle的计数
            {
                _assetData = new BundleAssetData(assetPath, string.Empty);
            }

            #if ASSET_DEBUG
            NLogger.LogChannel("Asset", string.Format($"Create AssetCache end path:{assetPath} tyep: {_loadType}"));
            #endif  
        } 

        public void Load()
        {
            if (_state == AssetCacheState.complete)
            {
                return;
            }
            else
            {
                if (_loadType == AssetLoadType.bundle)
                {
                    _unityAsset = AssetLoaderManager.Instance.BundleLoader.LoadAsset(_assetData,_isSprite);
#if(ASSET_DEBUG)
                    NLogger.Trace("Load asset with bundle mode. Path = {0}, IsSprite = {1}",
                        _assetData.AssetPath, _isSprite);
#endif
                }
                else if (_loadType == AssetLoadType.resources)
                {
                    // 使用bundle的模式，才用这个加载
                    if (UsingBundle)
                    {
                        _unityAsset = AssetLoaderManager.Instance.LoadingAssetLoader.LoadAsset(_assetData, _isSprite);
                    }
                    
                    //编辑器下用assetdatabase，运行时用resources
                    if (!_unityAsset && AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                    {                        
                        if (AssetLoaderManager.Instance.AssetDatabaseLoader != null)
                        {                            
                            _unityAsset = AssetLoaderManager.Instance.AssetDatabaseLoader.LoadAsset(_assetData,_isSprite);    
                        }                                                                                                
#if(ASSET_DEBUG)
                        NLogger.Trace("Load asset with database mode. Path = {0}, IsSprite = {1}", 
                            _assetData.AssetPath, _isSprite);
#endif
                    }
				
                    if(!_unityAsset)
                    {
                        _unityAsset = AssetLoaderManager.Instance.ResourcesLoader.LoadAsset(_assetData,_isSprite);

                        if (!_unityAsset)
                        {                
                            #if UNITY_EDITOR
                            NLogger.Warn(_assetData.AssetName + " 在本地资源路径中未找到，请尝试重新生成（执行 NexgenDragon/Resource/Resource Path Generator）");
                            #endif
                        }
                    }

#if(ASSET_DEBUG)
                    NLogger.Trace("Load asset with resource mode. Path = {0}, IsSprite = {1}", 
                        _assetData.AssetPath, _isSprite);
#endif
                }
                _state = AssetCacheState.complete;
                if (_unityAsset == null)
                {
                    _state = AssetCacheState.loading;
                }
            }
        }

        public void Load(Action<bool> callback, int priority = 999, bool asap = false)
        {
            //如果资源加载完毕，不会重复调用底层加载
            if (_state == AssetCacheState.complete)
            {
                callback(true);
                return;
            }
            //此处使用lambda表达式是安全的，在内部逻辑返回之前，也就是state为complete之前，
            //这个对象是不会发生析构的
            if (_loadType == AssetLoadType.bundle)
            {
                List<string> allBundleNeedSync = new List<string>();

                var dependenceData = BundleDependence.Instance.GetBundleDependence(_assetData.BundleName);
                if(dependenceData != null)
                {
                    allBundleNeedSync.AddRange(dependenceData.dependences);
                }
                allBundleNeedSync.Add(_assetData.BundleName);
                                                
                AssetBundleSynchro.Instance.SyncAssetBundles(
                    allBundleNeedSync,
                    delegate {
                        AssetLoaderManager.Instance.BundleLoader.LoadAsset(_assetData,(loadedObject) =>
                        {
                            _unityAsset = loadedObject;
                            //不管成功与否都是complete
                            if(_unityAsset != null)
                            {
                                _state = AssetCacheState.complete;
                            }	
                            if(loadedObject != null)
                            {
                                callback(true);
                            }
                            else
                            {
                                NLogger.Error("asset {0} load failed from bundle {1}",_assetData.AssetName, _assetData.BundleName);
                                callback(false);
                            }
                        },_isSprite, asap);
                    }
                ,null, priority,asap);
           
            }
            else if(_loadType == AssetLoadType.resources)
            {
                //编辑器下用assetdatabase，运行时用resources
                if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                {
                    var success = AssetLoaderManager.Instance.AssetDatabaseLoader.LoadAsset(_assetData,(loadedObject) =>
                    {
                        _unityAsset = loadedObject;
                        if(loadedObject != null)
                        {
                            _state = AssetCacheState.complete;
                            callback(true);
                        }
                    },_isSprite, asap);
                    if(!success)
                    {
                        AssetLoaderManager.Instance.ResourcesLoader.LoadAsset(_assetData,(loadedObject) =>
                        {
                            _unityAsset = loadedObject;
                            //不管成功与否都是complete
                            _state = AssetCacheState.complete;
                            if(loadedObject != null)
                            {
                                callback(true);
                            }
                            else
                            {
#if UNITY_EDITOR
                                NLogger.Warn(_assetData.AssetName + " 在本地资源路径中未找到，请尝试重新生成（执行 NexgenDragon/Resource/Resource Path Generator）");
#endif
                                callback(false);
                            }
                        },_isSprite, asap);
                    }
                }
                else
                {
                    AssetLoaderManager.Instance.ResourcesLoader.LoadAsset(_assetData,(loadedObject) =>
                    {
                        _unityAsset = loadedObject;
                        //不管成功与否都是complete
                        _state = AssetCacheState.complete;
                        if(loadedObject != null)
                        {
                            callback(true);
                        }
                        else
                        {
                            NLogger.Error("---------------- Asset {0} load failed --------------", _assetData.AssetName);
                            callback(false);
                        }
                    }, _isSprite, asap);
                }
            }
        }

        public override void Release ()
        {
            //unload asset
            DestroyAsset();
            //资源销毁减少assetbundle的引用
            if (_loadType == AssetLoadType.bundle)
            {
                AssetLoaderManager.Instance.BundleLoader.OnAssetCacheRelease(Data);
            }
        }

        public void ReleaseNotProcessBundle()
        {
            DestroyAsset();
        }

        public bool CheckRemove()
        {
            //加载中的资源不能删除，使用lambda表达式不能删除
            if (_referenceCount == 0 && _state != AssetCacheState.loading)
            {
                if (Time.time - _zeroTime > AssetTimeOut)
                {
                    return true;
                }
            }
            return false;
        }
        
        public bool CanRemove()
        {
            //加载中的资源不能删除，使用lambda表达式不能删除
            if (_referenceCount <= 0 && _state != AssetCacheState.loading)
            {
                return true; 
            }
            return false;
        }

        private void DestroyAsset()
        {
            if (_unityAsset)
            {
                //如果是无法立即卸载的资源，不能用unload asset
                if(!CanUnloadImmediate())
                {
                    _unityAsset = null;
                    //assetbundle里的prefab不能destroy，如果bundle还在，prefab被干掉了，再次加载会返回空
                    //GameObject.DestroyImmediate(_unityAsset,true);
                }
                else
                {
                    Resources.UnloadAsset(_unityAsset);
                    _unityAsset = null;
                }
            }
        }

        public bool CanUnloadImmediate()
        {
            var can = (_unityAsset is GameObject || _unityAsset is Component || _unityAsset is Texture || _unityAsset is Sprite || _unityAsset is Material);
            return !can && _unityAsset;
        }
    }   
}