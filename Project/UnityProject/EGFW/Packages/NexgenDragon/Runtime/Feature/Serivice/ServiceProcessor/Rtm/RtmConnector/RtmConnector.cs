using System;
using System.Collections.Generic;

namespace NexgenDragon
{
	public partial class RtmConnector : NexgenObject
	{
		public enum ConnectOperation
		{
			Connect,
			HoldOn,
			Break,
		}
		
		// 需要线程安全
		public interface IRtmConnectStrategy
		{
			void Reset();
			ConnectOperation GetConnectOperation(float dt);
			void OnError();
            void ProcessResponse(byte[] received,string receivedString, bool isHistory, long msgId, int typeCode);
            void SendRumStatistics(RumStatistics rumData);
		}
		private string vinayGaoClassType = null;
		public string VinayGaoGetClassType
		{
			get
			{
				if (vinayGaoClassType == null)
				{
					vinayGaoClassType ="VinayGao:" + GetType().Name;
				}

				return vinayGaoClassType;
			}
		}
        private string _host;
        private int _port;
        private object _params;

        protected event Action ConnectionEvent;
		protected event Action KickOutEvent;
		protected event Action DisconnectEvent;
		protected event Action ConnectFailedEvent;
        protected event Action<long, byte[],string, bool, int> ReceivedCallback;

        private readonly StateMachine _stateMachine;
        private readonly INetworkAgent _networkAgent;
        private IRtmConnectStrategy _connectStrategy;
		
        public RtmConnector(INetworkAgent networkAgent, Action<long, byte[],string, bool, int> receivedCallback)
		{
			ReceivedCallback = receivedCallback;
            _networkAgent = networkAgent;

			_stateMachine = new StateMachine();
            _stateMachine.AddState(new NotStartState(this));
            _stateMachine.AddState(new ConnectState(this));
            _stateMachine.AddState(new WorkingState(this));
            _stateMachine.AddState(new KickOutState(this));
			_stateMachine.ChangeState(typeof(NotStartState));
		}

		public void SetRequestStrategy(IRtmConnectStrategy connectStrategy)
		{
			_connectStrategy = connectStrategy;
		}

        public IRtmConnectStrategy GetStrategy()
        {
            return _connectStrategy;
        }

		public void Connect(string host, int port, object param)
		{
			_host = host;
			_port = port;
            _params = param;
            
            // 跳转到链接状态
            _stateMachine.ChangeState(typeof(ConnectState));
		}

        public void Disconnect()
        {
            _stateMachine.ChangeState(typeof(NotStartState));
        }

        public bool IsRtmReady
		{
			get
			{
				return (_stateMachine.CurrentState is WorkingState);
			}
		}

        public void GetOnlineUsers(List<long> uids,Action<List<long>> callback)
		{
			RtmApi.GetOnlineUsers(uids, callback);
		}

		public void AddRtmConnectCallback(Action callback)
		{
			ConnectionEvent += callback;
		}

		public void RemoveRtmConnectCallback(Action callback)
		{
			ConnectionEvent -= callback;
		}

		public void AddRtmConnectFailedCallback(Action callback)
		{
			ConnectFailedEvent += callback;
		}

		public void RemoveRtmConnectFailedCallback(Action callback)
		{
			ConnectFailedEvent -= callback;
		}

		public void AddRtmKickOutCallback(Action callback)
		{
			KickOutEvent += callback;
		}

		public void RemoveRtmKickOutCallback(Action callback)
		{
			KickOutEvent -= callback;
		}

		public void AddRtmDisconnectCallback(Action callback)
		{
			DisconnectEvent += callback;
		}

		public void RemoveRtmDisconnectCallback(Action callback)
		{
			DisconnectEvent -= callback;
		}

	    public bool CheckRtmAlive()
	    {
	        return RtmApi.CheckAlive();
	    }

	    public void Tick(float delta)
		{
			_stateMachine.Tick(delta);
		}

		public override void Release ()
		{
			base.Release();

			ConnectionEvent = null;
			KickOutEvent = null;
			DisconnectEvent = null;
			ConnectFailedEvent = null;
			ReceivedCallback = null;
		}
		
		// 建立长连接
        private void DoConnect()
        {
            RtmApi.Connect(_host, _port, _params);
        }

		// 事件触发
        private void OnFireConnectionEvent()
		{
			if (ConnectionEvent != null)
			{
				ServiceManager.Instance.reconnectParamsInEvent = _params;
                ConnectionEvent();
                ServiceManager.Instance.reconnectParamsInEvent = null;
			}
		}

        private void OnFireKickOutEvent()
		{
			if (KickOutEvent != null)
            {
	            ServiceManager.Instance.reconnectParamsInEvent = _params;
                KickOutEvent();
                ServiceManager.Instance.reconnectParamsInEvent = null;
            }
		}

        private void OnFireDisconnectEvent()
		{
			if (DisconnectEvent != null)
            {
	            ServiceManager.Instance.reconnectParamsInEvent = _params;
                DisconnectEvent();
                ServiceManager.Instance.reconnectParamsInEvent = null;
            }
		}

        private void OnFireConnectFailedEvent()
		{
			if (ConnectFailedEvent != null)
			{
				ServiceManager.Instance.reconnectParamsInEvent = _params;
				ConnectFailedEvent();
				ServiceManager.Instance.reconnectParamsInEvent = null;
			}
		}

        private void OnFireReceivedEvent(long msgId, byte[] received,string receivedString, bool isHistory, int codeType)
		{
			if(ReceivedCallback != null)
			{
				ServiceManager.Instance.reconnectParamsInEvent = _params;
				ReceivedCallback(msgId, received,receivedString, isHistory, codeType);
				ServiceManager.Instance.reconnectParamsInEvent = null;
			}
		}

        private IRtmApi RtmApi
		{
            get { return _networkAgent.Rtm; }
        }

        public void Initialize()
        {
            RtmApi.Initialize();
        }
    }
}

