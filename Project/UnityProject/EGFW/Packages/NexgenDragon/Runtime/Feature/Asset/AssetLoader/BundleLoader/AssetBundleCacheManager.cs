using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NexgenDragon;

[Serializable]
public class AssetCacheRefInfo
{
    public string infoType;
    public string assetPath;
    public int refCount;
    public List<string> reasons;
}

internal class AssetBundleCacheManager
{
    Dictionary<string,AssetBundleCache> _allAssetbundle = new Dictionary<string, AssetBundleCache>();

    public AssetBundleCache LoadAssetBundleCache(string bundleName)
    {
        AssetBundleCache cache = null;
        if (_allAssetbundle.TryGetValue(bundleName, out cache))
        {
            cache.Increase();
            return cache;
        }
        else
        {
            cache = new AssetBundleCache(bundleName,this);
            cache.Increase();
            _allAssetbundle.Add(bundleName, cache);
            cache.Initialize();
            return cache;
        }
    }

    public void LoadAssetBundleCache(string bundleName,Action<AssetBundleCache> callback)
    {
        AssetBundleCache cache = null;
        if (_allAssetbundle.TryGetValue(bundleName, out cache))
        {
            callback(cache);
        }

    }
    //cache 的创建是同步的，方便做引用计数，加载是异步的
    public AssetBundleCache GetAssetBundleCache(string bundleName)
    {
        AssetBundleCache bundle = null;
        if (_allAssetbundle.TryGetValue(bundleName, out bundle))
        {
            return bundle;
        }
        else
        {
            bundle = new AssetBundleCache(bundleName,this);
            _allAssetbundle.Add(bundleName, bundle);
            bundle.Initialize();
        }
        return bundle;
    }

    public void CheckZeorBundle()
    {
        List<string> removeKeys = new List<string>(16);
        var bundleIt = _allAssetbundle.GetEnumerator();
        while (bundleIt.MoveNext())
        {
            if (bundleIt.Current.Value.ReferenceCount <= 0)
            {
                removeKeys.Add(bundleIt.Current.Key);
            }
        }
        var removeIt = removeKeys.GetEnumerator();
        while (removeIt.MoveNext())
        {
            _allAssetbundle.Remove(removeIt.Current);
        }
    }

    public void UnloadAll()
    {
        var bundleIt = _allAssetbundle.GetEnumerator();
        while (bundleIt.MoveNext())
        {
            bundleIt.Current.Value.UnLoadTrue();
        }
        bundleIt.Dispose();
        _allAssetbundle.Clear();
    }

    public void UnloadAllLua()
    {
        var luaList = new List<string>();
        foreach (var pair in _allAssetbundle)
        {
            if (AssetBundleConfig.Instance.IsLuaBundle(pair.Key))
            {
                luaList.Add(pair.Key);
            }
        }
        foreach (var bundleName in luaList)
        {
            _allAssetbundle[bundleName].UnLoadTrue();
            _allAssetbundle.Remove(bundleName);
        }
    }

    public List<AssetCacheRefInfo> DumpBundleRefInfo()
    {
        List<AssetCacheRefInfo> result = new List<AssetCacheRefInfo>(64);
        var bundleIt = _allAssetbundle.GetEnumerator();
        while (bundleIt.MoveNext())
        {
            AssetCacheRefInfo info = new AssetCacheRefInfo();
            info.assetPath = bundleIt.Current.Key;
            info.refCount = bundleIt.Current.Value.ReferenceCount;
			info.reasons = bundleIt.Current.Value.Reasons;
            result.Add(info);
        }
        return result;
    }

}
