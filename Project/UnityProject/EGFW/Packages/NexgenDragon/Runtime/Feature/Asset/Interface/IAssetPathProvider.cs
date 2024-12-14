namespace NexgenDragon
{
    public interface IAssetPathProvider
    {
        bool IsBundleInfoConfig(string fileName);
        string GetPlatformPath();
        string GetBundleConfigPath(string bundleType, string fileName);
        string GetLocalBundleFilePath(string bundleType, string bundleName);
        string GetLocalBundleFolderPath(string bundleType);
        string GetLocalRelatedBundlePath(string bundleType, string platform, string bundleName);
        string GetLocalBundleListFilePath(string bundleType);
        string GetPackageLocalBundleListFilePath(string bundleType);
        string GetLocalBundleSizeFilePath(string bundleType);
        string GetPackageLocalBundleSizeFilePath(string bundleType);
        string GetLocalCacheBundleListFilePath(string bundleType);
        string GetPackageBundleListFilePath(string bundleType);
        string GetPackageBundleSizeFilePath(string bundleType);
        string GetBundleLocalPath(string bundleType, string bundleName);
        string GetBundlePath(string fileName, string hashName = null);
        string GetCustomBundlePath(string fileName);
        string GetRemoteBundleUrl(string RemoteCDN, string bundleType, string version, string bundleName);
        string GetRemoteBundleUrl(string RemoteCDN, string bundleType, string version, string bundleName, string plateform);
        string GetRemoteBundleListFilePath(string bundleType, string version);
        string GetRemoteBundleSizeFilePath(string bundleType, string version);
        string GetRemoteBundleTypeFilePath(string bundleType, string version);
        string BUNDLE_VERSION_FILE();
    }
}