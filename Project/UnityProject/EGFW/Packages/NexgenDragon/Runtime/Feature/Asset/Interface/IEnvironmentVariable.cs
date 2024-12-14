namespace NexgenDragon
{
    public interface IEnvironmentVariable
    {
        bool UNITY_EDITOR();
        bool USE_BUNDLE_IOS();
        bool USE_BUNDLE_ANDROID();
        bool USE_BUNDLE_STANDALONE();
        bool UNITY_ANDROID();        
        bool UNITY_WINDOWS();
        bool ASSET_DEBUG();
        bool UNITY_DEBUG();
        bool USE_SBP();
    }
}