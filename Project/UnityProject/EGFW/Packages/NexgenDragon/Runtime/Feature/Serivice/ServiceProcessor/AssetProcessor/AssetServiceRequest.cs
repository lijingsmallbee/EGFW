using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace NexgenDragon
{
	public class AssetServiceRequest
	{
		public static int IOExceptionTimes = 0;
		public AssetServiceRequest(string downloadUrl, string savePath, Action<bool, HttpResponseData> callback, int priority = 999, uint crcCheckValue = 0, long index = 0)
		{
			DownloadUrl = downloadUrl;
			SavePath = savePath;
			Callback += callback;
			Priority = priority;
			CrcCheckValue = crcCheckValue;
			Index = index;
		}

		public bool CancelMark = false;
		public string DownloadUrl;
		public string SavePath;
		public HttpRequestor HttpRequestor;
        public Action<bool, HttpResponseData> Callback;
        public int Priority;
        public long Index;
        public uint CrcCheckValue;
        
	
		private static Dictionary<string,string> _headerForAsset = new Dictionary<string, string> () { 
			{
				"Content-Type",
				"application/octet-stream"
			}
		};

        public static bool IsValid(string savePath)
        {
            var localName = Path.GetFileNameWithoutExtension(savePath);
            return !string.IsNullOrEmpty(localName);
        }

        private HttpRequestData _httpRequestData;
		public HttpRequestData HttpRequestData
		{
			get
			{
				if(_httpRequestData == null)
				{
					_httpRequestData = new HttpRequestData
					{ 
						url = DownloadUrl,
                        savePath = SavePath,
                        crcCheckValue = CrcCheckValue,
						method = HttpMethod.Get,
						headersDict = _headerForAsset,
						requestContent = null
					};
				}

				return _httpRequestData;
			}
		}
			
	}
}

