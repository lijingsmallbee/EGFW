using System;

namespace NexgenDragon
{
	public class OtaUpdateCompleteCondition : BaseBundleSynchroCondition
	{
		public OtaUpdateCompleteCondition (AssetBundleSynchro context):base(context)
		{

		}

		public override bool IsSatisfied ()
		{
			return _context.UpdateOtaBundleComplete;
		}
	}
}



