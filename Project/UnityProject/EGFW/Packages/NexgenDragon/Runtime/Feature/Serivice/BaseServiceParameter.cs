using System.Collections.Generic;

namespace NexgenDragon
{
    public abstract class BaseServiceParameter
	{
        private string _method;
        private string _service;
        private string _url;

        public bool NeedRetry { get; set; }
        public bool UseRtmAgent { get; set; }
        public bool GuaranteeCommit { get; set; }

        public string GetService()
		{
            return _service;
		}

		public string GetMethod()
		{
            return _method;
		}

        public string GetUrl()
        {
            return _url;
        }

        public BaseServiceParameter(string service, string method, string url)
        {
            this.NeedRetry = true;
            this.UseRtmAgent = true;
            this._service = service;
            this._method = method;
            this._url = url;
        }

        public abstract string GetServiceEventName();
        public abstract ServiceType GetServiceType();
		public abstract byte[] Encode(long seqenceNumber);
		public abstract void GetHttpHeader(Dictionary<string, string> header);
		public abstract bool NeedDeviceInfo();
	}
}
