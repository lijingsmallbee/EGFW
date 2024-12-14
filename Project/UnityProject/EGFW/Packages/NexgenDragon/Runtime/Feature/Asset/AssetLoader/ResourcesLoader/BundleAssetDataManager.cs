using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;

public class BundleAssetDataManager:Singleton<BundleAssetDataManager>
{
	private bool _rInit = false;
	private bool _gInit = false;
    private Dictionary<string,BundleAssetData> allAssetData = new Dictionary<string, BundleAssetData>();
    private Dictionary<string,string> _allPathData = new Dictionary<string, string>();
	   
//	public void Init()
//    {
//        allAssetData.Clear();
//
//		var allBundleType = AssetBundleSynchro.Instance.AllBundleType;
//		foreach(var bundleType in allBundleType)
//		{
//			string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, string.Format("bundle_assets_{0}.json", bundleType));
//
//			string jsonText = string.Empty;
//			if(AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
//			{
//				jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
//			}
//
//	        if (!string.IsNullOrEmpty(jsonText))
//	        {
//				Dictionary<string,string> allData = AssetManager.Instance.DataParser.FromJson<Dictionary<string,string>>(jsonText);
//	            int dataCount = allData.Count;
//	            var dataIt = allData.GetEnumerator();
//	            while (dataIt.MoveNext())
//	            {
//                    var bundledata = new BundleAssetData(dataIt.Current.Key, dataIt.Current.Value);
//                    if (!allAssetData.ContainsKey(bundledata.AssetName))
//                    {
//                        allAssetData.Add(bundledata.AssetName,bundledata);
//                    }
//                    else
//                    {
//                        NLogger.Warn("asset {0} has been added once,bundle {1}", dataIt.Current.Key, dataIt.Current.Value);
//                    }
//	                
//	            } 
//	        }
//	        else
//	        {
//	            NLogger.Error("bundle R config text is null or empty");
//	        }
//		}
//
//        NLogger.Log("bundle asset data count is {0}", allAssetData.Count);
//
//    }

	public bool LoadAssetPathByType(string bundleType)
	{		
		string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, string.Format("assets_path_{0}.json", bundleType));
		Dictionary<string, string> allData = new Dictionary<string, string>();
		try
		{
			string jsonText = string.Empty;
			if(AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
			{
				jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
				if (!string.IsNullOrEmpty(jsonText))
				{
					allData = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(jsonText);
				}
			}
		}
		catch (Exception e)
		{
			NLogger.Error("load asset path {0} failed,error {1}",bundleType,e.ToString());
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		//这里解析失败，
		if (allData == null || allData.Count == 0)
		{
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		using (var dataIt = allData.GetEnumerator())
		{
			while (dataIt.MoveNext())
			{
				if (_allPathData.ContainsKey(dataIt.Current.Key))
				{
					_allPathData[dataIt.Current.Key] = dataIt.Current.Value;
				}
				else
				{
					_allPathData.Add(dataIt.Current.Key, dataIt.Current.Value);
				}
			}
		}
		return true;
	}

	public bool LoadBundleAssetByType(string bundleType)
	{
		NLogger.LogChannel("Asset", "LoadBundleAssetByType " + bundleType);

		Dictionary<string, string> allData = new Dictionary<string, string>();
		string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType,
			string.Format("bundle_assets_{0}.json", bundleType));

		NLogger.Log($"LoadBundleAsset {bundleType} jsonFile {jsonFile}");

		try
		{
			if (AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
			{
				var jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
				if (!string.IsNullOrEmpty(jsonText))
				{
					allData = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(jsonText);
				}
			}
		}
		catch (Exception e)
		{
			NLogger.Error("read bundle assets {0} failed,error {1}",bundleType,e.ToString());
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		if (allData == null || allData.Count == 0)
		{
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;	
		}
		
		using (var dataIt = allData.GetEnumerator())
		{
			while (dataIt.MoveNext())
			{
				var bundledata = new BundleAssetData(dataIt.Current.Key, dataIt.Current.Value);
				if (!allAssetData.ContainsKey(bundledata.AssetName))
				{
					allAssetData.Add(bundledata.AssetName, bundledata);
				}
				//如果发生已有的记录，用新的进行覆盖
				else
				{
					allAssetData[bundledata.AssetName] = bundledata;
				}
			}	
		}

		if (bundleType == "R")
		{
			_rInit = true;
		}
		else if (bundleType == "G")
		{
			_gInit = true;
		}
		return true;
	}

	public bool ContainsAsset(string assetPath)
    {
        return allAssetData.ContainsKey(assetPath);
    }

    public BundleAssetData GetAssetData(string assetPath)
    {
        BundleAssetData data = null;
        allAssetData.TryGetValue(assetPath, out data);
        return data;
    }
    
    public string GetAssetPath(string assetName)
    {
	    string assetPath;
	    _allPathData.TryGetValue(assetName, out assetPath);
	    return assetPath;
    }

    public bool DataAllInit()
    {
	    return _gInit && _rInit;
    }
}
