//#define HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	public class AssetServiceProcessor : BaseServiceProcessor<AssetServiceRequest, AssetServiceResponse>
	{
		// private int HIGH_PRIORITY_ONLY_COUNT = 2;
		private int MAX_REQUEST_COUNT = 2;
		private long index;

		private List<AssetServiceRequest> _allHighPriorityRequestList = new List<AssetServiceRequest>();
        private List<AssetServiceRequest> _allProcessingRequest = new List<AssetServiceRequest>();
        private readonly AssetNetworkAgent _networkAgent;

		private bool _logDownloadAsError = false;

        public AssetServiceProcessor()
        {
            _networkAgent = new AssetNetworkAgent();
        }

		public bool LogDownloadAsError
		{ get { return _logDownloadAsError;}
			set { _logDownloadAsError = value; }
		}

		public void SetAssetDownloadQueue(int total)
		{
			// HIGH_PRIORITY_ONLY_COUNT = high;
			MAX_REQUEST_COUNT = total;
		}

		public override void InsertRequest(AssetServiceRequest request)
		{
			InsertHighPriorityRequest(request);
//			lock(_requestQueue)
//			{
//				AssetServiceRequest finalRequest = null;
//
//				// already exist in processing queue
//				var sameRequest = _allProcessingRequest.Find(p => p.DownloadUrl == request.DownloadUrl && p.SavePath == request.SavePath);
//				if(sameRequest != null)
//				{
//					sameRequest.Callback += request.Callback;
//					finalRequest = sameRequest;
//				}
//
//				// already exist in high priority queue
//				if(finalRequest == null)
//				{
//					foreach(var requestInQueue in _allHighPriorityRequestList)
//					{
//						if(requestInQueue.DownloadUrl == request.DownloadUrl &&
//							requestInQueue.SavePath == request.SavePath)
//						{
//							requestInQueue.Callback += request.Callback;
//							finalRequest = requestInQueue;
//						}
//					}
//				}
//
//				// already in normal queue
//				if(finalRequest == null)
//				{
//					foreach(var requestInQueue in _requestQueue)
//					{
//						if(requestInQueue.DownloadUrl == request.DownloadUrl &&
//							requestInQueue.SavePath == request.SavePath)
//						{
//							requestInQueue.Callback += request.Callback;
//							finalRequest = requestInQueue;
//						}
//					}
//				}
//
//				if(finalRequest == null)
//				{
//					_requestQueue.Enqueue(request);
//					finalRequest = request;
//				}
//			}
		}

		public void InsertHighPriorityRequest(AssetServiceRequest request)
		{
			DebugLog("{0} priority {1} add to high request list",
				request.DownloadUrl.Substring(request.DownloadUrl.LastIndexOf("/", StringComparison.Ordinal) + 1),
				request.Priority);
		
			request.Index = index++;
			lock(_allHighPriorityRequestList)
			{
				AssetServiceRequest finalRequest = null;

				// already exist in processing queue
				var sameRequest = _allProcessingRequest.Find(p => p.DownloadUrl == request.DownloadUrl && p.SavePath == request.SavePath);
				if(sameRequest != null)
				{
					// update priority
					if (request.Priority >= sameRequest.Priority)
					{
						sameRequest.Index = request.Index;
						sameRequest.Priority = request.Priority;
					}
					sameRequest.Callback += request.Callback;
					finalRequest = sameRequest;
				}

				// already exist in high priority queue
				if(finalRequest == null)
				{
					foreach(var requestInQueue in _allHighPriorityRequestList)
					{
						if(requestInQueue.DownloadUrl == request.DownloadUrl &&
							requestInQueue.SavePath == request.SavePath)
						{
							if (request.Priority >= requestInQueue.Priority)
							{
								requestInQueue.Index = request.Index;
								requestInQueue.Priority = request.Priority;
							}
							requestInQueue.Callback += request.Callback;
							finalRequest = requestInQueue;
						}
					}
				}

				// append to high priority queue
				if(finalRequest == null)
				{
					_allHighPriorityRequestList.Add(request);
					finalRequest = request;
				}

//				// remove if exist in normal queue
//				foreach(var requestInQueue in _requestQueue)
//				{
//					if(!requestInQueue.CancelMark &&
//						requestInQueue.DownloadUrl == request.DownloadUrl &&
//						requestInQueue.SavePath == request.SavePath)
//					{
//						finalRequest.Callback += requestInQueue.Callback;
//						requestInQueue.CancelMark = true;
//					}
//				}
			}
		}
			
		public override void Tick (float delta)
		{
			Perf.BeginSample("VinayGao:AssetServiceProcessor.Tick");
			lock (_allProcessingRequest)
			{
				foreach(AssetServiceRequest assetServiceRequest in _allProcessingRequest)
				{
					assetServiceRequest.HttpRequestor.Tick(delta);
				}
			}
            
            _networkAgent.Update(delta);

			TickRequestQueue();

			TickResponseQueue();
			Perf.EndSample();
		}

		protected void TickRequestQueue()
		{
			lock (_allProcessingRequest)
			{
				if(_allProcessingRequest.Count < MAX_REQUEST_COUNT)
				{
					lock(_allHighPriorityRequestList)
					{
						while((_allHighPriorityRequestList.Count > 0) &&  _allProcessingRequest.Count < MAX_REQUEST_COUNT)
						{
							AssetServiceRequest assetServiceRequest = null;

							if(_allHighPriorityRequestList.Count > 0)
							{
								_allHighPriorityRequestList.Sort(PriorityComparison);
								assetServiceRequest = _allHighPriorityRequestList[_allHighPriorityRequestList.Count -1];
								_allHighPriorityRequestList.RemoveAt(_allHighPriorityRequestList.Count -1);

								DebugLog("{0} priority {1} begin request list",
									assetServiceRequest.DownloadUrl.Substring(
										assetServiceRequest.DownloadUrl.LastIndexOf("/", StringComparison.Ordinal) + 1),
									assetServiceRequest.Priority);
							}

							if(assetServiceRequest != null && !assetServiceRequest.CancelMark)
							{
								_allProcessingRequest.Add(assetServiceRequest);

								var rumServiceName = assetServiceRequest.DownloadUrl;
								rumServiceName = rumServiceName.Substring(rumServiceName.LastIndexOf("/", StringComparison.Ordinal) + 1);

                                var httpRequester = new HttpRequestor(_networkAgent, rumServiceName, null, false)
                                {
                                    UserData = assetServiceRequest
                                };

                                NLogger.Log("~~ download asset " + assetServiceRequest.HttpRequestData.url + " " +
                                            assetServiceRequest.HttpRequestData.method + " " + assetServiceRequest.Priority);
								if(LogDownloadAsError)
								{
                                    NLogger.Error("~~ download asset " + assetServiceRequest.HttpRequestData.url + " " +
                                            assetServiceRequest.HttpRequestData.method + " " + assetServiceRequest.Priority);
                                }
								

								var fileName = Path.GetFileNameWithoutExtension(assetServiceRequest.SavePath);
								if (AssetBundleSynchro.Instance.IsDecompressing(fileName))
								{
									NLogger.Error("bundle {0} is decompressing,error happened",fileName);
								}
                            
								httpRequester.DoRequest(assetServiceRequest.HttpRequestData, _requestStrategy, true,
									delegate(HttpRequestor.BaseProtocol httpResponseData, long responseCode)
									{
										OnResponse(assetServiceRequest, httpResponseData.protocol as HttpResponseData);
									});

								assetServiceRequest.HttpRequestor = httpRequester;
							}
						}

						//DebugLog ("REQUEST COUNT: " + _allProcessingRequest.Count);
					}
				}
			}
		}

		// high at last, for new high request will be first process
		private int PriorityComparison(AssetServiceRequest x, AssetServiceRequest y)
		{
			if (x.Priority == y.Priority)
			{
				return x.Index.CompareTo(y.Index);
			}
			
			return x.Priority.CompareTo(y.Priority);
		}

		protected void TickResponseQueue()
		{
			lock(_responseQueue)
			{
				while(_responseQueue.Count > 0)
				{
                    var workload = _responseQueue.Dequeue();
                    AssetServiceResponse assetServiceResponse = workload.response;

                    AssetServiceRequest assetServiceRequest = assetServiceResponse.AssetServiceRequest;

                    if (_allProcessingRequest.Contains(assetServiceRequest))
                    {
                        _allProcessingRequest.Remove(assetServiceRequest);

                        var httpResponseOk = assetServiceResponse.HttpResponseData != null &&
                            assetServiceResponse.HttpResponseData.responseCode == 200;

                        DebugLog("Asset Download Result : {0}, from {1} to {2}",
                            httpResponseOk,
                            assetServiceResponse.AssetServiceRequest.DownloadUrl,
                            assetServiceResponse.AssetServiceRequest.SavePath);

                        if (httpResponseOk)
                        {
                            if (assetServiceRequest.Callback != null)
                            {
                                assetServiceRequest.Callback(true, assetServiceResponse.HttpResponseData);
                            }    
                        }
                    }
                    else
                    {
                        DebugLog("ignore loader response");
                    }
                }
            }
        }

        protected void OnResponse(AssetServiceRequest assetServiceRequest, HttpResponseData httpResponseData)
		{
            InsertResponse(new AssetServiceResponse(assetServiceRequest, httpResponseData),"", false, 0);
		}

        private static void DebugLog(string log, params object[] parameters)
		{
			#if HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED
			NLogger.LogChannel("AssetServiceProcessor", log, parameters);
			#endif
		}

		public override void Reset()
		{
			base.Reset();
			
			lock (_allHighPriorityRequestList)
			{
				_allHighPriorityRequestList.Clear();
			}

			lock (_allProcessingRequest)
			{
				_allProcessingRequest.Clear();	
			}

            _networkAgent.Reset();
        }
	}
}

