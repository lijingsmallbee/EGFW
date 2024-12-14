using System;

namespace NexgenDragon
{
	public class StaticUpdateCompleteCondition : BaseBundleSynchroCondition
	{
		public StaticUpdateCompleteCondition (AssetBundleSynchro context):base(context)
		{

		}

		public override bool IsSatisfied ()
		{
			return _context.UpdateStaticBundleComplete;
		}
	}
}

