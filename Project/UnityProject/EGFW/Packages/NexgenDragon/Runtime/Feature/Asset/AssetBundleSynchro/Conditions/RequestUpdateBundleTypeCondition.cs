using System;

namespace NexgenDragon
{
	public class RequestUpdateBundleTypeCondition : BaseBundleSynchroCondition
	{
		public RequestUpdateBundleTypeCondition (AssetBundleSynchro context):base(context)
		{

		}

		public override bool IsSatisfied ()
		{
			return _context.UpdateVersionListComplete;
		}
	}
}

