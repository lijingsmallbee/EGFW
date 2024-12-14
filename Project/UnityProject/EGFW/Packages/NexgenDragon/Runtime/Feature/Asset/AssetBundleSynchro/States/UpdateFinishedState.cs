using System;

namespace NexgenDragon
{
	public class UpdateFinishedState : BaseBundleSynchroState
	{
		public UpdateFinishedState (AssetBundleSynchro context):base(context)
		{

		}

		public override void Enter ()
		{
			base.Enter();

			NLogger.LogChannel("AssetBundleSynchro", "UpdateFinishedState:Enter");

			var allBundleType = _context.AllBundleType;

			foreach(var bundleType in allBundleType)
			{
				_context.RemoteUnusedBundleFiles(bundleType);
			}

			if(_context.Listener != null)
			{
				_context.Listener.OnUpdateFinished();
			}
		}

		public override void Tick (float delta)
		{

		}

		public override void Exit ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "UpdateFinishedState:Exit");

		}

	}
}

