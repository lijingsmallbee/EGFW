using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace NexgenDragon
{
    
    public class CustomBundleLoader : Singleton<CustomBundleLoader>, IManager
    {
        public string GetCustomBundleFilePath(string fileName)
        {
            var rPath = AssetManager.Instance.AssetPathProvider.GetCustomBundlePath(fileName);
#if UNITY_EDITOR && !USE_BUNDLE_ANDROID && !USE_BUNDLE_IOS
            if (string.IsNullOrEmpty(rPath))
            {
                rPath = _InnerPath(fileName);                
            }
#else
            if (string.IsNullOrEmpty(rPath))
            {
                NLogger.Error("{0} get full path failed,get {1}", fileName, rPath);
            }
#endif
            return rPath;
        }

        public bool HasCustomBundle(string inputName)
        {
            var path = _InnerPath(inputName);
            //TODO: fix me
            return File.Exists(path);
        }

        private string _InnerPath(string input)
        {
            var path = Path.Combine(Application.streamingAssetsPath, $"CustomBundle/PreCache/{input}");
            return path;
        }
        

#region CustomBundleMgr

        private Dictionary<string, CustomBundle> _registerCustomBundles = new Dictionary<string, CustomBundle>();
        public void RegisterCustomBundle(string bundleName, CustomBundle bundle)
        {
            if (_registerCustomBundles.ContainsKey(bundleName))
            {
                NLogger.Error($"[CustomBundleLoader][Error] repeated {bundleName}");
                return;
            }

            //var bundlePath = GetCustomBundleFilePath(bundleName);
        
            bundle.OnWillOpen(bundleName, GetCustomBundleFilePath);
            
            _registerCustomBundles[bundleName] = bundle;
        }

        public bool TryGetCustomBundle(string name, out CustomBundle cBundle)
        {
            return _registerCustomBundles.TryGetValue(name, out cBundle);
        }
        
        public bool UnRegisterCustomBundle(string bundleName)
        {
            CustomBundle cBundle = null;
            _registerCustomBundles.TryGetValue(bundleName, out cBundle);
            if (cBundle != null)
            {
                cBundle.Unload();
                return true;
            }
            return false;
        }
        
        private void ClearCustomBundle()
        {
            foreach (var de in _registerCustomBundles)
            {
                de.Value.Unload();
            }
            _registerCustomBundles.Clear();
        }
        
        
#endregion

        public void Initialize(NexgenObject configParam)
        {
            ClearCustomBundle();
        }

        public void Reset()
        {
            ClearCustomBundle();
        }

        public override void Release()
        {
            base.Release();
            Reset();
        }
    }

}
