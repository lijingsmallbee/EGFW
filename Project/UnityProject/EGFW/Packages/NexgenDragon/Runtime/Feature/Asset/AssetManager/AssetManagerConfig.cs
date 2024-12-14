namespace NexgenDragon
{
    public class AssetManagerConfig:NexgenObject
    {
        public IDataParser dataParser;
        public IAssetPathProvider assetPathProvider;
        public IEnvironmentVariable environmentVariable;
        public IIOtool IOtool;
        public BaseAssetDatabaseLoader assetbaDatabaseLoader;
        public IDownLoader downLoader;
        public bool IsLowMemoryDevice = true;
    }
}