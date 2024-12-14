using System;

namespace NexgenDragon
{
	public class IdleState : BaseBundleSynchroState
	{
		public override void Enter ()
		{
			base.Enter();

			NLogger.LogChannel("AssetBundleSynchro", "IdleState:Enter");
		}

		public override void Tick (float delta)
		{
			
		}

		public override void Exit ()
		{
			NLogger.LogChannel("AssetBundleSynchro", "IdleState:Exit");

		}

		public IdleState (AssetBundleSynchro context):base(context)
		{
			
		}
	}
}

