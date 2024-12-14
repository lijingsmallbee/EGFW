using System;
using System.Collections.Generic;

namespace NexgenDragon
{
    public class RtmData
    {
        public Dictionary<string, string> Header = new Dictionary<string, string>();
        public byte[] Body;
    }

    public enum DisconnectReason
    {
	    Normal_Release,
	    Normal_DisconnectBeforeConnect,
	    Normal_ClientNull,
	    Error_MaxTryTimes,
	    Error_OnConnect,
	    Error_DNSParse,
	    Error_DNSAddressNull,
	    Error_OnWatcherReConnect,
	    Error_DNSAndConnectTimeout,
	    Error_ConnectTimeout,
	    Error_PingPongTimeout,
	    Exception_Connect,
	    Exception_SocketRead,
	    Exception_SocketReadLessThan1,
	    Exception_SocketSend,
	    Exception_SocketSendSetLength0,
    }

	public interface IRtmApi : IObject
	{
        event Action<long, byte[],string, bool, int> OnReceived;
		event Action OnConnected;
		event Action OnDisconnected;
		event Action<RtmError> OnError;
		event Action OnKickedOut;

		RtmState State { get; }
		
		string IPAddress { get; }

        void Initialize();
        void Connect(string host, int port, object param);
        void Disconnect(DisconnectReason reason);
		void GetOnlineUsers (List<long> uids, Action<List<long>> callback);
        void Send(RtmData data, Action<RtmData, int> action);
	    bool CheckAlive();
        void Tick(float dt);
	}
}
