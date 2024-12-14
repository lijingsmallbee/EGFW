using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NexgenDragon
{
	public class UpdateVersionListState : BaseBundleSynchroState
	{
		Dictionary<string, Coroutine> _allCoroutine = new Dictionary<string, Coroutine>();

		public UpdateVersionListState(AssetBundleSynchro context):base(context)
		{

		}

		public override void Enter ()
		{
			base.Enter();

			NLogger.LogChannel("AssetBundleSynchro", "CheckNeedUpdateState:Enter");

			_allCoroutine.Clear();

			var allBundleType = _context.AllBundleType;

			foreach(var bundleType in allBundleType)
			{
				_allCoroutine.Add(bundleType, 
					CoroutineManager.Instance.StartCoroutine(WorkCoroutine(bundleType)));
			}
		}

		public override void Tick (float delta)
		{
			if(_allCoroutine.Count <= 0)
			{
				_context.UpdateVersionListComplete = true;
			}
		}

		public override void Exit ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "CheckNeedUpdateState:Exit");

			foreach(var keyValue in _allCoroutine)
			{
				if(keyValue.Value != null)
				{
					CoroutineManager.Instance.StopCoroutine(keyValue.Value);
				}
			}

			_allCoroutine.Clear();
		}

		IEnumerator WorkCoroutine(string bundleType)
		{
			yield return null;
			Perf.BeginSample("VinayGao:UpdateVersionListState.WorkCoroutine");
			string packageVersion = _context.GetPackageVersion(bundleType);

			#if DO_NOT_SYNC_BUNDLE
				_context.SetRemoteVersion (bundleType, packageVersion);
			#endif

			if (AssetBundleSynchro.Instance.SkipBundleSync)
			{
				_context.SetRemoteVersion (bundleType, packageVersion);
			}
			string remoteVersion = _context.GetRemoteVersion(bundleType);
			NLogger.Log ("{0} Local Version {1}, Remote Version {2}", bundleType, packageVersion, remoteVersion);

			bool remoteBundleListFileDownloaded = false;

			string localBundleListFilePath = _context.GetLocalBundleListFilePath (bundleType);
			string packageBundleListFilePath = _context.GetPackageBundleListFilePath (bundleType);
			string remoteBundleListFilePath = string.Empty;
			
			var h4 = AssetManager.Instance.IOtool.HaveGameAsset(packageBundleListFilePath);
			if (!h4)
			{
				AssetManager.Instance.IOtool.CopyStreamingAssetToExternal (localBundleListFilePath, packageBundleListFilePath);
				//首次安装包的时候，删除资源的配置文件,保证对以前资源的利用
				//理论上不用再删除旧包的json 但为了确保临界情况，这里覆盖安装还是删除
				AssetManager.Instance.IOtool.DeleteGameAsset(localBundleListFilePath);
				var localDepName = string.Format("bundle_dependences_{0}.json", bundleType);
				var localDependence = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType, localDepName);
				AssetManager.Instance.IOtool.DeleteGameAsset(localDependence);
				var localBundleAssets = string.Format("bundle_assets_{0}.json", bundleType);
				var localBundleAssetsPath = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType,localBundleAssets);
				AssetManager.Instance.IOtool.DeleteGameAsset(localBundleAssetsPath);
				//var assetPathName = string.Format("assets_path_{0}.json", bundleType);
				//var localAssetPathPath = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType,assetPathName);
				//AssetManager.Instance.IOtool.DeleteGameAsset(localAssetPathPath);
				var bundleName = string.Format("bundle_name_{0}.json", bundleType);
				var localBundleNamePath = AssetManager.Instance.AssetPathProvider.GetBundleConfigPath(bundleType,bundleName);
				AssetManager.Instance.IOtool.DeleteGameAsset(localBundleNamePath);
			}

			if(packageVersion == remoteVersion)
			{
				remoteBundleListFilePath = packageBundleListFilePath;
			}
			else
			{
				remoteBundleListFilePath = _context.GetRemoteBundleListFilePath(bundleType, remoteVersion);
			}
			var h3 = AssetManager.Instance.IOtool.HaveGameAsset(remoteBundleListFilePath);
			if (h3) 
			{
				remoteBundleListFileDownloaded = true;
			} 
			else 
			{
				// load from remote.
				AssetManager.Instance.DownLoader.DownloadAsset(
					_context.GetRemoteBundleListUrl(bundleType, remoteVersion),
					AssetManager.Instance.IOtool.GetExternalFullGameAssetPath (remoteBundleListFilePath),
					delegate(bool result, HttpResponseData responseData)
					{
						if (result)
						{
							remoteBundleListFileDownloaded = true;
						} 
						else
						{
							if(_context.Listener != null)
							{
								_context.Listener.OnLoadAssetFailed();
							}
						}
					}, 999);
			}
			
			bool remoteBundleSizeFileDownloaded = false;
			string localBundleSizeFilePath = _context.GetLocalBundleSizeFilePath(bundleType);
			string packageBundleSizeFilePath = _context.GetPackageBundleSizeFilePath (bundleType);
			string remoteBundleSizeFilePath = string.Empty;
			var h2 = AssetManager.Instance.IOtool.HaveGameAsset(packageBundleSizeFilePath);
			if (!h2) 
			{
				AssetManager.Instance.IOtool.CopyStreamingAssetToExternal (localBundleSizeFilePath, packageBundleSizeFilePath);
			}

			if(packageVersion == remoteVersion)
			{
				remoteBundleSizeFilePath = packageBundleSizeFilePath;
			}
			else
			{
				remoteBundleSizeFilePath = _context.GetRemoteBundleSizeFilePath(bundleType, remoteVersion);
			}

			var h1 = AssetManager.Instance.IOtool.HaveGameAsset(remoteBundleSizeFilePath);
			if (h1) 
			{
				remoteBundleSizeFileDownloaded = true;
			} 
			else 
			{
				// load from remote.
				AssetManager.Instance.DownLoader.DownloadAsset(_context.GetRemoteBundleSizeUrl(bundleType, remoteVersion),
					AssetManager.Instance.IOtool.GetExternalFullGameAssetPath (remoteBundleSizeFilePath),
					delegate(bool result, HttpResponseData responseData)
					{
						if (result)
						{
							remoteBundleSizeFileDownloaded = true;
						} 
						else
						{
							if(_context.Listener != null)
							{
								_context.Listener.OnLoadAssetFailed();
							}
						}
					}, 999);
			}

			while (!remoteBundleListFileDownloaded || !remoteBundleSizeFileDownloaded) 
			{
				yield return null;
			}

			// initialise version list
			Dictionary<string,string> remoteBundleVersion = new Dictionary<string, string>();
			try
			{
				string remoteBundleListJson = AssetManager.Instance.IOtool.ReadGameAssetAsText (remoteBundleListFilePath);
				var remoteBundleVersionMap = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>> (remoteBundleListJson);
				if (remoteBundleVersionMap == null || remoteBundleVersionMap.Count == 0)
				{
					AssetManager.Instance.IOtool.DeleteGameAsset(remoteBundleListFilePath);
					LoadExistBundleFail fail = new LoadExistBundleFail("",remoteBundleListFilePath);
					EventManager.Instance.TriggerEvent(fail);
					Perf.EndSample();
					yield break;
				}
				remoteBundleVersion = remoteBundleVersionMap;
				_context.SetRemoteBundleMd5s(bundleType, remoteBundleVersionMap);
				//init bundle size
				string remoteBundleSizeJson = AssetManager.Instance.IOtool.ReadGameAssetAsText (remoteBundleSizeFilePath);
				var remoteBundleSize = AssetManager.Instance.DataParser.FromJson<Dictionary<string, long>> (remoteBundleSizeJson);
				if (remoteBundleSize == null || remoteBundleSize.Count == 0)
				{
					AssetManager.Instance.IOtool.DeleteGameAsset(remoteBundleSizeFilePath);
					LoadExistBundleFail fail = new LoadExistBundleFail("",remoteBundleSizeJson);
					EventManager.Instance.TriggerEvent(fail);
					Perf.EndSample();
					yield break;
				}
				_context.SetRemoteBundleSize(bundleType, remoteBundleSize);
			}
			catch (Exception e)
			{
				NLogger.Error("read remove md5 or bundle size failed,will restart,error {0}",e.ToString());
				AssetManager.Instance.IOtool.DeleteGameAsset(remoteBundleListFilePath);
				AssetManager.Instance.IOtool.DeleteGameAsset(remoteBundleSizeFilePath);
				LoadExistBundleFail fail = new LoadExistBundleFail(remoteBundleListFilePath,remoteBundleSizeFilePath);
				EventManager.Instance.TriggerEvent(fail);
				Perf.EndSample();
				yield break;
			}
			
			// bundle list in local
			var localBundles = new Dictionary<string, string> ();
			var localCacheBundleListFilePath = _context.GetLocalCacheBundleListFilePath (bundleType);
			var h = AssetManager.Instance.IOtool.HaveGameAsset(localCacheBundleListFilePath);
			if (h) 
			{
				string localBundleListJson = AssetManager.Instance.IOtool.ReadGameAssetAsText (localCacheBundleListFilePath);
				localBundles = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>> (localBundleListJson);
			}

            if(localBundles == null)
            {
                localBundles = new Dictionary<string, string>();
            }
			_context.SetLocalBundleMd5s(bundleType, localBundles);
			_context.RemoveUnusedLocalBundleMd5s(bundleType);

			// bundle list in package
			var packageBundles = new Dictionary<string, string> ();
			var haveBundleList = AssetManager.Instance.IOtool.HaveGameAsset(packageBundleListFilePath);
			if (haveBundleList) 
			{
				try
				{
					string packageBundleListJson = AssetManager.Instance.IOtool.ReadGameAssetAsText(packageBundleListFilePath);
					packageBundles = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(packageBundleListJson);
				}
				catch(Exception e)
				{
					NLogger.Error("load package md5 fail {0}",e.ToString());
				}
			}
			//在有文件的情况下，这里发现了序列化失败，删掉重启,终止协程,没有文件认为包是空的，没有bundle
			if (haveBundleList && (packageBundles == null || packageBundles.Count == 0))
			{
				AssetManager.Instance.IOtool.DeleteGameAsset(packageBundleListFilePath);
				EventManager.Instance.TriggerEvent(new LoadExistBundleFail(packageBundleListFilePath,packageBundleListFilePath));
				Perf.EndSample();
				yield break;
			}

			_context.SetPackageBundleMd5s(bundleType, packageBundles);

			// initialise bundle update type
			var allBundleUpdateType = new Dictionary<string, List<string>>();

			foreach(var keyValue in remoteBundleVersion)
			{
				var typeAndName = keyValue.Key.Split(new char[]{'+'}, StringSplitOptions.RemoveEmptyEntries);	
				if(typeAndName != null && typeAndName.Length == 2)
				{
					if(!allBundleUpdateType.ContainsKey(typeAndName[0]))
					{
						allBundleUpdateType.Add(typeAndName[0], new List<string>());
					}

					allBundleUpdateType[typeAndName[0]].Add(keyValue.Key);
				}
				else
				{
					if (keyValue.Key.Contains (".json")) 
					{
						if(!allBundleUpdateType.ContainsKey("static"))
						{
							allBundleUpdateType.Add ("static", new List<string>());

						}
						allBundleUpdateType["static"].Add (keyValue.Key);
					}	
					else if(keyValue.Key.Contains (".wem") || keyValue.Key.Contains (".bnk"))
					{
						if(!allBundleUpdateType.ContainsKey("ota"))
						{
							allBundleUpdateType.Add ("ota", new List<string>());

						}
						allBundleUpdateType["ota"].Add (keyValue.Key);
					}
					else if (keyValue.Key.Contains(".vfsidx") || keyValue.Key.Contains(".vfsblk"))
					{
						if (!allBundleUpdateType.ContainsKey("ota"))
						{
							allBundleUpdateType.Add("ota", new List<string>());

						}
						allBundleUpdateType["ota"].Add(keyValue.Key);
					}
					else
					{
						NLogger.ErrorChannel("AssetBundleSynchro", "invalid bundle name:{0}", keyValue.Key);
					}
				}
			}

			foreach(var keyValue in allBundleUpdateType)
			{
				_context.SetBundleUpdateType(bundleType, keyValue.Key, keyValue.Value);
			}
			
			_context.RegenerateBundleTypeMap();

			_allCoroutine.Remove(bundleType);
			Perf.EndSample();
		}
	}
}

