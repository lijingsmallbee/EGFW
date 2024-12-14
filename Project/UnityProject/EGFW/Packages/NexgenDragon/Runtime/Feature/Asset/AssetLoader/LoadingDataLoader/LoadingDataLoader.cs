using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexgenDragon
{
    public class LoadingDataLoader:AssetBaseLoader
    {
        public const string LoadingAssetKey = "loadingprefab";
        Dictionary<string,string> _asset2Bundle = new Dictionary<string, string>();
        Dictionary<string,AssetBundle> _loadedBundle = new Dictionary<string, AssetBundle>();
        Dictionary<string,string> _asset2Path = new Dictionary<string, string>();
        // Use this for initialization
        public override UnityEngine.Object LoadAsset (BundleAssetData data,bool isSprite = false)
        {
            if (_asset2Path.TryGetValue(data.AssetName,out var assetPath) && _asset2Bundle.TryGetValue(data.AssetName,out var bundleName))
            {
                if (_loadedBundle.TryGetValue(bundleName, out var assetBundle))
                {
                    UnityEngine.Object o = null;
                    if (isSprite)
                    {
                        o = assetBundle.LoadAsset<Sprite>(assetPath);
                    }
                    else
                    {
                        o = assetBundle.LoadAsset(assetPath);
                    }

                    if (o == null)
                    {
                        NLogger.Error("can get asset {0},but load failed");
                    }
                    return o;
                }
                else
                {
                    NLogger.Error("can not find bundle {0}",bundleName);
                }
            }
            else
            {
                NLogger.WarnChannel("loadingasset","can not find asset {0}",data.AssetName);
            }
            return null;
        }
        
        //loading data暂时不支持异步加载
        public override bool LoadAsset(BundleAssetData data, System.Action<UnityEngine.Object> callback, bool isSprite,
            bool asap = false)
        {
            return false;
        }
        
        //策略是，先判断包外资源是否可用，不可用就用包内的
        private bool GetBundleNamesInBundleTypeInDocument(List<string> input, List<string> output, string bundleType)
        {
            //先判断包外是否有bundle_list.json
            var path = AssetBundleSynchro.Instance.GetLocalCacheBundleListFilePath(bundleType);
            var haveInDoc = AssetManager.Instance.IOtool.HaveGameAssetInDocument(path);
            if (haveInDoc)
            {
                var json = AssetManager.Instance.IOtool.ReadGameAssetAsText(path);
                if (!string.IsNullOrEmpty(json))
                {
                    var localBundleList = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(json);
                    var loadingBundle = string.Empty;
                    if (localBundleList != null)
                    {
                        foreach (var pair in localBundleList)
                        {
                            var bundle = pair.Key;
                            if (input.Count == 1)
                            {
                                if (bundle.Contains(input[0]))
                                {
                                    output.Add(bundle);
                                    break;
                                }
                            }
                            else
                            {
                                foreach (var b in input)
                                {
                                    if (bundle.Contains(b))
                                    {
                                        if (!output.Contains(bundle))
                                        {
                                            output.Add(bundle);
                                            break;
                                        }
                                    }
                                }

                                if (output.Count == input.Count)
                                {
                                    break;
                                }
                            }
                        }
                        
                        bool find = input.Count == output.Count;
                        if (!find)
                        {
                            NLogger.WarnChannel("loadingasset","loading loader find {0} in {1} failed",input[0],bundleType);
                        }
                        return input.Count == output.Count;
                    }
                    else
                    {
                        NLogger.Error("can find {0} but deserialize json failed",path);
                    }
                }
                else
                {
                    NLogger.Error("can find {0} but load json failed",path);
                }
            }

            return false;
        }
        
        private bool GetBundleNamesInBundleTypeInPackage(List<string> input, List<string> output, string bundleType)
        {
            //先判断包外是否有bundle_list.json
            var path = AssetBundleSynchro.Instance.GetLocalBundleListFilePath(bundleType);
            var haveInPkg = AssetManager.Instance.IOtool.HaveGameAssetInPackage(path);
            if (haveInPkg)
            {
                var json = AssetManager.Instance.IOtool.ReadStreamingAssetAsText(path);
                if (!string.IsNullOrEmpty(json))
                {
                    var localBundleList = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(json);
                    if (localBundleList != null)
                    {
                        foreach (var pair in localBundleList)
                        {
                            var bundle = pair.Key;
                            if (input.Count == 1)
                            {
                                if (bundle.Contains(input[0]))
                                {
                                    output.Add(bundle);
                                    break;
                                }
                            }
                            else
                            {
                                foreach (var b in input)
                                {
                                    if (bundle.Contains(b))
                                    {
                                        if (!output.Contains(bundle))
                                        {
                                            output.Add(bundle);
                                            break;
                                        }
                                    }
                                }

                                if (output.Count == input.Count)
                                {
                                    break;
                                }
                            }
                        }
                        
                        bool find = input.Count == output.Count;
                        if (!find)
                        {
                            NLogger.WarnChannel("loadingasset","loading loader find {0} in {1} failed",input[0],bundleType);
                        }

                        return input.Count == output.Count;
                    }
                    else
                    {
                        NLogger.Error("can find {0} but deserialize json failed",path);
                    }
                }
                else
                {
                    NLogger.Error("can find {0} but load json failed",path);
                }
            }
            return false;
        }
        
        private bool LoadBundle(string bundle)
        {
            string fullPath = AssetManager.Instance.AssetPathProvider.GetBundlePath(bundle);
            if (!string.IsNullOrEmpty(fullPath))
            {
                var loaded = AssetBundle.GetAllLoadedAssetBundles();
                foreach (var load in loaded)
                {
                    //已经加载的bundle里，没有hash，所以需要用contains
                    if (bundle.Contains(load.name))
                    {
                        if (!bundle.Contains("static+font"))
                        {
                            load.Unload(true);    
                        }
                        //如果是文字bundle，直接返回加载成功
                        else
                        {
                            return true;
                        }
                        break;
                    }
                }
                var bundleInst = AssetBundle.LoadFromFile(fullPath);
                if (bundleInst != null)
                {
                    _loadedBundle.Add(bundle,bundleInst);
                    return true;
                }
                else
                {
                    NLogger.Error("load bundle {0} failed",fullPath);
                }
            }
            else
            {
                NLogger.Error("get bundle full path failed {0}",bundle);
            }
            return false;
        }

        public override bool Init ()
        {
            var success = false;
            _asset2Bundle.Clear();
            //先判断包外是否有bundle_list.json
            var useDocument = false;
            var gOutput = new List<string>(2);
            var rOutput = new List<string>(2);
            if (GetBundleNamesInBundleTypeInDocument(new List<string> {LoadingAssetKey}, gOutput, "G"))
            {
                if(GetBundleNamesInBundleTypeInDocument(new List<string>{"static+font"},rOutput,"R"))
                {
                    if (LoadBundle(gOutput[0]) && LoadBundle(rOutput[0]))
                    {
                        useDocument = true;
                        success = true;
                    }
                }
            }
            
            if (!useDocument)
            {
                NLogger.WarnChannel("loadingasset","use streaming asset for loading");
                gOutput.Clear();
                rOutput.Clear();
                if (GetBundleNamesInBundleTypeInPackage(new List<string> {LoadingAssetKey}, gOutput, "G"))
                {
                    if(GetBundleNamesInBundleTypeInPackage(new List<string>{"static+font"},rOutput,"R"))
                    {
                        if (LoadBundle(gOutput[0]) && LoadBundle(rOutput[0]))
                        {
                           success  = true;
                        }
                    }
                }   
            }
            
            if (!success)
            {
                NLogger.Error("init loading asset failed");
            }

            var path = AssetBundleSynchro.Instance.GetLocalBundleListFilePath("G");
            var json = AssetManager.Instance.IOtool.ReadStreamingAssetAsText(path);
            if (!string.IsNullOrEmpty(json))
            {
                var localBundleList = AssetManager.Instance.DataParser.FromJson<Dictionary<string,string>> (json);
                if (localBundleList != null)
                {
                    var loadingBundleList = new List<string>(5);
                    foreach (var pair in localBundleList)
                    {
                        var bundle = pair.Key;
                        if (bundle.Contains("loadingimage"))
                        {
                            loadingBundleList.Add(bundle);
                        }
                    }

                    foreach (var bundle in loadingBundleList)
                    {
                        LoadBundle(bundle);
                    }

                    if (_loadedBundle.Count == 0)
                    {
                        NLogger.Error("load loading bundle failed,zero");
                    }

                    foreach (var pair in _loadedBundle)
                    {
                        var assets = pair.Value.GetAllAssetNames();
                        foreach (var assetPath in assets)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(assetPath);
                            if (!_asset2Bundle.ContainsKey(fileName))
                            {
                                _asset2Bundle.Add(fileName,pair.Key);    
                            }
                            if (!_asset2Path.ContainsKey(fileName))
                            {
                                _asset2Path.Add(fileName,assetPath);
                            }
                        }
                    }
                }
                else
                {
                    NLogger.Error("can not deserialize json file {0}",path);
                }
            }
            else
            {
                NLogger.WarnChannel("loadingasset","can not load json file {0}",path);
            }
            return true;
        }

        public override void OnAssetCacheCreate(BundleAssetData assetData)
        {
            
        }

        public override void OnAssetCacheRelease(BundleAssetData assetData)
        {
            
        }

        public override void Clear()
        {
            foreach (var pair in _loadedBundle)
            {
                if (pair.Value)
                {
                    if (!pair.Key.Contains("static+font"))
                    {
                        pair.Value.Unload(true);    
                    }
                }
            }
            _loadedBundle.Clear();
            _asset2Bundle.Clear();
            _asset2Path.Clear();
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
            Clear();
            Init();
        }

        public override List<ShaderVariantCollection> LoadSVCFormBundleList(List<string> bundleList)
        {
            return null;
        }
    }
}