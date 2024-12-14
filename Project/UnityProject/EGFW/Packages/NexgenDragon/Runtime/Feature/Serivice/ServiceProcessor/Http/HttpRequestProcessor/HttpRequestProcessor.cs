#define HTTP_REQUEST_PROCESSOR_LOG_ENABLED

using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	public class HttpRequestProcessor : BaseServiceProcessor<HttpRequestRequest, HttpRequestResponse>
	{
        private HttpRequestRequest _currentRequest;
        private readonly INetworkAgent _networkAgent;

        private long _sequenceNumber;
        private bool _pauseRequest;

        private long GenerateSequenceNumber()
        {
            _sequenceNumber++;
            return _sequenceNumber;
        }

        public HttpRequestProcessor(INetworkAgent agent)
        {
            _networkAgent = agent;
        }

		public bool IsIdle
		{
			get
			{
				lock (_responseQueue)
				{
					return _currentRequest == null && _requestQueue.Count <= 0 && _responseQueue.Count <=0;
				}
			}
		}

		public override void Tick (float delta)
		{
			Perf.BeginSample("VinayGao:HttpRequestProcessor.Tick");
			if(_currentRequest != null && _currentRequest.HttpRequestor != null && !_pauseRequest)
			{
				_currentRequest.HttpRequestor.Tick(delta);
			}

			TickResponseQueue(); // 这里tick两次response，是避免SequenceNumber的使用了上一次的
			TickRequestQueue();
			TickResponseQueue();
			Perf.EndSample();
		}
		
		// public static void LogPauseState(string msg)
		// {
		// 	UnityEngine.Debug.LogError("[http_pause_in_callinit] " + msg);
		// }

		public void Pause()
		{
			_pauseRequest = true;
			// HttpRequestProcessor.LogPauseState($"Pause, _currentRequest:{_currentRequest}, _pauseRequest:{_pauseRequest}, _requestQueue.Count:{_requestQueue.Count}");
			if (_currentRequest != null && _currentRequest.HttpRequestor != null)
			{
				if (_currentRequest.HttpRequestor.IsWorking)
				{
					// LogPauseState($"_currentRequest.HttpRequestor.Dispose(), number:{_currentRequest.SequenceNumber}");
					_currentRequest.HttpRequestor.Dispose();
					_currentRequest.HttpRequestor = null;
					_currentRequest.ClearHttpRequestDataCache();
				}
			}
		}

		public void Resume()
		{
			_pauseRequest = false;
			// HttpRequestProcessor.LogPauseState($"Resume, _currentRequest:{_currentRequest}, _pauseRequest:{_pauseRequest}, _requestQueue.Count:{_requestQueue.Count}");
			if (_currentRequest != null && _currentRequest.HttpRequestor == null)
			{
				// 重新启动请求
				// HttpRequestProcessor.LogPauseState($"DoStartRequest, _currentRequest:{_currentRequest}, _pauseRequest:{_pauseRequest}, _requestQueue.Count:{_requestQueue.Count}");
				DoStartRequest(_currentRequest);
			}
		}
		
		protected void TickRequestQueue()
		{
			// if (_pauseRequest)
			// {
			// 	HttpRequestProcessor.LogPauseState($"Tick, _currentRequest:{_currentRequest}, _pauseRequest:{_pauseRequest}, _requestQueue.Count:{_requestQueue.Count}");
			// }
			if(_currentRequest == null && !_pauseRequest)
			{
				lock(_requestQueue)
				{
					if(_requestQueue.Count > 0)
					{
						_currentRequest = _requestQueue.Dequeue();

						var sequenceNumber = GenerateSequenceNumber();

						_currentRequest.SequenceNumber = sequenceNumber;

                        DoStartRequest(_currentRequest);
					}
				}
			}
		}

		private void DoStartRequest(HttpRequestRequest currentRequest)
		{
			var param = currentRequest.Parameter;
			var rumServiceName = string.Format("{0}:{1}", param.GetService(), param.GetMethod());

			var httpRequester = new HttpRequestor(_networkAgent, rumServiceName,
				currentRequest.SequenceNumber.ToString(),
				_networkAgent.UseRtmHttpAgent && param.UseRtmAgent) { UserData = param };

			httpRequester.DoRequest(currentRequest.GetHttpRequestData(), _requestStrategy,
				param.NeedRetry,
				delegate(HttpRequestor.BaseProtocol protocol, long responseCode)
				{
					OnHttpResponse(currentRequest.SequenceNumber, protocol, responseCode);
				});

			currentRequest.HttpRequestor = httpRequester;
		}

		protected void TickResponseQueue ()
		{
			lock (_responseQueue)
			{
				while (_responseQueue.Count > 0) 
				{
                    var workload = _responseQueue.Dequeue();
                    var httpRequestResponse = workload.response;

                    var protocol = httpRequestResponse.Protocol;
                    if (protocol.sequenceNumber > 0)
                    {
                        // 修正当前序号
                        _sequenceNumber = protocol.sequenceNumber - 1;
                    }
                    
                    var httpRequestRequest = httpRequestResponse.HttpRequestServiceRequest;
                    _requestStrategy.ProcessResponse(httpRequestRequest.Parameter, protocol, httpRequestResponse.ResponseCode,
						httpRequestRequest.Callback, httpRequestRequest.UserData);
				}
			}
		}

        private void OnHttpResponse(long sequenceNumber, HttpRequestor.BaseProtocol protocol, long responseCode)
		{
			if(_currentRequest != null && _currentRequest.SequenceNumber == sequenceNumber)
			{
                InsertResponse(new HttpRequestResponse
                {
                    HttpRequestServiceRequest = _currentRequest,
                    Protocol = protocol,
                    ResponseCode = responseCode,
                },"", false, 0);

				if (responseCode >= 500)
				{
					--_sequenceNumber;
				}
				_currentRequest = null;
			}
		}

		public override void Reset()
		{
			base.Reset();
			_currentRequest = null;
			_sequenceNumber = 0;
			_pauseRequest = false;
		}

        private static void DebugLog(string log)
		{
			#if HTTP_REQUEST_PROCESSOR_LOG_ENABLED
			NLogger.Log(string.Format("<color=yellow>{0}</color>", log));
			#endif
		}
	}
}

