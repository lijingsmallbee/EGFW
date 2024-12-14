using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	public class RtmServiceProcessor : BaseServiceProcessor<string, byte[]>
	{
		protected RtmConnector _rtmConnector = null;

        public RtmServiceProcessor(INetworkAgent networkAgent)
        {
            _rtmConnector = new RtmConnector(networkAgent, OnReceived);
        }

		public void Connect(string host, int port, object param)
		{
            _rtmConnector.Connect(host, port, param);
		}

		public void Disconnect()
		{
			_rtmConnector.Disconnect();
		}

        public bool IsRtmReady
		{
            get { return _rtmConnector.IsRtmReady; }
        }

        public void GetOnlineUsers(List<long> uids,Action<List<long>> callback)
        {
	        var stopWatch = new Stopwatch();
	        stopWatch.Start();
			var stats = new RumStatistics();
			stats.rumServiceName = "Watcher:GetOnlineUsers";
			stats.url = "";
			stats.responseCode = 200;
			stats.requestSize = 0;
			stats.responseSize = 0;
			stats.rumRequestId = "";
			stats.sendTime = ServerTime.Instance.RealServerTimestampInMilliseconds;
			stats.count = 1;
			stats.status = "success";

			_rtmConnector.GetOnlineUsers(uids, delegate(List<long> list)
			{
				if (callback != null)
				{
					callback(list);	
				}
				
				stats.receivedTime = ServerTime.Instance.RealServerTimestampInMilliseconds;
				stats.lag = (int)stopWatch.ElapsedMilliseconds;
				_rtmConnector.GetStrategy().SendRumStatistics(stats);	
			});
		}

		public void AddRtmConnectCallback(Action callback)
		{
			_rtmConnector.AddRtmConnectCallback (callback);
		}

		public void RemoveRtmConnectCallback(Action callback)
		{
			_rtmConnector.RemoveRtmConnectCallback (callback);
		}

		public void AddRtmKickOutCallback(Action callback)
		{
			_rtmConnector.AddRtmKickOutCallback (callback);
		}

		public void RemoveRtmKickOutCallback(Action callback)
		{
			_rtmConnector.RemoveRtmKickOutCallback (callback);
		}

		public void AddRtmDisconnectCallback(Action callback)
		{
			_rtmConnector.AddRtmDisconnectCallback (callback);
		}

		public void RemoveRtmDisconnectCallback(Action callback)
		{
			_rtmConnector.RemoveRtmDisconnectCallback (callback);
		}

		public void AddRtmConnectFailedCallback(Action callback)
		{
			_rtmConnector.AddRtmConnectFailedCallback (callback);
		}

		public void RemoveRtmConnectFailedCallback(Action callback)
		{
			_rtmConnector.RemoveRtmConnectFailedCallback (callback);
		}

	    public bool CheckRtmAlive()
	    {
	        return _rtmConnector.CheckRtmAlive();
	    }

		public void SetRequestStrategy(RtmConnector.IRtmConnectStrategy connectStrategy)
		{
			_rtmConnector.SetRequestStrategy(connectStrategy);
		}

		public override void Release()
		{
			base.Release();
			_rtmConnector.Disconnect();
			_rtmConnector.Release();
		}

		public override void Reset()
		{
			_rtmConnector.Disconnect();
            _rtmConnector.Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            _rtmConnector.Initialize();
        }

        public override void Tick (float delta)
		{
			Perf.BeginSample("VinayGao:RtmServiceProcessor.Tick");
			_rtmConnector.Tick(delta);
			TickRequest();
			TickResponse();
			Perf.EndSample();
		}

		protected void TickRequest()
		{
			
		}

		protected void TickResponse()
		{
			lock(_responseQueue)
			{
				Perf.BeginSample("VinayGao:RtmServiceProcessor.TickResponse");
				while(_responseQueue.Count > 0)
				{
                    var workload = _responseQueue.Dequeue();
                    var received = workload.response;
                    var receivedString = workload.responseString;

                    var strategy = _rtmConnector.GetStrategy();
                    strategy.ProcessResponse(received,receivedString, workload.isHistory, workload.msgId, workload.typeCode);
                    
				}
				Perf.EndSample();
			}
		}

        protected void OnReceived(long msgId, byte[] received,string receivedString, bool isHistory, int codeType)
		{
			InsertResponse(received,receivedString, isHistory, msgId, codeType);
		}
	}
}

