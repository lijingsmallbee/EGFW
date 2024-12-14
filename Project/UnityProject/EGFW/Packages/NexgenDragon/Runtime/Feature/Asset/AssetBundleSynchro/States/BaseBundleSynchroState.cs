using System;

namespace NexgenDragon
{
	public abstract class BaseBundleSynchroState : State
	{
		protected AssetBundleSynchro _context;

		public BaseBundleSynchroState(AssetBundleSynchro context)
		{
			_context = context;
		}

		public override void Enter ()
		{
			_context.ResetSignals();
		}
	}
}

