using System;
using System.Collections.Generic;

namespace NexgenDragon
{
	public abstract class BaseServiceProcessor<TRequest, TResponse> : IObject, ITicker
	{
        public struct Workload
        {
            public bool isHistory;
            public long msgId;
            public int typeCode;
            public TResponse response;
            public string responseString;
        }

		protected Queue<TRequest> _requestQueue = new Queue<TRequest>();
		protected Queue<Workload> _responseQueue = new Queue<Workload>();
		protected HttpRequestor.IRequestStrategy _requestStrategy = null;
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
		public void SetRequestStrategy(HttpRequestor.IRequestStrategy strategy)
		{
			_requestStrategy = strategy;
		}

		public virtual void Initialize()
		{
			GameFacade.Instance.AddTicker(this);
		}

        public virtual void InsertRequest(TRequest request)
		{
			lock(_requestQueue)
			{
				_requestQueue.Enqueue(request);
			}
		}

        protected virtual void InsertResponse(TResponse response,string responseString, bool isHistory, long msgId, int typeCode = 2) //WatcherSDK.CodecType.Json
		{
			lock(_responseQueue)
			{
                _responseQueue.Enqueue(new Workload
                {
                    isHistory = isHistory,
                    msgId = msgId,
                    response = response,
                    responseString = responseString,
                    typeCode = typeCode
                });
			}
		}

		public virtual void Reset()
		{
			lock(_requestQueue)
			{
				_requestQueue.Clear();
			}

			lock(_responseQueue)
			{
				_responseQueue.Clear();
			}
		}

		public virtual void Release ()
		{
			GameFacade.Instance.RemoveTicker(this);
		}

		public abstract void Tick (float delta);
	}
}

