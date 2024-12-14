namespace NexgenDragon
{
    public interface IIOtool
    {
        string GetPersistentDataPath();
        bool AndroidHaveAssets(string relativePath);
        bool UnzipAsset(string fromRelativePath, string toRelativePath);
        bool CopyStreamingAssetToExternal(string fromRelativePath, string toRelativePath);
        byte[] ReadStreamingAsset(string relativePath);
        string ReadStreamingAssetAsText(string relativePath);
        string[] ListStreamingAssets(string relativePath);
        string[] ListDocumentAssets(string relativePath);
        bool HaveGameAssetInPackage(string relativePath);
        bool HaveGameAssetInDocument(string relativePath);
        bool HaveBundleAssetInPackage(string relativePath);
        bool HaveBundleAssetInDocument(string relativePath);
        void UpdateBundleAssetInDocument(string relativePath);
        bool HaveGameAsset(string relativePath, bool externalStorageOnly = false);
        bool DeleteGameAsset(string relativePath);
        bool DeleteGameAssetDir(string relativePath);
        byte[] ReadGameAsset(string relativePath);
        string ReadGameAssetAsText(string relativePath);
        bool WriteGameAsset(string relativePath, byte[] bytes);
        bool TestWrite1GBAsset(int size);
        bool WriteGameAsset(string relativePath, string context);
        bool WriteGameAsset(string relativePath, string context, bool encode = false);
        string GetFullGameAssetPath(string relativePath, bool externalStorageOnly = false);
        string GetExternalFullGameAssetPath(string relativePath);
        string GetLocalizedAssetName(string assetName);

        string GetLocalizedTextKey(string key);
        void ClearFuncCall();

        void GetFuncCall(out int doc, out int package, out int have);
    }
}        