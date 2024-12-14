//#define HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED

using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	public class HttpLoaderProcessor : BaseServiceProcessor<HttpLoaderRequest, HttpLoaderResponse>
	{
		protected int MAX_REQUEST_COUNT = 5;

		protected List<HttpLoaderRequest> _allRequest = new List<HttpLoaderRequest>();
		protected INetworkAgent _networkAgent;

		public HttpLoaderProcessor(INetworkAgent agent)
		{
			_networkAgent = agent;
		}

		public void AdjustMaxLoaderCount(int count)
		{
			MAX_REQUEST_COUNT = count;
		}

		public override void Tick(float delta)
		{
			Perf.BeginSample("VinayGao:HttpLoaderProcessor.Tick");
			foreach (HttpLoaderRequest httpLoaderRequest in _allRequest)
			{
				httpLoaderRequest.HttpRequestor.Tick(delta);
			}

			TickRequestQueue();

			TickResponseQueue();
			Perf.EndSample();
		}

		public override void Reset()
		{
			base.Reset();
			_allRequest.Clear();
		}

		protected void TickRequestQueue()
		{
			if (_allRequest.Count < MAX_REQUEST_COUNT)
			{
				lock (_requestQueue)
				{
					while (_requestQueue.Count > 0 && _allRequest.Count < MAX_REQUEST_COUNT)
					{
						HttpLoaderRequest httpLoaderServiceRequest = _requestQueue.Dequeue();

						_allRequest.Add(httpLoaderServiceRequest);

						var param = httpLoaderServiceRequest.Parameter;
						var rumServiceName = string.Format("{0}:{1}", param.GetService(), param.GetMethod());

						var httpRequester = new HttpRequestor(_networkAgent, rumServiceName, null,
							_networkAgent.UseRtmHttpAgent && param.UseRtmAgent) {UserData = param};

						httpRequester.DoRequest(httpLoaderServiceRequest.GetHttpRequestData(), _requestStrategy,
							param.NeedRetry,
							delegate(HttpRequestor.BaseProtocol protocol, long responseCode)
							{
								OnResponse(httpLoaderServiceRequest, protocol, responseCode);
							});

						httpLoaderServiceRequest.HttpRequestor = httpRequester;
					}
				}
			}
		}

		protected void TickResponseQueue()
		{
			lock (_responseQueue)
			{
				while (_responseQueue.Count > 0)
				{
					var workload = _responseQueue.Dequeue();
					HttpLoaderResponse httpLoaderResponse = workload.response;

					HttpLoaderRequest httpLoaderRequest = httpLoaderResponse.HttpLoaderServiceRequest;

					if (_allRequest.Contains(httpLoaderRequest))
					{
						_requestStrategy.ProcessResponse(httpLoaderRequest.Parameter, httpLoaderResponse.Protocol,
							httpLoaderResponse.ResponseCode,
							httpLoaderRequest.Callback, httpLoaderRequest.UserData);

						_allRequest.Remove(httpLoaderRequest);
					}
					else
					{
						DebugLog("ignore loader response");
					}
				}
			}
		}

		protected void OnResponse(HttpLoaderRequest httpLoaderRequest, HttpRequestor.BaseProtocol protocol,
			long responseCode)
		{
			InsertResponse(new HttpLoaderResponse(httpLoaderRequest, protocol, responseCode),"", false, 0);
		}

		private static void DebugLog(string log)
		{
#if HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED
			NLogger.Log(string.Format("<color=green>{0}</color>", log));
#endif
		}
	}
}

