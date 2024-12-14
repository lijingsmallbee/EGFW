using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;

public class BundleDependenceData
{
    public List<string> dependences = _emptyList;
    
    private static List<string> _emptyList = new List<string>();
}
public class BundleDependence:Singleton<BundleDependence>
{
    private Dictionary<string,BundleDependenceData> _allDependence = new Dictionary<string, BundleDependenceData>();
    private Dictionary<string,string> _rBundleName2Name = new Dictionary<string, string>();
    private Dictionary<string,string> _gBundleName2Name = new Dictionary<string, string>();
//	public void Init()
//	{
//		_allDependence.Clear();
//
//		var allBundleType = AssetBundleSynchro.Instance.AllBundleType;
//		foreach(var bundleType  in allBundleType)
//		{
//			string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, string.Format("bundle_dependences_{0}.json", bundleType));
//
//			string jsonText = string.Empty;
//			if(AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
//			{
//				jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
//			}
//				
//			if (!string.IsNullOrEmpty(jsonText))
//			{
//				Dictionary<string,List<string>> datas = AssetManager.Instance.DataParser.FromJson<Dictionary<string,List<string>>>(jsonText);
//		        var dataIt = datas.GetEnumerator();
//		        while (dataIt.MoveNext())
//		        {
//		            BundleDependenceData dependenceData = new BundleDependenceData();
//		            dependenceData.bundleName = dataIt.Current.Key;
//		            dependenceData.dependences = dataIt.Current.Value;
//
//					if(!_allDependence.ContainsKey(dataIt.Current.Key))
//					{
//						_allDependence.Add(dataIt.Current.Key, dependenceData);
//					}
//					else
//					{
//						NLogger.Error("bundle already exist {0}", dataIt.Current.Key);
//					}
//		        }
//			}
//			
//		}
//		if (AssetManager.Instance.EnvironmentVariable.UNITY_DEBUG())
//		{
//			foreach (var dep in _allDependence)
//			{
//				CheckCircleDependence(dep.Key);									
//			}
//		}
//    } 

	public string GetOriginalBundleName(string fileName)
	{
		if (fileName.Contains("ota+"))
		{
			if (_gBundleName2Name.TryGetValue(fileName, out var oriName))
			{
				return oriName;
			}
		}
		else if (fileName.Contains("static+"))
		{
			if (_rBundleName2Name.TryGetValue(fileName, out var oriName))
			{
				return oriName;
			}
		}
		return string.Empty;
	}

	public bool LoadBundleDependenceByType(string bundleType)
	{
		string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, string.Format("bundle_dependences_{0}.json", bundleType));
		Dictionary<string, List<string>> datas = null;
		try
		{
			string jsonText = string.Empty;
			if(AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
			{
				jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
			}

			if (!string.IsNullOrEmpty(jsonText))
			{
				datas = AssetManager.Instance.DataParser.FromJson<Dictionary<string, List<string>>>(jsonText);
			}
		}
		catch (Exception e)
		{
			NLogger.Error("load dependence file {0} failed,error {1}",bundleType,e.ToString());
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		if (datas == null || datas.Count == 0)
		{
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}

		Dictionary<string, string> nameDic = null;
		if (bundleType == "R")
		{
			if (!LoadBundleNameByType("G", out nameDic))
				return false;
			//这里只处理了g，没有处理r，因为r里东西很少了，也没有很深的路径，暂时不处理没问题
			foreach (var pair in nameDic)
			{
				_gBundleName2Name[pair.Value] = pair.Key;
			}
		}




		using(var dataIt = datas.GetEnumerator())
		{
			while (dataIt.MoveNext())
			{
				BundleDependenceData dependenceData = new BundleDependenceData();
				//dependenceData.bundleName = dataIt.Current.Key;
				dependenceData.dependences = dataIt.Current.Value;

				if (bundleType == "R")
				{
					//匹配当前G内的依赖bundle，支持R依赖的G内的bundle进行更新
					ReplaceDependBundleName(nameDic, dependenceData.dependences, "G");
				}

				if (!_allDependence.ContainsKey(dataIt.Current.Key))
				{
					_allDependence.Add(dataIt.Current.Key, dependenceData);
				}
				else
				{
					_allDependence[dataIt.Current.Key] = dependenceData;					
				}
			}
		}
		if (AssetManager.Instance.EnvironmentVariable.UNITY_DEBUG())
		{
			foreach (var dep in _allDependence)
			{
				CheckCircleDependence(dep.Key);
			}
		}
		return true;
	}

	private void CheckCircleDependence(string bundleName)
	{
		BundleDependenceData deps = null;
		_allDependence.TryGetValue(bundleName, out deps);
		if (deps != null)
		{
			if (deps.dependences.Contains(bundleName))
			{
				//NLogger.Error("bundle {0}'s dependence contains self",bundleName);
			}
			foreach (var dep in deps.dependences)
			{
				BundleDependenceData depDeps = null;
				_allDependence.TryGetValue(dep, out depDeps);
				if (depDeps != null && depDeps.dependences.Contains(bundleName))
				{
					NLogger.Warn("bundle dependence have circle between {0} and {1}",bundleName,dep);
				}				
			}
		}
	}

	private bool LoadBundleNameByType(string bundleType, out Dictionary<string, string> nameDic)
	{
		nameDic = new Dictionary<string, string>();
		string jsonFile = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, string.Format("bundle_name_{0}.json", bundleType));
		try
		{
			string jsonText = string.Empty;
			if(AssetManager.Instance.IOtool.HaveGameAsset(jsonFile))
			{
				jsonText = AssetManager.Instance.IOtool.ReadGameAssetAsText(jsonFile);
			}

			if (!string.IsNullOrEmpty(jsonText))
			{
				nameDic = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(jsonText);
			}
		}
		catch (Exception e)
		{
			NLogger.Error("load bundle name file {0} failed,error {1}", bundleType, e.ToString());
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		if (nameDic == null || nameDic.Count == 0)
		{
			AssetManager.Instance.IOtool.DeleteGameAsset(jsonFile);
			LoadExistBundleFail evt = new LoadExistBundleFail("", jsonFile);
			EventManager.Instance.TriggerEvent(evt);
			NLogger.Error("file {0} error,will delete and reset", jsonFile);
			return false;
		}
		
		return true;
	}

	private void ReplaceDependBundleName(Dictionary<string, string> nameDic, List<string> dependences, string bundleType)
	{
		if (nameDic == null || dependences == null)
		{
			return;
		}
		
		string prefix;
		if (bundleType == "G")
		{
			prefix = "ota+";
		}
		else
		{
			prefix = "static+";
		}
		
		for (int i = 0; i < dependences.Count; i++)
		{
			string bundleName = dependences[i];
			if (bundleName.StartsWith(prefix))
			{
				if (nameDic.TryGetValue(bundleName, out string hashName))
				{
					dependences[i] = hashName;
				}
				else
				{
					NLogger.Error("bundle {0} not find hash name.", bundleName);
				}
			}
		}
	}


	public BundleDependenceData GetBundleDependence(string bundleName)
    {
        BundleDependenceData data = null;
        _allDependence.TryGetValue(bundleName, out data);
        if (data == null)
        {
	        data = new BundleDependenceData();
	        //data.bundleName = bundleName;
        }
        return data;
    }
}
