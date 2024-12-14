//#define HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
    public enum ServiceResult
    {
        Sent,
        Ignored,
        NotSupported,
    }

    public interface IServiceUILocker
    {
        void Lock(RectTransform[] lockable);
        void Unlock(RectTransform[] lockable);
    }

	public interface IServiceSceneLocker
	{
		void Lock(string prefabName, Transform lockable, Vector3 pos, Quaternion rot, Vector3 scale);
		void Unlock(string prefabName, Transform lockable);
	}

    public delegate void ServiceResponseCallback(bool result, BaseServiceResponse response);
    public delegate bool ServiceExceptionCallback(HttpRequestor.BaseProtocol protocol);

    public class ServiceManager : Singleton<ServiceManager> , IManager
    {
        private HttpRequestProcessor _defaultHttpRequestProcessor = null;
        private HttpLoaderProcessor _defaultHttpLoaderProcessor = null;
        private HttpRequestProcessor _httpRequestProcessor = null;
        private HttpLoaderProcessor _httpLoaderProcessor = null;
        private AssetServiceProcessor _assetServiceProcessor = null;
        private AssetServiceProcessor _smallAssetServiceProcessor = null;
        private RtmServiceProcessor _rtmServiceProcessor = null;
        private HttpLoaderProcessor _rtmHttpLoaderProcessor = null;
        private AssetServiceProcessor _configServiceProcessor = null;
        private ServiceExceptionCallback _onException = null;
        private IServiceUILocker _uiLocker = null;
		private IServiceSceneLocker _sceneLocker = null;

		private Dictionary<string, ServiceResponseCallback> _allCallback = new Dictionary<string, ServiceResponseCallback>();

		protected ServiceManagerConfig config = null;
		private bool _logDownloadAsError = false;

		private int _smallAssetDownloadCount = 8;

		private bool _useSmallAssetDownloader = true;

		public bool UseSmallAssetDownloader
		{
			get { return _useSmallAssetDownloader; }
			set { _useSmallAssetDownloader = value; }
		}

        public ServiceManagerConfig GetServiceManagerConfig()
		{
			return config;
		}
		public void Initialize (NexgenObject configParam)
		{
//			TimeUtils.UpdateRecord("ServiceManager Initialize");
			config = configParam as ServiceManagerConfig;

            _defaultHttpRequestProcessor = new HttpRequestProcessor(config.NetworkAgent);
            _defaultHttpRequestProcessor.Initialize();
            _defaultHttpRequestProcessor.SetRequestStrategy(config.RequestStrategy);

            _httpRequestProcessor = _defaultHttpRequestProcessor;

            _defaultHttpLoaderProcessor = new HttpLoaderProcessor(config.NetworkAgent);
            _defaultHttpLoaderProcessor.Initialize();
            _defaultHttpLoaderProcessor.SetRequestStrategy(config.LoaderStrategy);
            
            if (config.IsLowMemoryDevice)
            {
	            _defaultHttpLoaderProcessor.AdjustMaxLoaderCount(1);
            }

            _httpLoaderProcessor = _defaultHttpLoaderProcessor;

            _rtmServiceProcessor = new RtmServiceProcessor(config.NetworkAgent);
			_rtmServiceProcessor.Initialize();
			_rtmServiceProcessor.SetRequestStrategy(config.RtmConnectStrategy);
            
            _rtmHttpLoaderProcessor = new HttpLoaderProcessor(config.NetworkAgent);
            _rtmHttpLoaderProcessor.Initialize();
            _rtmHttpLoaderProcessor.SetRequestStrategy(config.RtmStrategy);

            _assetServiceProcessor = new AssetServiceProcessor();
			_assetServiceProcessor.Initialize();
			_assetServiceProcessor.SetRequestStrategy(config.AssetStrategy);

            _smallAssetServiceProcessor = new AssetServiceProcessor();
			_smallAssetServiceProcessor.Initialize();
			_smallAssetServiceProcessor.SetRequestStrategy(config.AssetStrategy);
			_smallAssetServiceProcessor.SetAssetDownloadQueue(_smallAssetDownloadCount);

            _configServiceProcessor = new AssetServiceProcessor();
			_configServiceProcessor.Initialize();
			_configServiceProcessor.SetRequestStrategy(config.ConfigStrategy);
            _uiLocker = config.UILocker;
			_sceneLocker = config.SceneLocker;
			_onException = config.OnException;
		}
		
		public static bool EnablePauseRequestProcessorFeature = true;

		public void PauseRequestProcessor()
		{
			if (EnablePauseRequestProcessorFeature)
			{
				if (_httpRequestProcessor != null)
				{
					_httpRequestProcessor.Pause();
				}
			}
		}

		public void ResumeRequestProcessor()
		{
			if (EnablePauseRequestProcessorFeature)
			{
				if (_httpRequestProcessor != null)
				{
					_httpRequestProcessor.Resume();
				}
			}
		}

	    public void SetAssetDownloadQueue(int total_queue)
	    {
		    _assetServiceProcessor.SetAssetDownloadQueue(total_queue);
	    }

		public void SetSmallAssetDownloadCount(int small_asset_count)
		{
			_smallAssetDownloadCount= small_asset_count;
			_smallAssetServiceProcessor.SetAssetDownloadQueue(small_asset_count);
		}

	    public void SetAssetStrategy(HttpRequestor.IRequestStrategy strategy)
	    {
		    _assetServiceProcessor.SetRequestStrategy(strategy);
			_smallAssetServiceProcessor.SetRequestStrategy(strategy);
	    }

	    public void SetConfigStrategy(HttpRequestor.IRequestStrategy strategy)
	    {
		    _configServiceProcessor.SetRequestStrategy(strategy);
	    }
		
		public ServiceResult SendWithFullScreenLock(BaseServiceParameter serviceParameter, object userData = null)
		{
			return SendUI(serviceParameter, null, userData);
		}

		public ServiceResult Send(BaseServiceParameter serviceParameter, RectTransform lockable, object userData = null)
		{
		    return SendUI(serviceParameter, lockable ? new[] {lockable} : new RectTransform[] { }, userData);
		}

		public ServiceResult Send(BaseServiceParameter serviceParameter, object userData, string lockerPrefabName, Transform lockable, Vector3 pos, Quaternion rot, Vector3 scale)
		{
			return SendScene(serviceParameter, userData, lockable, lockerPrefabName, pos, rot, scale);
		}
		
		public void SetDownloadLogAsError(bool b)
		{
			_logDownloadAsError = b;
			_assetServiceProcessor.LogDownloadAsError = b;
			_smallAssetServiceProcessor.LogDownloadAsError = b;

        }

        public void DownloadAsset(string url, string savePath, Action<bool, HttpResponseData> callback, int priority = 999, uint crcCheckValue = 0,uint fileSize = 0)
		{
            if (!AssetServiceRequest.IsValid(savePath))
            {
                return;
            }

			if(UseSmallAssetDownloader && fileSize > 0 && fileSize <= 90*1024)
			{
				_smallAssetServiceProcessor.InsertRequest(new AssetServiceRequest(url, savePath, callback, priority, crcCheckValue));
                DebugLog("url {0} savePath {1} priority {2}", url, savePath, priority);
                return;
			}

            _assetServiceProcessor.InsertRequest(new AssetServiceRequest(url, savePath, callback, priority, crcCheckValue));
			
			DebugLog("url {0} savePath {1} priority {2}", url, savePath, priority);
		}

        public void DownloadConfig(string url, string savePath, Action<bool, HttpResponseData> callback,
		    bool downloadAsSoonAsPosible = false)
	    {
            if (!AssetServiceRequest.IsValid(savePath))
            {
                return;
            }
            
            DebugLog("url {0} savePath {1} downloadAsSoonAsPossible {2}", url, savePath, downloadAsSoonAsPosible);
		    if(downloadAsSoonAsPosible)
		    {
			    _configServiceProcessor.InsertHighPriorityRequest(new AssetServiceRequest(url, savePath, callback));
		    }
		    else
		    {
			    _configServiceProcessor.InsertRequest(new AssetServiceRequest(url, savePath, callback));
		    }
	    }
        
        private static void DebugLog(string log, params object[] parameters)
        {
#if HTTP_LOADER_SERVICE_PROCESSOR_LOG_ENABLED
			NLogger.LogChannel("ServiceManager", log, parameters);
#endif
        }

        private void DoLockUI(RectTransform[] allLockable)
        {
            if (_uiLocker != null)
            {
                _uiLocker.Lock(allLockable);
            }
        }

        private void DoUnlockUI(RectTransform[] allLockable)
        {
            if(_uiLocker != null)
            {
                _uiLocker.Unlock(allLockable);
            }
        }

		private void DoLockScene(string prefabName, Transform lockable, Vector3 pos, Quaternion rot, Vector3 scale)
		{
			if (_sceneLocker != null)
			{
				_sceneLocker.Lock (prefabName, lockable, pos, rot, scale);
			}
		}

		private void DoUnlockScene(string prefabName, Transform lockable)
		{
			if(_sceneLocker != null)
			{
				_sceneLocker.Unlock (prefabName, lockable);
			}
		}

		private ServiceResult SendScene(BaseServiceParameter serviceParameter, object userData, Transform lockable, string prefabName, Vector3 pos, Quaternion rot, Vector3 scale)
		{
			return SendInternal (serviceParameter, () => DoLockScene (prefabName, lockable, pos, rot, scale), () => DoUnlockScene (prefabName, lockable), userData);
		}

        private ServiceResult SendUI(BaseServiceParameter serviceParameter, RectTransform[] allLockable, object userData)
		{
			return SendInternal (serviceParameter, () => DoLockUI (allLockable), () => DoUnlockUI (allLockable), userData);
		}

		private ServiceResult SendInternal(BaseServiceParameter serviceParameter, Action onLock, Action onUnlock, object userData)
		{
			Action<bool, BaseServiceResponse, HttpRequestor.BaseProtocol> responseCallback = delegate(bool result, BaseServiceResponse serviceResponse, HttpRequestor.BaseProtocol protocol)
			{
				// 解锁
				onUnlock.Invoke();

				if(!result)
				{
					//通用处理成功后，直接return不再广播消息
					if (DispatchException(protocol))
					{
						return;
					}

                    if (serviceResponse == null)
                    {
                        return;
                    }
				}

				DispatchCallback (serviceParameter.GetServiceEventName (), result, serviceResponse);
			};

#if UNITY_DEBUG
			NLogger.LogChannel(Color.green, "HTTP",$"[HTTP][Request]Service:{serviceParameter.GetService()} Method {serviceParameter.GetMethod()}");	
#endif
			switch(serviceParameter.GetServiceType())
			{
			case ServiceType.REQUEST:
				if (!_httpRequestProcessor.IsIdle && !serviceParameter.GuaranteeCommit)
				{
					NLogger.Log ("HttpRequestProcessor is busy now, ignore the request");
					return ServiceResult.Ignored;
				}

				onLock.Invoke ();
				_httpRequestProcessor.InsertRequest(new HttpRequestRequest(serviceParameter, responseCallback, userData));
				return ServiceResult.Sent;

			case ServiceType.LOADER:
				onLock.Invoke ();
				_httpLoaderProcessor.InsertRequest(new HttpLoaderRequest(serviceParameter, responseCallback, userData));
				return ServiceResult.Sent;
            
            case ServiceType.RTM:
                onLock.Invoke();
                _rtmHttpLoaderProcessor.InsertRequest(new HttpLoaderRequest(serviceParameter, responseCallback, userData));
                return ServiceResult.Sent;

			default:
				NLogger.Error(string.Format("unsupported service type: {0}", serviceParameter.GetServiceType()));
				return ServiceResult.NotSupported;
			}
		}

        public void SetFakeProcesser(HttpRequestProcessor reqprocessor,HttpLoaderProcessor loadProcessor)
        {
	        _httpRequestProcessor = reqprocessor;
            _httpLoaderProcessor = loadProcessor;
        }

        public void ResetHttpProcesser()
        {
	        _httpRequestProcessor = _defaultHttpRequestProcessor;
            _httpLoaderProcessor = _defaultHttpLoaderProcessor;
        }

        public void Reset ()
        {
//	        TimeUtils.UpdateRecord("ServiceManager Reset");
	        
	        // RTM会在Reset的时候注册监听
            _allCallback.Clear();
	        
			ResetHttpProcesser(); // order matters
			_httpRequestProcessor.Reset();
			_httpLoaderProcessor.Reset();
            _rtmServiceProcessor.Reset();
            _rtmHttpLoaderProcessor.Reset();
			_assetServiceProcessor.Reset();
            _smallAssetServiceProcessor.Reset ();

            _configServiceProcessor.Reset();
		}

        public override void Release ()
        {
//	        TimeUtils.UpdateRecord("ServiceManager Release");
			_httpRequestProcessor.Release();
			_httpLoaderProcessor.Release();
			_rtmServiceProcessor.Release();
            _rtmHttpLoaderProcessor.Release();
			_assetServiceProcessor.Release();
            _smallAssetServiceProcessor.Release ();

            _configServiceProcessor.Release();

			_httpRequestProcessor = null;
			_httpLoaderProcessor = null;
			_rtmServiceProcessor = null;
            _rtmHttpLoaderProcessor = null;
            
			_allCallback.Clear();
        }

		public void ConnectRtm (string host, int port, object param)
        {
			_rtmServiceProcessor.Connect(host, port, param);
        }

        public void DisconnectRtm ()
        {
	        try
	        {
		        _rtmServiceProcessor.Disconnect();
	        }
	        catch (Exception e)
	        {
				NLogger.Error("ServiceManager DisconnectRtm with error: " + e.Message);   
	        }
        }

		public bool IsRtmReady
		{
			get
			{
				return _rtmServiceProcessor.IsRtmReady;
			}
		}

		public const string RTM_Tag_Main = "Main";
		public const string RTM_Tag_AllianceChampionship = "AllianceChampionship";
		
		public object reconnectParamsInEvent = null;

        public void GetOnlineUsers(List<long> uids,Action<List<long>> callback)
		{
			_rtmServiceProcessor.GetOnlineUsers(uids, callback);
		}

        public void AddRtmConnectCallback(Action callback)
		{
			_rtmServiceProcessor.AddRtmConnectCallback (callback);
		}

		public void RemoveRtmConnectCallback(Action callback)
		{
			_rtmServiceProcessor.RemoveRtmConnectCallback (callback);
		}

		public void AddRtmKickOutCallback(Action callback)
		{
			_rtmServiceProcessor.AddRtmKickOutCallback (callback);
		}

		public void RemoveRtmKickOutCallback(Action callback)
		{
			_rtmServiceProcessor.RemoveRtmKickOutCallback (callback);
		}

	    public void AddRtmConnectFailedCallback(Action callback)
	    {
		    _rtmServiceProcessor.AddRtmConnectFailedCallback(callback);
	    }
	    
	    public void RemoveRtmConnectFailedCallback(Action callback)
	    {
		    _rtmServiceProcessor.RemoveRtmConnectFailedCallback(callback);
	    }

	    public void AddRtmDisconnectCallback(Action callback)
		{
			_rtmServiceProcessor.AddRtmDisconnectCallback (callback);
		}

		public void RemoveRtmDisconnectCallback(Action callback)
		{
			_rtmServiceProcessor.RemoveRtmDisconnectCallback (callback);
		}

        public bool CheckRtmAlive()
        {
            return _rtmServiceProcessor.CheckRtmAlive ();
        }

        public void AddResponseCallback(string eventKey, ServiceResponseCallback callback)
		{
			if(!_allCallback.ContainsKey(eventKey))
			{
				_allCallback.Add(eventKey, null);
			}

			_allCallback[eventKey] += callback;
		}

		public void RemoveResponseCallback(string eventKey, ServiceResponseCallback callback)
		{
			if(_allCallback.ContainsKey(eventKey))
			{
				_allCallback[eventKey] -= callback;
			}
		}

		public void DispatchCallback(string eventKey, bool result, BaseServiceResponse serviceResponse)
		{
			ServiceResponseCallback callback = null;
			if(_allCallback.TryGetValue(eventKey, out callback) && callback != null)
			{
				callback.Invoke(result, serviceResponse);
			}
		}

        private bool DispatchException(HttpRequestor.BaseProtocol protocol)
		{
			if(_onException != null)
			{
                return	_onException(protocol);
			}
			return false;
		}

        public string DumpAllCallbacks()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pair in _allCallback)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                var callbacks = pair.Value.GetInvocationList();
                if (callbacks.Length <= 0)
                {
                    continue;
                }

                sb.AppendFormat("Service Name: {0} ({1}) >>>>>", pair.Key, callbacks.Length);
                foreach (var listener in callbacks)
                {
                    if (listener.Target != null)
                    {
                        sb.AppendFormat("Member Function: {0}.{1}\n", listener.Target.GetType(), listener.Method.Name);
                    }
                    else
                    {
                        sb.AppendFormat("Static Function: {0}\n", listener.Method.Name);
                    }
                }
            }
            return sb.ToString();
        }

		public int GetListenerCount()
		{
			int count = 0;
			foreach (var pair in _allCallback){
				if (pair.Value != null)
				{
					var callbacks = pair.Value.GetInvocationList();
				 	count += callbacks.Length;
				}
			}
			return count;
		}

        /// <summary>
        /// config并行下载数量上限
        /// </summary>
        public void SetConfigDownloadQueue(int count)
        {
	        _configServiceProcessor?.SetAssetDownloadQueue(count);
        }
    }
}