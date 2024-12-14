using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace NexgenDragon
{
    /// <summary>
    /// Asset's interface
    /// 1.Hide the differece between *Resource.Load* and *AssetBundle*
    /// 2.Automatic load dependence. 
    /// </summary>
    /// 
    /// 后续考虑加入pool
    /// 需要缓存callback的环境是 1.可以cancel 2.callback不能多次传入调用，例如assetbundle的加载，
    /// cancel的情况在最外层进行了处理，不会出现一个资源多次调用的情况，在callback返回前是可以cancel的
    /// assetbundle的情况是通过assetbundle的cache逻辑，当加载中的时候缓存callback，加载完毕立即调用callback
    /// 内部逻辑是不会cancel的，只要处理好引用计数即可，所以可以使用lamda表达式
    /// 防止多次调用assetbundle的加载
    public class AssetHandleWrap
    {
        public bool needRemove = false;
        public AssetHandle assetHandle;
        public AssetHandleWrap(AssetHandle handle)
        {
            assetHandle = handle;
        }
    }

    public class AssetHandleList
    {
        public List<AssetHandleWrap> handleList = new List<AssetHandleWrap>(8);
    }
    
    public static class AssetManagerDebugHelper
    {
		private static bool _logLoadAsError = false;
	    public static bool LogLoadAsError
	    {
		    get => _logLoadAsError;
		    set => _logLoadAsError = value;
	    }
	    
    }
        
	public class AssetManager : Singleton<AssetManager>, IManager, ISecondTicker
    {
		private Dictionary<string,AssetHandleList> allAssetRequest = new Dictionary<string,AssetHandleList>();
        private AssetCacheManager _assetCacheManager = new AssetCacheManager();
        private AssetManagerHelper _assetHelper = null;
	    private IDataParser _dataParser;
	    private IAssetPathProvider _assetPathProvider;
	    private IEnvironmentVariable _environmentVariable;
	    private IIOtool _IOtool;
	    private IDownLoader _downLoader;
	    private BaseAssetDatabaseLoader _assetDatabaseLoader;
	    private bool _isLowMemoryDevice = true;

	    private AssetManagerConfig _config = null;

	    public bool ForceResetAllCache
	    {
		    get
		    {
#if UNITY_EDITOR && !USE_BUNDLE_IOS && !USE_BUNDLE_ANDROID && !USE_BUNDLE_STANDALONE
			    return false;
#endif
			    var reset = PlayerPrefs.GetInt("ForceResetAllCache", 0);
			    return reset == 1;
		    }
		    set
		    {
			    PlayerPrefs.SetInt("ForceResetAllCache", value ? 1 : 0);
		    }
	    }

		private bool _canClearAllBundle = false;

		private bool _optimizeError = false;

		public bool CanClearAllBundle
		{
			get { return _canClearAllBundle; }
			set { _canClearAllBundle = value; }
		}

        public bool OptimizeError
        {
            get { return _optimizeError; }
            set { _optimizeError = value; }
        }

        public static string GetCacheIndex(string originPath)
	    {
		    //加载逻辑内部都使用小写       
		    //这里的path已经转换为小写，所以后续不需要再次转换
			var IOtool = AssetManager.Instance.IOtool;
			if (IOtool == null)
			{
			    NLogger.Error("AssetManager GetCacheIndex IOtool is null");
				return String.Empty;
			}
			string path = IOtool.GetLocalizedAssetName(originPath);
			if (path != null)
		    	path = path.ToLower();

		    if (originPath.ToLower() != path)
		    {
			    NLogger.LogChannel("LocalizedAssetName", string.Format("originPath : {0} --------- localizedPath: {1}", originPath, path));    
		    }  

		    var cacheIndex = String.Empty;
		    try
		    {
			    //cache index 可以理解为资源的唯一标示，我们认为本地化资源与原来资源是不同的资源，所以使用不同的标示
			    //所以cache index 从转换后的path中获取，其实就是file name
			    //资源管理内部都是基于 cache index处理，path主要是负责打log等辅助操作
			    //所以保证资源id的唯一性很重要
			    cacheIndex = Path.GetFileName(path);
		    }
		    catch (Exception e)
		    {
			    NLogger.Error("get file name error {0}",originPath);
			    throw;
		    }

		    return cacheIndex;
	    }
	    
        public AssetHandle LoadAsset (string originPath,bool isSprite = false)
        { 	       
	        TimeUtils.StartRecord();

	        var cacheIndex = GetCacheIndex(originPath); 
			if (EnvironmentVariable == null){
			    NLogger.Error("AssetManager LoadAsset EnvironmentVariable is null");
			}
	        else if (EnvironmentVariable.UNITY_EDITOR())
	        {
		        BundleCollector.Collect(cacheIndex, originPath);
	        }
			else if(AssetManagerDebugHelper.LogLoadAsError)
			{
				NLogger.Error($"[ASSET-UPDATE-DEBUG][AssetManager.LoadAsset]{originPath},{isSprite}");

			}
#if (MO_LOG || UNITY_DEBUG || MO_DEBUG) && !UNITY_EDITOR
			AssetCollector.Collect(cacheIndex, originPath);
#endif
			if (cacheIndex == null)
			{
				throw new ArgumentNullException ("path");
			}

            var cache = _assetCacheManager.LoadAssetCache(cacheIndex,isSprite);
            var handle = new AssetHandle();
            handle.SetAssetCache(cache);
            handle.cacheIndex = cacheIndex;
            handle.path = originPath;

			if(!handle.asset)
            {
                NLogger.Warn ("[AssetManager]LoadAsset: Failed to sync load {0}.", cacheIndex);
            }
            
//	        TimeUtils.UpdateRecord("LoadAsset {0} (sync)", path);
			return handle;
        }

        public UnityEngine.Object DirectlyLoadAsset(string originPath, bool isSprite = false)
        {
	        var cacheIndex = GetCacheIndex(originPath);
	        var assetData = BundleAssetDataManager.Instance.GetAssetData(cacheIndex);
	        if (assetData != null)
	        { //try ab
		        AssetCache.AssetLoaderManager.Instance.BundleLoader.OnAssetCacheCreate(assetData);
		        
		        var  unityAsset = AssetCache.AssetLoaderManager.Instance.BundleLoader.LoadAsset(assetData, isSprite);
		        if (unityAsset == null)
		        {
			        // 使用bundle的模式，才用这个加载
			        unityAsset = AssetCache.AssetLoaderManager.Instance.LoadingAssetLoader.LoadAsset(assetData, isSprite);
		        }

		        return unityAsset;
	        }
	        
	        assetData = new BundleAssetData(cacheIndex, String.Empty);
	        NLogger.Log("Lua Loader load {0} from resources",assetData.AssetName);
	        return AssetCache.AssetLoaderManager.Instance.ResourcesLoader.LoadAsset(assetData, isSprite);
        }

		private bool IsHandleListValid(List<AssetHandleWrap> list)
		{
			if (list != null && list.Count > 0)
			{
				foreach (var data in list)
				{
					if (data.needRemove == false)
					{
						return true;
					}
				}
			}
			return false;
		}

		public AssetHandle LoadAsset (string originPath, Action<bool, AssetHandle> callback,bool isSprite = false, int priority = 999, bool asap = false)
        {	        	        
	        TimeUtils.StartRecord();
	        
	        //加载逻辑内部都使用小写       
	        //这里的path已经转换为小写，所以后续不需要再次转换
	        var path = AssetManager.Instance.IOtool.GetLocalizedAssetName(originPath).ToLower();

	        if (originPath.ToLower() != path)
	        {
		        NLogger.LogChannel("LocalizedAssetName", string.Format("originPath : {0} --------- localizedPath: {1}", originPath, path));    
	        }  
	        
	        var cacheIndex = String.Empty;

	        try
	        {
		        //cache index 可以理解为资源的唯一标示，我们认为本地化资源与原来资源是不同的资源，所以使用不同的标示
		        //所以cache index 从转换后的path中获取，其实就是file name
		        //资源管理内部都是基于 cache index处理，path主要是负责打log等辅助操作
		        //所以保证资源id的唯一性很重要
		        cacheIndex = Path.GetFileName(path);
	        }
	        catch (Exception e)
	        {
		        NLogger.Error("get file name error {0}",originPath);
		        throw;
	        }
            
	        
	        if (EnvironmentVariable.UNITY_EDITOR())
	        {
		        BundleCollector.Collect(cacheIndex, originPath);
	        }
	        else if(AssetManagerDebugHelper.LogLoadAsError)
	        {
		        NLogger.Error($"[ASSET-UPDATE-DEBUG][AssetManager.LoadAssetCallback]{originPath},{isSprite},{priority},{asap}");
	        }
#if (MO_LOG || UNITY_DEBUG || MO_DEBUG) && !UNITY_EDITOR
			AssetCollector.Collect(cacheIndex, originPath);
#endif
	        if (cacheIndex == null)
			{
				throw new ArgumentNullException ("path");
			}
                                    
			AssetHandle handle = new AssetHandle();
			handle.cacheIndex = cacheIndex;
            handle.path = originPath;
            handle.callback = callback;
            AssetHandleList list = null;
            //这里存在两种情况，一种是资源没有准备好，一种是资源已经准备好
            //逻辑分为先按照准备好的方式查询，速度更快，不用操作请求列表
            AssetCache cache = _assetCacheManager.GetAssetCache(cacheIndex);
            //资源已经准备好
            if (cache != null && cache.CacheState == AssetCache.AssetCacheState.complete)
            {
                handle.SetAssetCache(cache);
                if (callback != null)
                {
                    callback(cache.Asset != null, handle);
                }
//	            TimeUtils.UpdateRecord("LoadAsset {0} end(cache)", path);
                return handle;
            }
            // 如果资源没有准备好，但是存在这个请求，并且请求的个数大于0，说明已经调用过底层的加载函数了，不再调用，加入到回调list即可
            //新修改建立在asset cache一旦被发起创建，么有结果返回之前不会卸载对应的bundle和资源
            if (allAssetRequest.TryGetValue(handle.cacheIndex, out list))
            {
                //大于0说明请求过加载函数
                //以上注释作废，实际情况是，异步加载一个资源是无法在底层取消的，所以只要有监听存在就增加监听即可，不需要考虑已有的创建逻辑完成监听被取消的情况
                //这样才可以保证引用计数的正确性
            //    if (IsHandleListValid(list.handleList))
                {
                    list.handleList.Add(new AssetHandleWrap(handle));
//	                TimeUtils.UpdateRecord("LoadAsset {0} end(request)", path);
                    return handle;
                }
            /*    //不大于0说明之前的加载请求已经取消了
             //注释掉的原因是已经在加载中的资源，不能再次申请加载，会导致引用计数错误
                else
                {
                    list.handleList.Add(new AssetHandleWrap(handle));
                    _assetCacheManager.LoadAssetCache(cacheIndex,OnAssetCacheLoad,isSprite,priority);
//	                TimeUtils.UpdateRecord("LoadAsset {0} end(cancel)", path);
                    return handle;
                }  */
            }
            //最后的情况是，没有可用的
            else
            {
                list = new AssetHandleList();
                list.handleList.Add(new AssetHandleWrap(handle));
                allAssetRequest.Add(handle.cacheIndex, list);
                _assetCacheManager.LoadAssetCache(handle.cacheIndex,OnAssetCacheLoad,isSprite,priority, asap);
//	            TimeUtils.UpdateRecord("LoadAsset {0} end(new)", path);
                return handle;
            }
			
        }

		public bool LoadSceneAsync(string sceneName, LoadSceneMode mode,Action<bool> callback)
		{
			#if UNITY_EDITOR
			return AssetCache.AssetLoaderManager.Instance.AssetDatabaseLoader.LoadSceneAsync(sceneName, mode, callback);
			#endif
			return false;
		}

        public void UnloadAsset (AssetHandle handle)
        {
			if (handle == null)
			{
				return;
			}

            //异步加载的逻辑，需要删除在等待队列中的callback
            if (handle.callback != null)
            {
                AssetHandleList list = null;
                //这里不会理解删除列表，因为可能是在回调函数中unload资源，这样会导致迭代器失效问题
                if (allAssetRequest.TryGetValue(handle.cacheIndex, out list))
                {
                    AssetHandleWrap wrap = list.handleList.Find(x=>x.assetHandle == handle);
                    if (wrap != null)
                    {
                        wrap.needRemove = true;
                    }
                }
                
                handle.callback = null; // 防止闭包引起的内存泄露
            }
            _assetCacheManager.UnloadAsset(handle.cacheIndex);
        }

		public string LoadText(string textPath)
		{
			var handle = LoadAsset (textPath);
			if (handle.asset)
			{
				var txt = handle.asset as TextAsset;
				if (EnvironmentVariable.UNITY_EDITOR())
				{
					if (txt == null)
					{
						NLogger.Error("asset {0} is not text asset",textPath);
					}
				}
				string text = txt.text;
				UnloadAsset (handle);
				return text;
			}
			return string.Empty;
		}

        public Material LoadMaterial(string materialName)
        {
            return CommonMaterialCache.Instance.GetMaterial(materialName);
        }

        public byte[] LoadTextBytes(string textPath)
        {
            var handle = LoadAsset (textPath);
            if (handle.asset)
            {
                var txt = handle.asset as TextAsset;
                if (EnvironmentVariable.UNITY_EDITOR())
                {
	                if (txt == null)
	                {
		                NLogger.Error("asset {0} is not text asset",textPath);
	                }
                }
                var text = txt.bytes;              
                UnloadAsset (handle);
                return text;
            }
            return null;
        }

        public AssetHandle LoadSpriteAsset(string mapName)
        {
            var handle = LoadAsset (mapName);
            return handle;
        }

		public AssetHandle LoadTexture(string texName)
		{
			var handle = LoadAsset (texName);
			return handle;
		}

		public Font LoadFont(string fontName)
		{
			var handle = LoadAsset (fontName);
			NLogger.Warn($"wgy LoadFont {handle}");
			if (handle != null && handle.asset != null)
			{
				return handle.asset as Font;
			}
			return null;
		}

		public List<ShaderVariantCollection> LoadSVCListFromBundleList(List<string> shaderBundleList)
		{
			return AssetCache.AssetLoaderManager.Instance.BundleLoader.LoadSVCFormBundleList(shaderBundleList);
		}

    //    public AssetHandle LoadMaterial(string mapName)
    //    {
    //        var handle = LoadAsset (mapName);
    //        return handle;
    //    }

        public RuntimeAnimatorController LoadAnimationController(string assetName)
        {
            var handle = LoadAsset(assetName);
            return handle.asset as RuntimeAnimatorController;
        }

        public VideoClip LoadVideoClip(string videoName)
        {
	        var handle = LoadAsset(videoName);
	        return handle.asset as VideoClip;
        }

		public void Initialize(NexgenObject configParam)
		{
//			TimeUtils.UpdateRecord("AssetManager Initialize");

			_config = configParam as AssetManagerConfig;
			if (_config != null)
			{
				_dataParser = _config.dataParser;
				_assetPathProvider = _config.assetPathProvider;
				_IOtool = _config.IOtool;
				_environmentVariable = _config.environmentVariable;
				_assetDatabaseLoader = _config.assetbaDatabaseLoader;
				_downLoader = _config.downLoader;
				_isLowMemoryDevice = _config.IsLowMemoryDevice;
			}						
			_assetCacheManager.Init();
            var helper = new GameObject("AssetManager");
            _assetHelper = helper.AddComponent<AssetManagerHelper>();
            GameObject.DontDestroyOnLoad(helper);

        }

//		public void PostInit()
//		{
//			ReloadConfig();
//		}

//		public void ReloadConfig()
//		{			
//			if (EnvironmentVariable.UNITY_EDITOR() && !EnvironmentVariable.USE_BUNDLE_IOS() &&
//			    !EnvironmentVariable.USE_BUNDLE_ANDROID() && !EnvironmentVariable.USE_BUNDLE_STANDALONE())
//			{
//				return;				
//			}
//
//			BundleAssetDataManager.Instance.Init();
//			
//			BundleDependence.Instance.Init();
//		
		
		public bool ReloadRConfig()
		{			
			NLogger.LogChannel("Asset", "--------------------- ReloadRConfig -----------");
			
			if (EnvironmentVariable.UNITY_EDITOR() && !EnvironmentVariable.USE_BUNDLE_IOS() &&
			    !EnvironmentVariable.USE_BUNDLE_ANDROID() && !EnvironmentVariable.USE_BUNDLE_STANDALONE())
			{
				return true;				
			}
			/*
			if (EnvironmentVariable.USE_SBP())
			{
				return BundleAssetDataManager.Instance.LoadBundleAssetByType("R") && BundleDependence.Instance.LoadBundleDependenceByType("R") && BundleAssetDataManager.Instance.LoadAssetPathByType("R");	
			}
			*/
			return BundleAssetDataManager.Instance.LoadBundleAssetByType("R") &&
			       BundleDependence.Instance.LoadBundleDependenceByType("R");
		}
		
		public bool ReloadGConfig()
		{	
			NLogger.LogChannel("Asset", "--------------------- ReloadGConfig -----------");
			
			if (EnvironmentVariable.UNITY_EDITOR() && !EnvironmentVariable.USE_BUNDLE_IOS() &&
			    !EnvironmentVariable.USE_BUNDLE_ANDROID() && !EnvironmentVariable.USE_BUNDLE_STANDALONE())
			{
				return true;				
			}
			/*
			if (EnvironmentVariable.USE_SBP())
			{
				return BundleAssetDataManager.Instance.LoadBundleAssetByType("G") && BundleDependence.Instance.LoadBundleDependenceByType("G") && BundleAssetDataManager.Instance.LoadAssetPathByType("G");
			}
			*/
			return BundleAssetDataManager.Instance.LoadBundleAssetByType("G") &&
			       BundleDependence.Instance.LoadBundleDependenceByType("G");
		}
		
		

		public void Reset()
		{
			AssetManagerEvent.InvokeBeforeClear();
            AssetCache.AssetLoaderManager.Instance.Reset();
            if (_canClearAllBundle)
            {
	            GameObjectCreateHelper.Clear();
	            allAssetRequest.Clear();
	            CommonMaterialCache.Instance.Clear();
	            _assetCacheManager.ClearAllNotProcessBundle();
	            AssetCache.AssetLoaderManager.Instance.Clear();    
            }
		}

		//在更新完bundle调用
		public void Clear()
		{
			AssetManagerEvent.InvokeBeforeClear();
			UnloadUnused();            
			AssetManagerEvent.InvokeAfterClear();
		}
        

        public void UnloadUnused()
        {
	        _assetHelper.NeedClear = true;
	        //    GC.Collect();
        //    Resources.UnloadUnusedAssets();            
        }

        //这个函数直接调用很有可能资源卸载失败，用UnloadUnused
        public void UnloadUnusedImmediate()
        {
	        AssetManagerEvent.InovkeBeforeUnloadUnused();
	        _assetCacheManager.ClearUnused();
        }

		public override void Release()
		{
//			TimeUtils.UpdateRecord("AssetManager Release");
			_assetCacheManager.Release();
		}
            
		public void Tick(float delta)
		{
			
		}
        //资源真正准备好后回调，如果发生unload，列表会减少对应回调，不会影响对应的逻辑
        //通过这里将内部的callback与外部请求callback进行解耦，当unload发生的时候，内部逻辑不需要考虑callback的操作，降低内部逻辑复杂度
        private void OnAssetCacheLoad(AssetCache cache)
        {
            AssetHandleList list = null;
            if (allAssetRequest.TryGetValue(cache.Data.AssetName, out list))
            {
                var handleIt = list.handleList.GetEnumerator();
                while (handleIt.MoveNext())
                {
                    if (handleIt.Current.needRemove == false && handleIt.Current.assetHandle.callback != null)
                    {
                        handleIt.Current.assetHandle.SetAssetCache(cache);
                        //在迭代中调用外部回调，需要做好exception处理，防止连续爆红
                        try
                        {
                            handleIt.Current.assetHandle.callback(cache.Asset != null, handleIt.Current.assetHandle);
                            handleIt.Current.assetHandle.callback = null; // 防止闭包引起的内存泄露
                        }
                        catch(Exception e)
                        {
	                        Debug.LogException(e);
                        }
                    }
                }
                handleIt.Dispose();
                //资源回调产生，说明资源加载完毕，短时间不会发生异步加载了，删除对应的回调列表
                allAssetRequest.Remove(cache.Data.AssetName);
            }
        }

        public IDataParser DataParser
        {
            get { return _dataParser; }
        }

        public IAssetPathProvider AssetPathProvider
        {
            get { return _assetPathProvider; }
        }

        public IEnvironmentVariable EnvironmentVariable
        {
            get { return _environmentVariable; }
        }

        public IIOtool IOtool
        {
            get { return _IOtool; }
        }

        public IDownLoader DownLoader
        {
            get { return _downLoader; }
        }

        public BaseAssetDatabaseLoader AssetDatabaseLoader
        {
            get { return _assetDatabaseLoader; }
        }

	    public List<AssetCacheRefInfo> DumpAssetRefCount()
        {
            return _assetCacheManager.DumpAssetRefCount();
        }

	    public void HandleLowMemory()
	    {
		    UnloadUnused();
	    }

	    public bool IsLowMemoryDevice
	    {
		    get { return _isLowMemoryDevice; }
	    }
    }
}
