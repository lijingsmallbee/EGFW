using System.Collections.Generic;

namespace NexgenDragon
{
	public class AssetBundleSynchroConfig : NexgenObject
	{
		public List<string> AllBundleType = new List<string>();
		public IAssetBundleSynchroProcessor Processor;
	}
}


