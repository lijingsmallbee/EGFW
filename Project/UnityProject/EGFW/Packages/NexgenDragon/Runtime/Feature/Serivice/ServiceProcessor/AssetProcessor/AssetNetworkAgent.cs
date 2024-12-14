namespace NexgenDragon
{
    public class AssetNetworkAgent : INetworkAgent
    {
        private readonly AssetHttpApi _httpApi = new AssetHttpApi();
        
        public IHttpApi Http
        {
            get { return _httpApi; }
        }

        public IRtmApi Rtm
        {
            get { return null; }
        }

        public bool UseRtmHttpAgent
        {
            get { return false; }
        }

        public void Update(float delta)
        {
            _httpApi.Tick(delta);
        }

        public void Reset()
        {
            _httpApi.Reset();
        }
    }
}