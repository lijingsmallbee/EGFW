using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace NexgenDragon
{
    //这个类只有在plugin.dll里才可以new出实例和使用
    public class ResourcesLoader : AssetBaseLoader
    {
        private Dictionary<string,string> _allDataPath = new Dictionary<string, string>();
        private string GetResourcePath(string assetName)
        {
            string assetPath;
            _allDataPath.TryGetValue(assetName, out assetPath);
            return string.IsNullOrEmpty(assetPath) ? string.Empty : assetPath;
        }
        public override UnityEngine.Object LoadAsset (BundleAssetData data,bool isSprite)
        {            
            var assetPath = GetResourcePath(data.AssetName);
            
            NLogger.Trace("[ResourcesLoader]LoadAsset: Path = {0}, IsSprite = {1}, AssetName = {2}", 
                assetPath, isSprite, data.AssetName);
            
            if (isSprite)
            {
                return Resources.Load(assetPath, typeof(Sprite));
            }
            else
            {
                return Resources.Load(assetPath);   
            }
                
        }

        public override bool LoadAsset (BundleAssetData data, System.Action<UnityEngine.Object> callback,bool isSprite, bool asap)
        {
            var assetPath = GetResourcePath(data.AssetName);
            ResourceRequest req = null;
            if (isSprite)
            {
                req = Resources.LoadAsync(assetPath,typeof(Sprite));
            }
            else
            {
                req = Resources.LoadAsync(assetPath);
            }
            if (req != null)
            {
                AsyncRequestManager.Instance.AddTask(AsyncRequest<ResourceRequest>.Create(req,OnAssetLoaded,callback));
                return true;
            }
            return false;
        }

        private void OnAssetLoaded(ResourceRequest req,object userData)
        {
            var callback = userData as System.Action<UnityEngine.Object>;
            if (callback != null)
            {
                callback(req.asset);
            }
        }


        public override bool Init ()
        {
            var configtxt = Resources.Load<TextAsset>("ResourcesPath");
            
            NLogger.LogChannel("ResourcesLoader", "ResourcesLoader Init with: " + configtxt.text);
            
            _allDataPath = AssetManager.Instance.DataParser.FromJson<Dictionary<string,string>>(configtxt.text);
            return true;
        }

        public override void OnAssetCacheCreate(BundleAssetData assetData)
        {
            throw new NotImplementedException();
        }

        public override void OnAssetCacheRelease(BundleAssetData assetData)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            
        }

        public override void ClearScript()
        {
            throw new NotImplementedException();
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
            return null;
        }

        public bool ContainsAsset(string asset)
        {
            return _allDataPath.ContainsKey(asset);
        }
    }

}
