using System;

namespace NexgenDragon
{
    public interface IDownLoader
    {
        void DownloadAsset(string url, string savePath, Action<bool, HttpResponseData> callback,
            int priority = 999, uint crcCheckValue = 0,uint fileSize = 0);
    }
}