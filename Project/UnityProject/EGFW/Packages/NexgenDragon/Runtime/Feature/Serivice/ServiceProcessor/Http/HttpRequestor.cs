using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Debug = UnityEngine.Debug;

namespace NexgenDragon
{
    public class RumStatistics
    {
        public string rumServiceName;
        public string url;
        public long responseCode;
        public int lag;
        public int requestSize;	
        public ulong responseSize;
        public string rumRequestId;
        public long sendTime;
        public long receivedTime;
        public int count;
        public string status; // 0 : success 1: discard 2: timeout 3: skip 4: failed
        public string dnsIp;
        public int useStargateAgent;
    }

    public class HttpRequestor
	{
        public struct BaseProtocol
        {
            public string jsonString;
            public object protocol;
            public long sequenceNumber;
            public object protocolExt;
        }
        public interface IRequestStrategy
		{
            bool IsConnected { get; }
			void GetRequestTimeOut(HttpRequestor requestor, out bool needRetry, out float timeOut, out bool reusePrevious);
			bool IsLockTimeout(BaseProtocol protocol, long responseCode);
			bool FixServiceToken(BaseProtocol protocol, long responseCode);
			bool IsRetryImmediately(HttpResponseData responseData, out float timeout);
            bool NeedRetryOnServerError();
			// parse protocol async?
			bool IsParseProtocolAsync();
            BaseProtocol ParseProtocol (HttpResponseData responseData);
			HttpRequestData FixRequestToken(HttpRequestData requestData);
			void SendRumStatistics(RumStatistics rumData);
            void ProcessResponse(BaseServiceParameter param, BaseProtocol protocol, long responseCode, Action<bool, BaseServiceResponse, HttpRequestor.BaseProtocol> callback, object userData);
            void OnTimeOut(HttpRequestor httpRequester);
			void ProcessProtocolBeforeDoResponse(ref HttpRequestor.BaseProtocol protocol);
            bool NeedCrcCheck(HttpRequestData requestData);
		}

		private bool _working = false;
		private IRequestStrategy _requestStrategy = null;
		private float _deltaTime = 0.0f;
		private float _maxWaitTime = 0.0f;
		private int _alreadyTryTimes = 0;
		private int _validRequestNumber = 0;
        private bool _needRetry;
        private bool _lastTimeOut;

		private string _rumServiceName = string.Empty;
		public string RumServiceName
		{
			get { return _rumServiceName; }
		}
		
		private string _rumRequestId = string.Empty;

		// private static int _requestCounter;
		// private int _requestIndex = 0;
        private readonly INetworkAgent _networkAgent;
        private readonly RtmHttpAgent _rtmHttpAgent;
        private readonly bool _useRtmAgent;
        private readonly List<IHttpAsyncOperation> _asyncOperations = new List<IHttpAsyncOperation>();

        public HttpRequestor(INetworkAgent networkAgent, string rumServiceName, string requestId, bool useRtmAgent)
		{
			// _requestIndex = _requestCounter++;
			_rumServiceName = rumServiceName;
			_rumRequestId = requestId;
            _networkAgent = networkAgent;
            _rtmHttpAgent = new RtmHttpAgent(_networkAgent.Rtm);
            _useRtmAgent = useRtmAgent;
            
            //NLogger.TraceChannel ("HttpRequestor", "HttpRequestor Consturctor: index {0} {1}", _requestIndex, _rumServiceName);
        }

		private HttpRequestData _httpRequestData;
		public HttpRequestData RequestData
		{
			get 
			{
				return _httpRequestData;
			}
		}	

		private Action<BaseProtocol, long> _callback;

		public object UserData {get; set;}
		
		public bool isDisposed { get; private set; }

		public int AlreadyTryTimes
		{
			get 
			{
				return _alreadyTryTimes;
			}
		}

		public bool IsWorking => _working;

		public void Dispose()
		{
			if (isDisposed)
			{
				return;
			}
			isDisposed = true;
			Reset();
		}

        private void Reset()
        {
			_working = false;

            AbortOperations();
        }

        private void AbortOperations()
        {
            foreach (var operation in _asyncOperations)
            {
	            try
	            {
		            operation.Abort();
	            }
	            catch (Exception e)
	            {
		            // ignored
	            }
            }

            _asyncOperations.Clear();
        }

        private IHttpApi HttpApi
        {
            get { return _useRtmAgent ? _rtmHttpAgent : _networkAgent.Http; }
        }
        
        public bool UseRtmAgent
        {
            get { return _useRtmAgent; }
        }

        public void DoRequest(HttpRequestData httpRequestData, IRequestStrategy requestStrategy, bool retry,
            Action<BaseProtocol, long> callback)
        {
	        if (isDisposed)
	        {
		        NLogger.Error("HttpRequestor.DoRequest() error, isDisposed: {0}, _rumServiceName: {1}, _rumRequestId: {2}, _useRtmAgent: {3}",
			        isDisposed, _rumServiceName, _rumRequestId, _useRtmAgent);
		        return;
	        }
            //NLogger.TraceChannel ("HttpRequestor","HttpRequestor DoRequest: index {0} {1}", _requestIndex, _rumServiceName);

            _httpRequestData = httpRequestData;
            _requestStrategy = requestStrategy;
            _callback = callback;
            _deltaTime = 0;
            _maxWaitTime = 0;
            _alreadyTryTimes = 0;
            _needRetry = retry;

            bool needRetry = false;
            bool reusePrevious = false;
            float timeOut = 0.0f;

            _requestStrategy.GetRequestTimeOut(this, out needRetry, out timeOut, out reusePrevious);

            DoRequestInternal(timeOut, false, false);
        }

        public void Tick(float delta)
		{
			if (isDisposed)
			{
				return;
			}
			if(_working)
			{
				_deltaTime += delta;

                var isTimeOut = _deltaTime >= _maxWaitTime;
				if(isTimeOut)
				{
                    if (_needRetry)
                    {
                        if (!_lastTimeOut)
                        {
                            _requestStrategy.OnTimeOut(this);
                        }
                        
                        if (_requestStrategy.IsConnected)
                        {
                            bool needRetry;
                            bool reusePrevious;
                            float timeOut;
                        
                            _requestStrategy.GetRequestTimeOut(this, out needRetry, out timeOut, out reusePrevious);
                            if(needRetry)
                            {
                                DoRequestInternal(timeOut, reusePrevious, true);
                            }
                            else
                            {
                                OnResponse(_validRequestNumber, null);
                            
                                var stats = new RumStatistics();
                                stats.rumServiceName = _rumServiceName;
                                stats.url = _httpRequestData.url;
                                stats.rumRequestId = _rumRequestId;
                                stats.status = "timeout";
                                stats.count = _alreadyTryTimes;
                                stats.useStargateAgent = _useRtmAgent ? 1 : 0;

                                _requestStrategy.SendRumStatistics(stats);
                            }
                        }
                    }
                    else
                    {
                        OnResponse(_validRequestNumber, null);
                        
                        var stats = new RumStatistics();
                        stats.rumServiceName = _rumServiceName;
                        stats.url = _httpRequestData.url;
                        stats.rumRequestId = _rumRequestId;
                        stats.status = "timeout";
                        stats.count = _alreadyTryTimes;
                        stats.useStargateAgent = _useRtmAgent ? 1 : 0;

                        _requestStrategy.SendRumStatistics(stats);
                    }
				}

                _lastTimeOut = isTimeOut;
            }
		}

        private void DoRequestInternal(float timeOut, bool reusePrevious, bool isRetry)
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			_working = true;
			_alreadyTryTimes ++;
            _deltaTime = 0;
			_maxWaitTime = timeOut;
            _lastTimeOut = false;

			if(!reusePrevious)
			{
				_validRequestNumber++;
			}

			int requestNumber = _validRequestNumber;

			long sendTime = ServerTime.Instance.RealServerTimestampInMilliseconds;
		    if (isRetry)
		    {
		        _httpRequestData.headersDict["Client-Retry"] = "1";
#if UNITY_EDITOR
		        if (_httpRequestData.requestContent != null)
		        {
			        Debug.LogError(string.Format("Retry RequestNumber:{0} RetryTimes:{1} Time:{2} Content:{3}", requestNumber,
				        _alreadyTryTimes, sendTime,System.Text.Encoding.UTF8.GetString(_httpRequestData.requestContent)));
		        }
#endif
		    }

		    if (_httpRequestData.crcCheckValue > 0 && !_requestStrategy.NeedCrcCheck(_httpRequestData))
		    {
			    _httpRequestData.crcCheckValue = 0;
		    }

		    _httpRequestData.isRetry = isRetry;

		    var count = _alreadyTryTimes;

		    var operation = HttpApi.Send(_httpRequestData, delegate(HttpResponseData httpResponseData)
			{
				if (isDisposed)
				{
					return;
				}
				stopWatch.Stop();
				
				long receivedTime = ServerTime.Instance.RealServerTimestampInMilliseconds;

				int requestSize = 0;

				//NLogger.TraceChannel ("HttpRequestor", "HttpRequestor Response: index {0} {1}", _requestIndex, _rumServiceName);

				if(_httpRequestData.requestContent != null)
				{
					requestSize = _httpRequestData.requestContent.Length;
				}

                var stats = new RumStatistics();
                stats.rumServiceName = _rumServiceName;
                stats.url = _httpRequestData.url;
                stats.responseCode = httpResponseData.responseCode;
                stats.lag = (int)stopWatch.ElapsedMilliseconds;
                stats.requestSize = requestSize;
                stats.responseSize = httpResponseData.downloadedBytes;
                stats.rumRequestId = _rumRequestId;
                stats.sendTime = sendTime;
                stats.receivedTime = receivedTime;
                stats.count = count;
                stats.useStargateAgent = _useRtmAgent ? 1 : 0;

                if(httpResponseData.responseCode == 200)
				{
					var status = OnResponse(requestNumber, httpResponseData);
					stats.status = status;
				}
				else
                {
                    var isServerError = httpResponseData.responseCode / 500 == 1;
                    if (isServerError && !_requestStrategy.NeedRetryOnServerError())
                    {
                        var protocol = _requestStrategy.ParseProtocol(httpResponseData);
                    
                        _callback.Invoke (protocol, httpResponseData.responseCode);
                        stats.status = "serverError";

                        Reset();
                        Debug.LogError(string.Format("MultiTimes Request Server Error Response Code:{0}",
	                        httpResponseData.responseCode));
                    }
                    else
                    {
	                    if (_httpRequestData.requestContent != null)
	                    {
		                    Debug.LogError(string.Format("MultiTimes Request Error.Response Code:{0}, Request:{1}",
			                    httpResponseData.responseCode, System.Text.Encoding.UTF8.GetString(_httpRequestData.requestContent)));
	                    }
	                    else
	                    {
		                    Debug.LogError(string.Format("MultiTimes Request Error Response Code:{0}",
			                    httpResponseData.responseCode));
	                    }
                        // make it retry next tick, or wait until time out
                        float timeout;
                        if(_requestStrategy.IsRetryImmediately(httpResponseData, out timeout))
                        {
                            _deltaTime = 0;
                            _maxWaitTime = timeout;
                        }
					
                        stats.status = "failed";
                    }
				}
                
                _requestStrategy.SendRumStatistics(stats);
			});

            if (operation != null)
            {
                _asyncOperations.Add(operation);
            }
		}

        private string OnResponse(int requestNumber, HttpResponseData httpResponseData)
		{
			if(!_working)
			{
				//NLogger.LogChannel ("HttpRequestor","already have one response, skip this one");
				return "skip";
			}

			if(requestNumber != _validRequestNumber)
			{
				//NLogger.LogChannel ("HttpRequestor","abondand request, skip this one");
				return "discard";
			}

			var responseCode = (httpResponseData != null) ? httpResponseData.responseCode : 0;

			if (_requestStrategy.IsParseProtocolAsync())
			{
				// async parse json data
				// TaskManager.Instance.RunAsync(() =>
				var taskPool = TaskPoolManager.Instance.Service;
				taskPool.RunAsync(() =>
				{
					if (isDisposed)
					{
						return;
					}
					BaseProtocol protocol = _requestStrategy.ParseProtocol(httpResponseData);

					// do in main queue
					//TaskManager.Instance.QueueOnMainThread(() =>
					taskPool.QueueOnMainThread(() =>
					{
						DoResponse(protocol, responseCode);
					});
				});
			}
			else
			{
				// sync parse
				BaseProtocol protocol = _requestStrategy.ParseProtocol(httpResponseData);
				
				DoResponse(protocol, responseCode);
			}
			
			return httpResponseData != null ? "success" : "timeout";
		}

		/// do response
		private void DoResponse(BaseProtocol protocol, long responseCode)
		{
			if (isDisposed)
			{
				return;
			}
			
			_requestStrategy.ProcessProtocolBeforeDoResponse(ref protocol);
			if (_requestStrategy.IsLockTimeout (protocol, responseCode))
			{
			    //NLogger.LogChannel(UnityEngine.Color.blue, "HttpRequestor", "Retry When Lock Timeout.");
			    return;
			}

			if (_requestStrategy.FixServiceToken (protocol, responseCode))
			{
			    //NLogger.LogChannel(UnityEngine.Color.blue, "HttpRequestor", "Retry After Service Token Fixed.");
			    // HttpRequestProcessor.LogPauseState("Client Need FixServiceToken");
			    var requestData = _requestStrategy.FixRequestToken(_httpRequestData);
			    DoRequest(requestData, _requestStrategy, _needRetry, _callback);
				return;
			}

			_working = false;
			_callback.Invoke (protocol, responseCode);

			Reset();
		}
	}
}

