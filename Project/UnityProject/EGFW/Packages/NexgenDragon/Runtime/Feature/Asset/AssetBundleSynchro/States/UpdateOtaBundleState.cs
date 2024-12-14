using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
	[Obsolete("这个功能在PrepareOtaState中实现了")]
	public class UpdateOtaBundleState : BaseBundleSynchroState
	{
		Dictionary<string, int> _allFinishedCount = new Dictionary<string, int>();
		Dictionary<string, int> _allTotalCount = new Dictionary<string, int>();

		void SetFinishedCount(string bundleType, int finishedCount)
		{
			if (_allFinishedCount.ContainsKey (bundleType)) {
				_allFinishedCount [bundleType] = finishedCount;
			}
			else {
				_allFinishedCount.Add (bundleType, finishedCount);
			}
		}

		void SetTotalCount(string bundleType, int finishedCount)
		{
			if (_allTotalCount.ContainsKey (bundleType)) {
				_allTotalCount [bundleType] = finishedCount;
			}
			else {
				_allTotalCount.Add (bundleType, finishedCount);
			}
		}

		int FinishedCount 
		{
			get 
			{
				int result = 0;
				foreach (var keyValue in _allFinishedCount) 
				{
					result += keyValue.Value;
				}
				return result;
			}
		}

		int TotalCount 
		{
			get 
			{
				int result = 0;
				foreach (var keyValue in _allTotalCount) 
				{
					result += keyValue.Value;
				}
				return result;
			}
		}

		public UpdateOtaBundleState (AssetBundleSynchro context):base(context)
		{

		}

		protected bool _alreadyRequestDownLoad = false;
		public override void Enter ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "UpdateOtaBundleState:Enter");

			base.Enter();

			_allFinishedCount.Clear ();
			_allTotalCount.Clear ();

			_alreadyRequestDownLoad = false;
		}

		public override void Tick (float delta)
		{
			if (!_context.RequestUpdateOta)
			{
				return;			
			}
			 
			if(!_alreadyRequestDownLoad)
			{
				NLogger.LogChannel("AssetBundleSynchro", "UpdateOtaBundleState:StartDownload");

				_alreadyRequestDownLoad = true;

				var allBundleType = _context.AllBundleType;

				foreach(var bundleType in allBundleType)
				{
					RequestDownloadOtaBundles(bundleType, _context.GetRemoteVersion(bundleType));
				}
			}

			if(_alreadyRequestDownLoad && TotalCount == FinishedCount)
			{
				_context.UpdateOtaBundleComplete = true;
			}
		}

		public override void Exit ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "UpdateOtaBundleState:Exit");

			if(_context.Listener != null)
			{
				_context.Listener.OnUpdateOtaBundleFinished();
			}
		}

        List<string> GetBundleWithKeyName(List<string> bundles,string keyName,List<string> resultStore)
        {
            var tempList = new List<string>(256);
            var tempList2 = new List<string>(256);
            foreach(var bundle in bundles)
            {
                if(bundle.Contains(keyName))
                {
                    tempList.Add(bundle);
                }
                else
                {
                    tempList2.Add(bundle);
                }
            }
            resultStore.AddRange(tempList);
            return tempList2;
        }

        void GetBundlesInLoaded(List<string> bundles, List<string> resultStore)
        {
            var list = AssetBundleConfig.Instance.GetOtaLoadedBundles();
            if(list != null)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    var bundleName = list[i];
                    if (bundleName.Contains("cache"))
                    {
                        continue;
                    }
                    else
                    {
                        if (bundles.Contains(bundleName))
                        {
                            resultStore.Add(bundleName);
                        }
                    }
                }
            }
        }

		void RequestDownloadOtaBundles(string bundleType, string remoteVersion)
		{	
			var allOtaBundles = _context.GetBundlesWithUpdateType(bundleType, "ota");

			var processed = _context.Processor.FillterOtaBundleList(allOtaBundles);
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
			_context.Tag = $"{this}.RequestDownloadOtaBundles.{bundleType}";
#endif
			_context.SyncAssetBundles (processed, null, delegate(int finishedCount, int totalCount, long byteCount,long totalByte) {
				SetFinishedCount (bundleType, finishedCount);
				SetTotalCount (bundleType, totalCount);

 				NLogger.LogChannel ("AssetBundleSynchro", "Ota Task:{0} {1}/{2}", bundleType, finishedCount, totalCount);
			}, -1);
		}
	}
}

