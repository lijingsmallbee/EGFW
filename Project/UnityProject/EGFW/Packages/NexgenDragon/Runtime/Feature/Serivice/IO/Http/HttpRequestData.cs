using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace NexgenDragon
{
	public enum HttpMethod
	{
		Post,
		Get,
	}

    public class HttpRequestData : NexgenObject
	{
		public string url;
        public string savePath;
		public uint crcCheckValue;
		public HttpMethod method = HttpMethod.Post;
		public byte[] requestContent;
		public Dictionary<string, string> headersDict = new Dictionary<string, string>();
		public bool isRetry;
	}
}

