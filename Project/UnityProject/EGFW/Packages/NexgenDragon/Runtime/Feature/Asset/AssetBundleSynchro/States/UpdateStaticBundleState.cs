using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace NexgenDragon
{
	[Obsolete("这个功能在SyncAssetBundleState中实现了")]
	public class UpdateStaticBundleState : BaseBundleSynchroState
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

		private long _total = 0L;

		long TotalByte
		{
			get { return _total; }
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

		public UpdateStaticBundleState (AssetBundleSynchro context):base(context)
		{

		}

		public override void Enter ()
		{
			base.Enter();

			NLogger.LogChannel("AssetBundleSynchro", "UpdateStaticBundleState:Enter");

			_allFinishedCount.Clear ();
			_allTotalCount.Clear ();

			var allBundleType = _context.AllBundleType;

			foreach(var bundleType in allBundleType)
			{
				var total = RequestDownloadStaticBundles(bundleType, _context.GetRemoteVersion(bundleType));
				_total += total;
			}
		}

		public override void Tick (float delta)
		{
			if(FinishedCount == TotalCount)
			{
				_context.UpdateStaticBundleComplete = true;
			}

			if(_context.Listener != null)
			{
				_context.Listener.OnUpdateStaticBundleProgress(FinishedCount, TotalCount,TotalByte);
			}
		}

		public override void Exit ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "UpdateStaticBundleState:Exit");

			bool haveAssetChanged = TotalCount > 0;
			if(haveAssetChanged)
			{
				AssetManager.Instance.UnloadUnused();				
			}

			if(_context.Listener != null)
			{
				_context.Listener.OnUpdateStaticBundleFinished(haveAssetChanged);
			}
		}

		long RequestDownloadStaticBundles(string bundleType, string remoteVersion)
		{	
			var allStaticBundles = _context.GetBundlesWithUpdateType(bundleType, "static");

			var allNeedSync = new List<string> ();
			
			allNeedSync.AddRange (allStaticBundles);

#if MO_LOG || UNITY_DEBUG || MO_DEBUG
			_context.Tag = $"{this}.RequestDownloadStaticBundles.{bundleType}";
#endif
			var total= _context.SyncAssetBundlesEx (allNeedSync, null, delegate(int finishedCount, int totalCount, long byteCount,long totalByte) {
				SetFinishedCount(bundleType, finishedCount);
				SetTotalCount(bundleType, totalCount);
				NLogger.LogChannel("AssetBundleSynchro", "Static Task {0} {1}/{2}", bundleType, finishedCount, totalCount);
			});
			return total;
		}
	}
}

