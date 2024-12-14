using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexgenDragon
{
    public class AssetBundleLoader:AssetBaseLoader
    {
        //用来进行bundle异步加载结果查询的类
        private AssetBundleCacheManager _bundleCacheManager = new AssetBundleCacheManager();
        // Use this for initialization
        public override UnityEngine.Object LoadAsset (BundleAssetData data,bool isSprite = false)
        {
            AssetBundleCache cache = _bundleCacheManager.GetAssetBundleCache(data.BundleName);
            if (AssetManager.Instance.EnvironmentVariable.UNITY_DEBUG())
            {
                var bundleType = AssetBundleSynchro.Instance.GetBundleUpdateType(data.BundleName);
                var isStatic = bundleType == "static";
                var depData = BundleDependence.Instance.GetBundleDependence(data.BundleName);
//                bool hasOTA = false;
#if ASSET_DEBUG
                NLogger.LogChannel("Asset", $"Before LoadAsset: bundleType {bundleType} ");
#endif 
                if(isStatic && depData != null)
                {
                    foreach(var bundle in depData.dependences)
                    {
                        var depBundleType = AssetBundleSynchro.Instance.GetBundleUpdateType(bundle);
#if ASSET_DEBUG
                NLogger.LogChannel("Asset", $"Before LoadAsset depData: bundle {bundle} depBundleType {depBundleType}");
#endif                        
                        if(depBundleType == "ota")
                        {
                            // NLogger.Error("when load asset {0} error,bundle {1} depends {2} is a ota bundle",data.AssetPath,data.BundleName,bundle);
                        }
                    }
                }
            }
    
            cache.Load();
            return cache.LoadAsset(data.AssetName,isSprite);
        }
        //callback来自于assetcache对象，对象在回调被调用前不会删除
        public override bool LoadAsset(BundleAssetData data, System.Action<UnityEngine.Object> callback, bool isSprite,
            bool asap = false)
        {            
            AssetBundleCache cache = _bundleCacheManager.GetAssetBundleCache(data.BundleName);
            if (loadAssetBundleAsync)
            {
               // NLogger.LogChannel("loadAssetBundleAsync", $"load bundle use async mode");
             //   NLogger.Error($"load bundle use async mode");
                cache.Load(success =>
                {
                    if (success)
                        cache.LoadAsset(data.AssetName, callback, isSprite, 999, asap);
                    else
                    {
                        NLogger.LogChannel("loadAssetBundleAsync",$"load bundle {data.BundleName} failed async");
                        callback(null);
                    }
                        
                });
                return true;
            }
            else
            {
                //budle由于要支持同步和异步加载两种形式所以异步加载资源的bundle创建也用同步的
                cache.Load();
                return cache.LoadAsset(data.AssetName, callback, isSprite, 999, asap);
            }
        }

        public override bool Init ()
        {
            AssetManagerEvent.AddAfterClearListener(ClearLua);            
            return true;
        }

        public override void OnAssetCacheCreate(BundleAssetData assetData)
        {
            if(!string.IsNullOrEmpty(assetData.BundleName))
            {
                _bundleCacheManager.GetAssetBundleCache(assetData.BundleName).Increase(assetData.AssetName);
            }
        }

        public override void OnAssetCacheRelease(BundleAssetData assetData)
        {
            if (!string.IsNullOrEmpty(assetData.BundleName))
            {
                _bundleCacheManager.GetAssetBundleCache(assetData.BundleName).Decrease(assetData.AssetName);
            }
        }

        public override void Clear()
        {
            //去掉所有的正在加载的request
            AsyncRequestManager.Instance.Clear();
            _bundleCacheManager.UnloadAll();
        }

        public override void ClearScript()
        {
            ClearLua();
        }

        public override bool LoadSceneAsync(string sceneName, LoadSceneMode mode, Action<bool> callback)
        {
            return false;
        }

        public override void Reset()
        {
            
        }

        public override List<ShaderVariantCollection> LoadSVCFormBundleList(List<string> bundleList)
        {
            List<ShaderVariantCollection> list = new List<ShaderVariantCollection>();
            foreach (var bundle in bundleList)
            {
                var bundleCache = _bundleCacheManager.LoadAssetBundleCache(bundle);
                bundleCache.Load(false);
                var svc = bundleCache.LoadAllAssets<ShaderVariantCollection>();
                if (svc != null && svc.Length > 0)
                {
                    foreach (var s in svc)
                    {
                        if (!list.Contains(s))
                        {
                            list.Add(s);
                        }
                    }
                }
            }
            return list;
        }

        void ClearLua()
        {
            _bundleCacheManager.UnloadAllLua();
        }

        public List<AssetCacheRefInfo> DumpBundleInfo()
        {
            return _bundleCacheManager.DumpBundleRefInfo();
        }

        public static bool loadAssetBundleAsync = true;
    }
}
