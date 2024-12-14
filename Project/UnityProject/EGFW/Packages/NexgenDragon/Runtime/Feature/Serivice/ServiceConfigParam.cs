namespace NexgenDragon
{
    public class ServiceManagerConfig : NexgenObject
    {
        public INetworkAgent NetworkAgent;
        public IServiceUILocker UILocker;
		public IServiceSceneLocker SceneLocker;
		public HttpRequestor.IRequestStrategy RequestStrategy;
		public HttpRequestor.IRequestStrategy LoaderStrategy;
		public HttpRequestor.IRequestStrategy AssetStrategy;
	    public RtmConnector.IRtmConnectStrategy RtmConnectStrategy;
        public HttpRequestor.IRequestStrategy RtmStrategy;
	    public HttpRequestor.IRequestStrategy ConfigStrategy;
        public ServiceExceptionCallback OnException;
	    public bool IsLowMemoryDevice;
    }
}

