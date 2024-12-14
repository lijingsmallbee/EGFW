using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public abstract class AssetBaseLoader
{
    public abstract UnityEngine.Object LoadAsset(BundleAssetData data,bool isSprite);
    public abstract bool LoadAsset(BundleAssetData data,Action<UnityEngine.Object> callback,bool isSprite, bool asap = false);
    public abstract bool Init();
    public abstract void OnAssetCacheCreate(BundleAssetData assetData);
    public abstract void OnAssetCacheRelease(BundleAssetData assetData);
    public abstract void Clear();
    public abstract void ClearScript();
    public abstract bool LoadSceneAsync(string sceneName, LoadSceneMode mode, Action<bool> callback);

    public abstract void Reset();

    public abstract List<ShaderVariantCollection> LoadSVCFormBundleList(List<string> bundleList);
    private string vinayGaoClassType = null;
    public string VinayGaoGetClassType
    {
        get
        {
            if (vinayGaoClassType == null)
            {
                vinayGaoClassType ="VinayGao:" + GetType().Name;
            }

            return vinayGaoClassType;
        }
    }
    
}
