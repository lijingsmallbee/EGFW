using System;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NexgenDragon
{
    public abstract class BaseAssetDatabaseLoader : AssetBaseLoader
    {        
        public override Object LoadAsset(BundleAssetData data,bool isSprite)
        {
            throw new System.NotImplementedException();
        }

        public override bool LoadAsset(BundleAssetData data, System.Action<Object> callback,bool isSprite, bool asap)
        {
            throw new System.NotImplementedException();
        }
        
        public override bool LoadSceneAsync(string sceneName,LoadSceneMode mode,Action<bool> callback)
        {
            return true;
        }

        public override bool Init()
        {
            throw new System.NotImplementedException();
        }

        public override void OnAssetCacheCreate(BundleAssetData assetData)
        {
            throw new System.NotImplementedException();
        }

        public override void OnAssetCacheRelease(BundleAssetData assetData)
        {
            throw new System.NotImplementedException();
        }

        public override void Clear()
        {
            
        }
    }
}