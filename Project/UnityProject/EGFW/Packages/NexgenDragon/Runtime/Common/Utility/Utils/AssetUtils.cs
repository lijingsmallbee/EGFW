using System;

namespace NexgenDragon
{
    public static class AssetUtils
    {
        public delegate AssetHandle LoadAssetHandler(string path);
        public delegate AssetHandle LoadAssetAsyncHandler(string path, Action<bool, AssetHandle> callback, int priority, bool asap);
        public delegate void UnloadAssetHandler(AssetHandle handle);

        public delegate void DecompressLzmaHandler(string input, string outPut, Action<bool> success);

        // TODO: 
        public static LoadAssetHandler LoadAsset;
        public static LoadAssetAsyncHandler LoadAssetAsync;
        public static UnloadAssetHandler UnloadAsset;
        public static DecompressLzmaHandler DecompressLzma;
    }
}
