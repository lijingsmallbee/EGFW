using System.IO;
public class BundleAssetData
{
    public BundleAssetData(string assetPath,string bundleName)
    {
        BundleName = bundleName;
        AssetName = Path.GetFileName(assetPath);
    }

    public string AssetName { get; }

    public string BundleName { get; }
}
