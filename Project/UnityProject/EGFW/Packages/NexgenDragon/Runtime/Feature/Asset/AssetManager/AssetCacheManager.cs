using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;
using System;

public delegate void AssetInternalCallBack(BundleAssetData data,UnityEngine.Object loadedAsset);
internal class AssetCacheManager : NexgenObject,ISecondTicker
{
    private Dictionary<string,AssetCache> _allAssetCache = new Dictionary<string, AssetCache>();
    public void Init()
    {
    //    GameFacade.Instance.AddTicker(this);
    }
    //获得一个资源，不存在不会创建
    public AssetCache GetAssetCache(string assetPath)
    {
        AssetCache asset = null;
        if (_allAssetCache.TryGetValue(assetPath, out asset))
        {
            asset.Increase();
            return asset;
        }
        return null;
    }
    //加载的时候依据文件的唯一标示来进行，内部会有路径处理逻辑，不需要传入路径
    //传入路径可能导致 bundle模式加载失败
    public AssetCache LoadAssetCache(string assetIndex,bool isSprite)
    {
        AssetCache asset = null;
        if (_allAssetCache.TryGetValue(assetIndex, out asset))
        {
            asset.Increase();
            asset.Load();
            return asset;
        }
        if (assetIndex == string.Empty){
            NLogger.Error("AssetCahcheManager LoadAssetCache assetIndex is empty");
            return asset;
        }
        AssetCache newCache = new AssetCache(assetIndex);
        newCache.IsSprite = isSprite;
        newCache.Load();
        newCache.Increase();
        _allAssetCache.Add(assetIndex, newCache);
        return newCache;
    }

    //加载的时候依据文件的唯一标示来进行，内部会有路径处理逻辑，不需要传入路径
    //传入路径可能导致 bundle模式加载失败
    public void LoadAssetCache(string assetIndex,Action<AssetCache> callback,bool isSprite, int priority = 999, bool asap = false)
    {
        //assetCache对象的创建是同步的，所以即使没有加载完毕的时候立即释放对象，引用计数不会出错
        AssetCache asset = null;
        if (_allAssetCache.TryGetValue(assetIndex, out asset))
        {
            asset.Increase();
        }
        else
        {
            asset = new AssetCache(assetIndex);
            asset.IsSprite = isSprite;
            asset.Increase();
            _allAssetCache.Add(assetIndex, asset);
        }
        asset.Load((success) =>
        {
            callback(asset);
        },priority, asap);
    }

    //与加载一样，卸载也是基于 cache index，资源的唯一标示
    public void UnloadAsset(string assetIndex)
    {
        AssetCache asset = null;
        if (_allAssetCache.TryGetValue(assetIndex, out asset))
        {
            var isZero = asset.Decrease();
            //如果是prefab类型的资源，不会立即卸载，会进入倒计时逻辑，经过一段时间后才会卸载
            //发生切换关卡调用了clear unused，会把倒计时中的资源卸载
            if (isZero && asset.CacheState != AssetCache.AssetCacheState.loading && asset.CanUnloadImmediate())
            {
                asset.Release();
                _allAssetCache.Remove(assetIndex);
            }
        }
    }
    
    // 不要每次都new
    List<string> removeList = new List<string>(64);
    //这个函数秒级调用即可
    void CheckCacheRemove()
    {
        removeList.Clear();
        var cacheIt = _allAssetCache.GetEnumerator();
        while (cacheIt.MoveNext())
        {
            if (cacheIt.Current.Value.CheckRemove())
            {
                cacheIt.Current.Value.Release();
                removeList.Add(cacheIt.Current.Key);
            }
        }
        var removeIt = removeList.GetEnumerator();
        while (removeIt.MoveNext())
        {
            _allAssetCache.Remove(removeIt.Current);
        }
    }
        
    public List<AssetCacheRefInfo> DumpAssetRefCount()
    {
        List<AssetCacheRefInfo> result = new List<AssetCacheRefInfo>(64);
        var cacheIt = _allAssetCache.GetEnumerator();
        while (cacheIt.MoveNext())
        {
            AssetCacheRefInfo info = new AssetCacheRefInfo();
            info.assetPath = cacheIt.Current.Key;
            info.refCount = cacheIt.Current.Value.ReferenceCount;
            result.Add(info);
        }
        return result;
            
    }
    
    public override void Release()
    {
        base.Release();
    //    GameFacade.Instance.RemoveTicker(this);
    }

    public void ClearAll()
    {
        var resIt = _allAssetCache.GetEnumerator();
        while (resIt.MoveNext())
        {
            resIt.Current.Value.Release();
        }
        resIt.Dispose();
        _allAssetCache.Clear();
    }

    public void ClearAllNotProcessBundle()
    {
        var resIt = _allAssetCache.GetEnumerator();
        while (resIt.MoveNext())
        {
            resIt.Current.Value.ReleaseNotProcessBundle();
        }
        resIt.Dispose();
        _allAssetCache.Clear();
    }

    public void ClearUnused()
    {
        var resIt = _allAssetCache.GetEnumerator();
        var deleteList = new List<string>(16);
        while (resIt.MoveNext())
        {
            if (resIt.Current.Value.CanRemove())
            {
                deleteList.Add(resIt.Current.Key);
                resIt.Current.Value.Release();
            }
        }
        resIt.Dispose();

        var deleteIt = deleteList.GetEnumerator();
        while (deleteIt.MoveNext())
        {
            _allAssetCache.Remove(deleteIt.Current);
        }
        deleteIt.Dispose();

    }

    public void Tick(float delta)
    {
    //    CheckCacheRemove();
    }
}
