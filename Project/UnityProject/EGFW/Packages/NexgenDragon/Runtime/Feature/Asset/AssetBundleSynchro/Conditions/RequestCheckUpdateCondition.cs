using System;

namespace NexgenDragon
{
	public class RequestCheckUpdateCondition : BaseBundleSynchroCondition
	{
		public RequestCheckUpdateCondition (AssetBundleSynchro context):base(context)
		{
			
		}

		public override bool IsSatisfied ()
		{
			return _context.RequestCheckUpdate;
		}
	}
}

