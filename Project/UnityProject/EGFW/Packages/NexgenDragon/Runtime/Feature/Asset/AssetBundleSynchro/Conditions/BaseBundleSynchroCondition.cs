using System;

namespace NexgenDragon
{
	public abstract class BaseBundleSynchroCondition : BaseCondition
	{
		protected AssetBundleSynchro _context;
	
		public BaseBundleSynchroCondition(AssetBundleSynchro context)
		{
			_context = context;
		}
		
	}
}

