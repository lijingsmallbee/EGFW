using System;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NexgenDragon
{
    public class WWWHttpApi : NexgenObject, IHttpApi, IGapTicker
    {
		private Dictionary<WWW, Action<HttpResponseData>> _requestDict = new Dictionary<WWW, Action<HttpResponseData>>();
		private List<WWW> _removals = new List<WWW>();

        public WWWHttpApi()
        {
            GameFacade.Instance.AddTicker(this);
        }

        public IHttpAsyncOperation Send(HttpRequestData request, Action<HttpResponseData> response)
        {
            WWW www = new WWW(request.url, request.requestContent, request.headersDict);

            _requestDict.Add(www, response);
            return new WWWHttpOperation(null);
        }

        public void Tick(float delta)
        {
	        Perf.BeginSample("VinayGao:WWWHttpApi.Tick");
			_removals.Clear ();

            for (var it = _requestDict.GetEnumerator(); it.MoveNext();)
			{
                WWW request = it.Current.Key;
                
				if (request.isDone)
				{
                    HttpResponseData responseData = new HttpResponseData();
                    responseData.responseCode = 200;
                    responseData.responseContent = request.bytes;
                    responseData.url = request.url;

                    if (null != it.Current.Value)
					{
                        it.Current.Value.Invoke(responseData);
                    }
                    request.Dispose();
                    _removals.Add(request);
                }
                else if (!string.IsNullOrEmpty(request.error))
				{
                    HttpResponseData responseData = new HttpResponseData();
                    responseData.responseCode = -100;
                    responseData.responseContent = Encoding.ASCII.GetBytes(request.error);
                    responseData.url = request.url;

					if (null != it.Current.Value)
					{
						it.Current.Value.Invoke(responseData);
					}

                    request.Dispose();
                    _removals.Add(request);
                }
            }

            for (int i = 0; i < _removals.Count; i++)
			{
                _requestDict.Remove(_removals[i]);
            }
	        Perf.EndSample();
        }
        
        public override void Release()
        {
            GameFacade.Instance.RemoveTicker(this);
            
            for (var it = _requestDict.GetEnumerator(); it.MoveNext();) {
                WWW request = it.Current.Key;
                if (!request.isDone) {
                    request.Dispose();
                }
                request.Dispose();
            }
            _requestDict.Clear();
        }

		public bool IsThreadSupported
		{
			get
			{
				return false;
			}
		}

		public bool EnableProxy { get; set; }
		public string ProxyIP { get; set; }
		public int ProxyPort { get; set; }
		public void Reset()
		{
			
		}
    }
}
