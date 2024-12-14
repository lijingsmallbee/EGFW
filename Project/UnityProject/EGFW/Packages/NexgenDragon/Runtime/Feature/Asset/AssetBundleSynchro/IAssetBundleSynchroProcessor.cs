using System.Collections.Generic;

namespace NexgenDragon
{
    public interface IAssetBundleSynchroProcessor
    {
        List<string> FillterOtaBundleList(List<string> origin);
    }
}