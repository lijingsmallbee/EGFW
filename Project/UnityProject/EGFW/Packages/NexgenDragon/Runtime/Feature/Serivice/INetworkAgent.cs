namespace NexgenDragon
{
    public interface INetworkAgent
    {
        IHttpApi Http { get; }
        IRtmApi Rtm { get; }
        bool UseRtmHttpAgent { get; }
    }
}
