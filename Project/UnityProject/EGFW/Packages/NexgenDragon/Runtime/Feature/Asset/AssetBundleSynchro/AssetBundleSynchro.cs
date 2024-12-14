using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace NexgenDragon
{
    public class AssetBundleSynchro : Singleton<AssetBundleSynchro>, IManager, ITicker
    {
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
        private string _tag = "unset";// 用来标记当前游戏跑到什么阶段
        public string Tag
        {
            set => _tag = value;
        }
        private StreamWriter _logSw;
        private void FlushLog(bool isClose)
        {
            if (_logSw != null)
            {
                _logSw.Flush();
                if (isClose)
                {
                    _logSw.Close();
                }
            }
        }
#endif
        public interface IListener
        {
            void OnUpdateStaticBundleProgress(int currentCount, int totalCount, long totalByte);
            void OnUpdateStaticBundleFinished(bool haveAssetChanged);
            void OnUpdateOtaBundleFinished();
            void OnUpdateFinished();
            void OnLoadAssetFailed();
        }

        //这个数据只用来做基础的检查使用，不能作为是否下载的依据，下载有严格的错误检查，这个只能用来显示下载辅助能容的查询
        private HashSet<string> _allCompleteBundle = new HashSet<string>();
        private HashSet<string> _allDownloadingBundle = new HashSet<string>();

        private HashSet<string> _allDecompressing = new HashSet<string>();

        public IListener Listener = null;

        private StateMachine _statemation;

        private bool _tutorialEnd = false;


        public string RemoteCDN = string.Empty;
        public string RemoteVersionR = string.Empty;
        public string RemoteVersionG = string.Empty;

        public bool SkipBundleSync = false;

        private bool _enableDecompress = false;

        private Dictionary<string, string> _allRemoteVersion = new Dictionary<string, string>();

        public List<string> AllBundleType
        {
            get { return _configParameter.AllBundleType; }
        }

        public IAssetBundleSynchroProcessor Processor
        {
            get { return _configParameter.Processor; }
        }

        private Dictionary<string, Dictionary<string, string>> _packageBundleMd5 = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> _localBundleMd5s = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> _remoteBundleMd5s = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, uint>> _remoteBundleCrcCheckValues = new Dictionary<string, Dictionary<string, uint>>();
        private Dictionary<string, Dictionary<string, long>> _remoteBundleSize = new Dictionary<string, Dictionary<string, long>>();
        private Dictionary<string, Dictionary<string, List<string>>> _allBundleUpdateTypes = new Dictionary<string, Dictionary<string, List<string>>>();
        private Dictionary<string, string> _allBundleType = new Dictionary<string, string>();
        public bool RequestUpdateOta = false;

        private HashSet<string> _allRemoteBundleHashFile = new HashSet<string>();

        // switch conditions
        public bool RequestCheckUpdate = false;
        public bool UpdateVersionListComplete = false;
        public bool UpdateStaticBundleComplete = false;
        public bool UpdateOtaBundleComplete = false;

        private AssetBundleSynchroConfig _configParameter = null;

        public bool NeedUpdateLocalCacheVersion = false;
        public bool NeedUpdateRemoveBundle = false;
        private float _saveLocalElapse;
        private float _removeUnusedElapse;
        private List<string> _removeList = new List<string>();
        private List<string> _localUnusedBundles = new List<string>();

        public void Initialize(NexgenObject configParam)
        {
            _configParameter = configParam as AssetBundleSynchroConfig;

            _statemation = new StateMachine();
            _statemation.AddState(new IdleState(this));
            _statemation.AddState(new UpdateVersionListState(this));
            _statemation.AddState(new UpdateStaticBundleState(this));
            _statemation.AddState(new UpdateOtaBundleState(this));
            _statemation.AddState(new UpdateFinishedState(this));

            _statemation.AddTransition<IdleState, UpdateVersionListState>(
                new RequestCheckUpdateCondition(this));
            //只保留基础的更新version逻辑，更新static逻辑放到外部逻辑中，更加灵活
            /*	_statemation.AddTransition<UpdateVersionListState, UpdateStaticBundleState>(
                    new RequestUpdateBundleTypeCondition(this));
                _statemation.AddTransition<UpdateStaticBundleState, UpdateOtaBundleState>(
                    new StaticUpdateCompleteCondition(this));
                _statemation.AddTransition<UpdateOtaBundleState, UpdateFinishedState>(
                    new OtaUpdateCompleteCondition(this)); */

            _statemation.ChangeState(typeof(IdleState));

            GameFacade.Instance.onApplicationFocus += OnApplicationFocus;
            GameFacade.Instance.onApplicationQuit += OnApplicationQuit;

            GameFacade.Instance.AddTicker(this);

            _saveLocalElapse = 0.0f;
            _removeUnusedElapse = 0.0f;
            NeedUpdateLocalCacheVersion = false;
            NeedUpdateRemoveBundle = false;
            _localUnusedBundles.Clear();
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
            // _logSw = new StreamWriter(Path.Combine(AssetManager.Instance.IOtool.GetPersistentDataPath(), $"AssetBundleSynchro_{DateTime.Now:yy-MM-dd.HHmmss}.log"), true, Encoding.UTF8);
            _logSw = new StreamWriter(Path.Combine(Application.persistentDataPath, $"AssetBundleSynchro_{DateTime.Now:yy-MM-dd.HHmmss}.log"), true, Encoding.UTF8);
#endif
        }

        private void OnApplicationQuit()
        {
            SaveLocalBundleVersion();
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
            FlushLog(true);
#endif
        }

        public void SetTutorialEnd(bool end)
        {
            _tutorialEnd = end;
        }

        private void OnApplicationFocus(bool obj)
        {
            if (!obj)
            {
                SaveLocalBundleVersion();
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
                FlushLog(false);
#endif
            }
        }

        public void SaveLocalBundleVersion()
        {
            if (_enableForceResetAllCache && AssetManager.Instance.ForceResetAllCache)
            {
                NLogger.ErrorChannel("AssetBundleSynchro", "[AssetBundleSynchro.SaveLocalBundleVersion]skip for ForceResetAllCache.rvr:{0},rvg:{1}", RemoteVersionR, RemoteVersionG);
                if (_enableFirebaseReport)
                {
                    NLogger.Exception(new Exception("[AssetBundleSynchro.SaveLocalBundleVersion]ForceResetAllCache"));
                }
                return;
            }

            if (_bundleMd5Dirty)
            {
                NLogger.WarnChannel("AssetBundleSynchro", "SaveLocalCacheBundleVersion");

                _bundleMd5Dirty = false;

                var allBundleType = AllBundleType;

                foreach (var bundleType in allBundleType)
                {
                    var bundleVersions = GetLocalBundleMd5s(bundleType);

                    string localBundleListFilePath =
                        AssetManager.Instance.AssetPathProvider.GetLocalCacheBundleListFilePath(bundleType);
                    AssetManager.Instance.IOtool.WriteGameAsset(localBundleListFilePath,
                        AssetManager.Instance.DataParser.ToJson(bundleVersions));
                }
            }
        }

        public void ResetSignals()
        {
            RequestCheckUpdate = false;
            UpdateVersionListComplete = false;
            UpdateStaticBundleComplete = false;
            UpdateOtaBundleComplete = false;
        }

        public void Reset()
        {
            SaveLocalBundleVersion();

            ResetSignals();

            RequestUpdateOta = false;

            _statemation.ChangeState(typeof(IdleState));

            _allDownloadingBundle.Clear();
            _allCompleteBundle.Clear();

            _localUnusedBundles.Clear();

            NeedUpdateRemoveBundle = false;
            
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
            FlushLog(false);
#endif
        }

        public override void Release()
        {
            ResetSignals();

            RequestUpdateOta = false;

            _statemation.ChangeState(typeof(IdleState));

            GameFacade.Instance.onApplicationFocus -= OnApplicationFocus;
            GameFacade.Instance.onApplicationQuit -= OnApplicationQuit;
            GameFacade.Instance.RemoveTicker(this);
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
            FlushLog(true);
#endif
        }

        public void SetRemoteVersion(string bundleType, string version)
        {
            if (_allRemoteVersion.ContainsKey(bundleType))
            {
                _allRemoteVersion.Remove(bundleType);
            }

            _allRemoteVersion.Add(bundleType, version);
        }

        public string GetRemoteVersion(string bundleType)
        {
            string result = string.Empty;

            if (_allRemoteVersion.TryGetValue(bundleType, out result))
            {
                return result;
            }
            else
            {
                throw new SystemException(string.Format("Get Remote Version Failed: {0}", bundleType));
            }
        }

        bool _bundleMd5Dirty = false;

        public void SetLocalBundleMd5(string bundleType, string bundleName, string md5)
        {
            var bundleVersions = GetLocalBundleMd5s(bundleType);
            if (bundleVersions.TryGetValue(bundleName, out string oldMd5))
            {
                if (oldMd5 != md5)
                {
                    bundleVersions[bundleName] = md5;
                    _bundleMd5Dirty = true;
                }
            }
            else
            {
                bundleVersions.Add(bundleName, md5);
                _bundleMd5Dirty = true;
            }
        }

        public void CollectLocalUnusedBundles()
        {
            _localUnusedBundles.Clear();

            if (!IsNeedClearUnusedBundle())
            {
                return;
            }

            try
            {
                InitAllBundleSet();

                RemoveUnusedLocalBundleMd5s();

                var allBundleType = AllBundleType;
                foreach (var bundleType in allBundleType)
                {
                    string folderPath = AssetManager.Instance.AssetPathProvider.GetLocalBundleFolderPath(bundleType);
                    string[] allFiles = AssetManager.Instance.IOtool.ListDocumentAssets(folderPath);
                    foreach (var filePath in allFiles)
                    {
                        string fileName = Path.GetFileName(filePath);
                        if (!_allRemoteBundleHashFile.Contains(fileName))
                        {
                            if (fileName.EndsWith(".json") && !AssetManager.Instance.AssetPathProvider.IsBundleInfoConfig(fileName))
                            {
                                continue;
                            }

                            NeedUpdateRemoveBundle = true;
                            _localUnusedBundles.Add(filePath);
                            if (_localUnusedBundles.Count > 2000)
                            {
                                return;
                            }
                        }
                    }
                }

                _allRemoteBundleHashFile.Clear();
            }
            catch (Exception e)
            {
                NLogger.Error("CollectLocalUnusedBundles Exception:{0}", e.ToString());
            }
        }

        private bool IsNeedClearUnusedBundle()
        {
            try
            {
                var str = PlayerPrefs.GetString("LastRemoveUnusedDay", "0");
                if (!long.TryParse(str, out var value))
                {
                    return false;
                }

                var curDay = ServerTime.Instance.ServerTimestampInDays;
                if (curDay - value < 7)
                {
                    return false;
                }

                PlayerPrefs.SetString("LastRemoveUnusedDay", curDay.ToString());
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        private void InitAllBundleSet()
        {
            _allRemoteBundleHashFile.Clear();

            var allBundleType = AllBundleType;
            foreach (var bundleType in allBundleType)
            {
                if (_remoteBundleMd5s.TryGetValue(bundleType, out Dictionary<string, string> bundleMd5s))
                {
                    foreach (var pair in bundleMd5s)
                    {
                        if (IsBundleFile(pair.Key))
                        {
                            if (!_allRemoteBundleHashFile.Contains(pair.Key))
                            {
                                _allRemoteBundleHashFile.Add(pair.Key);
                            }
                        }
                        else
                        {
                            if (!_allRemoteBundleHashFile.Contains(pair.Key))
                            {
                                _allRemoteBundleHashFile.Add(pair.Key);
                            }
                        }
                    }
                }
            }
        }

        public void TickRemoveLocalUnusedBundle()
        {
            if (_localUnusedBundles.Count == 0)
                return;

            int removeIndex = _localUnusedBundles.Count - 1;
            string path = _localUnusedBundles[removeIndex];
            AssetManager.Instance.IOtool.DeleteGameAsset(path);
            _localUnusedBundles.RemoveAt(removeIndex);
        }

        public void Tick(float delta)
        {
            Perf.BeginSample("VinayGao:AssetBundleSynchro.Tick");
            _statemation.Tick(delta);

            if (NeedUpdateLocalCacheVersion)
            {
                _saveLocalElapse += delta;
                if (_saveLocalElapse > 10.0f)
                {
                    _saveLocalElapse = 0.0f;
                    SaveLocalBundleVersion();
                }
            }

            if (NeedUpdateRemoveBundle)
            {
                _removeUnusedElapse += delta;
                if (_removeUnusedElapse > 0.3f)
                {
                    _removeUnusedElapse = 0.0f;
                    TickRemoveLocalUnusedBundle();
                }
            }

            Perf.EndSample();
        }

        public string GetPackageBundleMd5(string assetBundle)
        {
            string packageMd5;

            string bundleType = GetBundleType(assetBundle);

            var allPackageMd5 = GetPackageBundleMd5s(bundleType);

            allPackageMd5.TryGetValue(assetBundle, out packageMd5);

            return packageMd5;
        }

        public string GetLocalBundleMd5(string assetBundle)
        {
            string localMd5 = string.Empty;

            string bundleType = GetBundleType(assetBundle);

            var allLocalMd5 = GetLocalBundleMd5s(bundleType);

            if (allLocalMd5 != null)
            {
                allLocalMd5.TryGetValue(assetBundle, out localMd5);
            }

            return localMd5;
        }

        public uint GetRemoteBundleCrcCheckValue(string assetBundle)
        {
            uint crcCheckValue = 0;

            string bundleType = GetBundleType(assetBundle);
            if (bundleType == null)
            {
                return 0;
            }

            var allCrcCheckValue = GetRemoteBundleCrcCheckValues(bundleType);

            allCrcCheckValue.TryGetValue(assetBundle, out crcCheckValue);

            return crcCheckValue;
        }

        public string GetRemoteBundleMd5(string assetBundle)
        {
            string remoteMd5 = string.Empty;

            string bundleType = GetBundleType(assetBundle);
            if (bundleType == null)
            {
                return string.Empty;
            }

            var allRemoteMd5 = GetRemoteBundleMd5s(bundleType);

            allRemoteMd5.TryGetValue(assetBundle, out remoteMd5);

            return remoteMd5;
        }

        public string GetBundleHashName(string assetBundle, bool findLocal = false)
        {
            string hashName = GetRemoteBundleMd5(assetBundle);

            if (string.IsNullOrEmpty(hashName) && findLocal)
            {
                hashName = GetLocalBundleMd5(assetBundle);
            }

            return hashName;
        }

        public long GetRemoteBundleSize(string assetBundle)
        {
            string remoteMd5 = string.Empty;

            string bundleType = GetBundleType(assetBundle);

            Dictionary<string, long> sizeDic = null;
            _remoteBundleSize.TryGetValue(bundleType, out sizeDic);
            if (sizeDic != null)
            {
                long size = 0;
                sizeDic.TryGetValue(assetBundle, out size);
                return size;
            }

            //默认一个10k
            return 10240L;
        }

        public bool SyncAssetBundles(List<string> assetBundles, Action finishedCallback,
            Action<int, int, long, long> progressCallback = null, int priority = 999, bool needBI = false)
        {
            return SyncAssetBundlesEx(assetBundles, finishedCallback, progressCallback, priority, needBI) > 0;
        }

        //可能还有一些配置文件，配置文件走过去的更新方式，bundle走新的
        private bool IsBundleFile(string bundleName)
        {
            return bundleName.StartsWith("ota+") || bundleName.StartsWith("static+");
        }

        public long SyncAssetBundlesWithTag(string bundleType,string tag,string tag2,string tag3, Action finishedCallback,
            Action<int, int, long, long> progressCallback = null, int priority = 999, bool needBI = false)
        {
            var remoteBundleMd5S = GetRemoteBundleMd5s(bundleType);
            List<string> tempList = ListPool<string>.Get();
            foreach (var pair in remoteBundleMd5S)
            {
                var tagOK = false;
                var tag2OK = false;
                var tag3OK = false;
                if (string.IsNullOrEmpty(tag))
                {
                    tagOK = true;
                }
                else
                {
                    tagOK = pair.Key.Contains(tag);
                }

                if (!tagOK) continue;
                
                if (string.IsNullOrEmpty(tag2))
                {
                    tag2OK = true;
                }
                else
                {
                    tag2OK = pair.Key.Contains(tag2);
                }
                
                if (!tag2OK) continue;
                
                if (string.IsNullOrEmpty(tag3))
                {
                    tag3OK = true;
                }
                else
                {
                    tag3OK = pair.Key.Contains(tag3);
                }
                
                if (!tag3OK) continue;

                //
                tempList.Add(pair.Key);
            }
            var ret = SyncAssetBundlesEx(tempList, finishedCallback, progressCallback, priority, needBI);
            ListPool<string>.Release(tempList);
            return ret;
        }

        // callback<finishedCount, totalCount>
        public long SyncAssetBundlesEx(List<string> assetBundles, Action finishedCallback, Action<int, int, long, long> progressCallback = null, int priority = 999, bool needBI = false)
        {
            Perf.BeginSample("VinayGao:AssetBundleSynchro.SyncAssetBundlesEx");
            bool needDownload = true;
            int totalCount = 0;
            long totalByte = 0;
            int finishedCount = 0;
            long curByte = 0L;

            var start = Time.realtimeSinceStartup;
            AssetManager.Instance.IOtool.ClearFuncCall();

            foreach (var assetBundle in assetBundles)
            {
                string bundleType = GetBundleType(assetBundle);
                if (string.IsNullOrEmpty(bundleType))
                {
                    NLogger.WarnChannel("AssetBundleSynchro", "{0} skipped, can not find bundle type", assetBundle);
                    continue;
                }

                string bundlePath = AssetManager.Instance.AssetPathProvider.GetLocalBundleFilePath(bundleType, assetBundle);
                string remoteMd5 = GetRemoteBundleMd5(assetBundle);
                
                bool needUpdate = false;
                {
                    string localMd5 = GetLocalBundleMd5(assetBundle);
                    string packageMd5 = GetPackageBundleMd5(assetBundle);
                    if (string.IsNullOrEmpty(remoteMd5))
                    {
                        NLogger.ErrorChannel("AssetBundleSynchro", "{0} skipped, can not find version from remote.rvr={1},rvg={2}", assetBundle, RemoteVersionR, RemoteVersionG);
                        // 非bundle才需要处理
                        if (!IsBundleFile(assetBundle))
                        {
                            AssetManager.Instance.ForceResetAllCache = true;
                        }
                        if (_enableFirebaseReport)
                        {
                            NLogger.Exception(new Exception("[AssetBundleSynchro.SyncAssetBundlesEx]ForceResetAllCache"));
                        }
                        continue;
                    }

                    //       NLogger.LogChannel(Color.grey, "AssetBundleSynchro", "{0}\r\nP {1}\r\nL {2}\r\n R {3}", assetBundle,
                    //           packageMd5, localMd5, remoteMd5);
                    bool isInDocument = AssetManager.Instance.IOtool.HaveBundleAssetInDocument(bundlePath);
                    bool isFoundPackage = false;
                    bool isInPackage = false;

                    // NLogger.Error("SyncAssetBundlesEx bundlePath = {0}, localMd5 = {1}, packageMd5 = {2}, remoteMd5 = {3}, inpack = {4}, isBundle = {5}, isInDoc = {6}", 
                    // bundlePath, localMd5, packageMd5, remoteMd5, AssetManager.Instance.IOtool.HaveGameAssetInPackage(bundlePath), IsBundleFile(assetBundle), isInDocument);
                    // asset bundle deleted by others.
                    if (localMd5 != packageMd5 && !isInDocument)
                    {
                        // NLogger.Error("SyncAssetBundlesEx needUpdate 1");
                        needUpdate = true;
                        localMd5 = packageMd5;
                        SetLocalBundleMd5(bundleType, assetBundle, localMd5);
                    }
                    var h1 = AssetManager.Instance.IOtool.HaveBundleAssetInDocument(bundlePath) || 
                             AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                    if (localMd5 != remoteMd5 || !h1)
                    {
                        // NLogger.Error("SyncAssetBundlesEx needUpdate 2");
                        needUpdate = true;
                        //如果不是bundle，需要依赖拷贝逻辑从包内拿数据，所以还是需要删除旧的json
                        var i1 = IsBundleFile(assetBundle);
                        if (!i1)
                        {
                            //再删除旧的，这里会跟后续的冲突，由于文件唯一，即使md5不一样，也可以用本地文件作为下载结果
                            if (isInDocument)
                            {
                                NLogger.LogChannel(Color.grey, "AssetBundleSynchro", $"delete unused bundle {bundlePath} from document");
                                AssetManager.Instance.IOtool.DeleteGameAsset(bundlePath);
                            }
                        }
                    }
                    else if (!isInDocument)
                    {
                        isFoundPackage = true;
                        isInPackage = AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                        if (!isInPackage)
                        {
                            // NLogger.Error("SyncAssetBundlesEx needUpdate 3");
                            needUpdate = true;
                        }
                    }
                    var h2 = AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                    if (needUpdate && packageMd5 == remoteMd5 && h2)
                    {
                        // NLogger.Error("SyncAssetBundlesEx needUpdate 4");
                        needUpdate = false;
                        SetLocalBundleMd5(bundleType, assetBundle, remoteMd5);
                        totalCount++;
                        finishedCount++;
                    }

                    //本地md5对比失效的时候，由于文件有唯一性，所以如果本地有这个文件，也认为是下载完毕
                    //这个只适合bundle，不适合各种配置json，配置json不自带版本描述
                    var i2 = IsBundleFile(assetBundle);
                    if (needUpdate && i2 && isInDocument)
                    {
                        // NLogger.Error("SyncAssetBundlesEx needUpdate 5");
                        needUpdate = false;
                        SetLocalBundleMd5(bundleType, assetBundle, remoteMd5);
                        totalCount++;
                        finishedCount++;
                    }
                }
                
                if (needUpdate)
                {
                    // NLogger.Error("SyncAssetBundlesEx needUpdate DownloadAsset");
                    // down load from remote server 
                    totalCount++;

                    var size = GetRemoteBundleSize(assetBundle);
                    totalByte += size;

                    if (_tutorialEnd && needBI)
                    {
                        EventManager.Instance.TriggerEvent(new DownLoadBundleEvent(assetBundle));
                    }

                    string remoteVersion = GetRemoteVersion(bundleType);
                    var url = AssetManager.Instance.AssetPathProvider.GetRemoteBundleUrl(RemoteCDN, bundleType,
                        remoteVersion, assetBundle);

                    var savePath = AssetManager.Instance.IOtool.GetExternalFullGameAssetPath(
                        AssetManager.Instance.AssetPathProvider.GetLocalBundleFilePath(bundleType, assetBundle));

                    var crcCheckValue = GetRemoteBundleCrcCheckValue(assetBundle);

                    NLogger.Log("DownloadAsset Start assetBundle:{0}, priority:{1}", assetBundle, priority);

                    AssetManager.Instance.DownLoader.DownloadAsset(
                        url,
                        savePath,
                        delegate(bool result, HttpResponseData response)
                        {
                            Perf.BeginSample("AssetBundleSynchro.DownloadAsset.Complete");
                            NLogger.Log("DownloadAsset Finish assetBundle:{0}", assetBundle);

                            if (result)
                            {
                                finishedCount++;

                                if (!_allCompleteBundle.Contains(assetBundle))
                                {
                                    _allCompleteBundle.Add(assetBundle);
                                }

                                if (_allDownloadingBundle.Contains(assetBundle))
                                {
                                    _allDownloadingBundle.Remove(assetBundle);
                                }

                                SetLocalBundleMd5(bundleType, assetBundle, remoteMd5);
                                
                                AssetManager.Instance.IOtool.UpdateBundleAssetInDocument(bundlePath);

                                if (progressCallback != null)
                                {
                                    long byteCount = 0;
                                    if (response != null)
                                    {
                                        var bytes = response.responseContent;
                                        byteCount = bytes != null ? bytes.Length : 0;
                                        // byteCount = (long) response.downloadedBytes;
                                    }

                                    progressCallback(finishedCount, totalCount, byteCount, totalByte);
                                }

                                if (totalCount == finishedCount)
                                {
                                    if (finishedCallback != null)
                                    {
                                        finishedCallback();
                                    }
                                }
#if MO_LOG || UNITY_DEBUG || MO_DEBUG
                                {
                                    if (_logSw != null)
                                    {
                                        _logSw.WriteLine($"[{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}][{_tag}][{finishedCount}/{totalCount}]{assetBundle},{savePath},{url}");
                                    }
                                }
#endif
                            }
                            else
                            {
                                NLogger.ErrorChannel("AssetBundleSynchro", "UpdateReSynchro Download Error Url:{0} SavePath:{1} bundleName:{2}",
                                    url, savePath, assetBundle);
                            }
                            Perf.EndSample();
                        }, priority, crcCheckValue,(uint)size
                    );

                    if (!_allDownloadingBundle.Contains(assetBundle))
                    {
                        _allDownloadingBundle.Add(assetBundle);
                    }
                }
            }

            if (progressCallback != null)
            {
                progressCallback(finishedCount, totalCount, 0, totalByte);
            }

            if (totalCount == finishedCount)
            {
                needDownload = false;
                if (finishedCallback != null)
                {
                    finishedCallback();
                }
            }
            Perf.EndSample();

            return totalByte;
        }

        public bool CheckAssetBundlesNeedSync(List<string> assetBundles, bool deleteOld = false)
        {
            return CheckAssetBundlesNeedSyncEx(assetBundles, null, deleteOld) > 0;
        }

        //因为是检测是否需要下载，用比较简化的方式确认，
        //如果本地md5与远程相等或者包的md5与远程相等，就认为不需要下载，排除一些特殊情况，加快检查速度
        public int CheckAssetBundlesNeedSyncEx(List<string> assetBundles, List<string> outputList, bool deleteOld = false)
        {
            Perf.BeginSample("AssetBundleSynchro.CheckAssetBundlesNeedSyncEx");
            bool needDownload = true;
            int totalCount = 0;
            int finishedCount = 0;

            foreach (var assetBundle in assetBundles)
            {
                if (_allCompleteBundle.Contains(assetBundle))
                {
                    continue;
                }

                if (_allDownloadingBundle.Contains(assetBundle))
                {
                    totalCount++;
                    if (outputList != null)
                    {
                        outputList.Add(assetBundle);
                    }

                    continue;
                }

                string bundleType = GetBundleType(assetBundle);
                if (string.IsNullOrEmpty(bundleType))
                {
                    NLogger.WarnChannel("AssetBundleSynchro", "{0} skipped, can not find bundle type", assetBundle);
                    continue;
                }

                var bundleFileName = assetBundle;
                if (AssetBundleConfig.IsBundle(assetBundle))
                {
                    bundleFileName = GetBundleHashName(assetBundle);
                }

                string bundlePath = AssetManager.Instance.AssetPathProvider.GetLocalBundleFilePath(bundleType, bundleFileName);
                string remoteMd5 = GetRemoteBundleMd5(assetBundle);
                bool needUpdate = false;

                /*    if (IsBundleFile(assetBundle))
                    {
                        var assetInDoc = AssetManager.Instance.IOtool.HaveGameAssetInDocument(bundlePath);
                        if (!assetInDoc)
                        {
                            needUpdate = true;
                        }
                    }
                    else */
                {
                    string localMd5 = GetLocalBundleMd5(assetBundle);
                    string packageMd5 = GetPackageBundleMd5(assetBundle);

                    if (string.IsNullOrEmpty(remoteMd5))
                    {
                        NLogger.ErrorChannel("AssetBundleSynchro", "{0} skipped, can not find version from remote",
                            assetBundle);
                        continue;
                    }

                    //       NLogger.LogChannel(Color.grey, "AssetBundleSynchro", "{0}\r\nP {1}\r\nL {2}\r\n R {3}", assetBundle,
                    //           packageMd5, localMd5, remoteMd5);

                    bool isInDocument = AssetManager.Instance.IOtool.HaveBundleAssetInDocument(bundlePath);
                    
                    bool isFoundPackage = false;
                    bool isInPackage = false;

                    // asset bundle deleted by others.
                    if (localMd5 != packageMd5 && !isInDocument)
                    {
                        needUpdate = true;
                        localMd5 = packageMd5;
                    }

                    var g = AssetManager.Instance.IOtool.HaveBundleAssetInDocument(bundlePath) || 
                            AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                    if (localMd5 != remoteMd5 || !g)
                    {
                        needUpdate = true;
                    }
                    else if (!isInDocument)
                    {
                        isFoundPackage = true;
                        isInPackage = AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                        if (!isInPackage)
                        {
                            needUpdate = true;
                        }
                    }

                    var h = AssetManager.Instance.IOtool.HaveBundleAssetInPackage(bundlePath);
                    if (needUpdate && packageMd5 == remoteMd5 && h)
                    {
                        needUpdate = false;
                    }

                    //本地md5对比失效的时候，由于文件有唯一性，所以如果本地有这个文件，也认为是下载完毕,同时更新md5
                    //这个只适合bundle，不适合各种配置json，配置json不自带版本描述
                    if (needUpdate && IsBundleFile(assetBundle) && isInDocument)
                    {
                        needUpdate = false;
                        SetLocalBundleMd5(bundleType, assetBundle, remoteMd5);
                    }
                }

                if (!needUpdate)
                {
                    _allCompleteBundle.Add(assetBundle);
                }
                else
                {
                    totalCount++;
                    if (outputList != null)
                    {
                        outputList.Add(assetBundle);
                    }
                }
            }

            Perf.EndSample();
            return totalCount;
        }

        //这个接口默认bundle都开始了下载
        public int CheckAssetBundlesSyncNotComplete(List<string> assetBundles)
        {
            int totalCount = 0;
            foreach (var assetBundle in assetBundles)
            {
                if (_allCompleteBundle.Contains(assetBundle))
                {
                    continue;
                }

                totalCount++;
            }

            return totalCount;
        }

        public void SetBundleUpdateType(string bundleType, string updateType, List<string> bundles)
        {
            if (!_allBundleUpdateTypes.ContainsKey(bundleType))
            {
                _allBundleUpdateTypes.Add(bundleType, new Dictionary<string, List<string>>());
            }

            var updateTypes = _allBundleUpdateTypes[bundleType];

            if (updateTypes.ContainsKey(updateType))
            {
                updateTypes.Remove(updateType);
            }

            updateTypes.Add(updateType, bundles);
        }

        public void RegenerateBundleTypeMap()
        {
            _allBundleType.Clear();

            foreach (var bundleTypeKeyValue in _allBundleUpdateTypes)
            {
                foreach (var updateTypeKeyValue in bundleTypeKeyValue.Value)
                {
                    foreach (var bundleName in updateTypeKeyValue.Value)
                    {
                        if (!_allBundleType.ContainsKey(bundleName))
                        {
                            _allBundleType.Add(bundleName, bundleTypeKeyValue.Key);
                        }
                        else
                        {
                            NLogger.ErrorChannel("AssetBundleSynchro", "bundle already exists {0}", bundleName);
                        }
                    }
                }
            }
        }


        public List<string> GetBundlesWithUpdateType(string bundleType, string updateType)
        {
            if (_allBundleUpdateTypes.ContainsKey(bundleType))
            {
                var updateTypes = _allBundleUpdateTypes[bundleType];
                if (updateTypes.ContainsKey(updateType))
                {
                    return updateTypes[updateType];
                }
            }

            return new List<string>();
        }

        // static, ota, dynamic
        public string GetBundleUpdateType(string assetBundle)
        {
            foreach (var bundleTypeKeyValue in _allBundleUpdateTypes)
            {
                foreach (var buneleUpdateTypeKeyValue in bundleTypeKeyValue.Value)
                {
                    if (buneleUpdateTypeKeyValue.Value.Contains(assetBundle))
                    {
                        return buneleUpdateTypeKeyValue.Key;
                    }
                }
            }

            return string.Empty;
        }

        public void SetPackageBundleMd5s(string bundleType, Dictionary<string, string> bundleInfos)
        {
            GetBundleListInfos(bundleInfos, out var bundleMd5s, out var bundleCrcCheckValues);

            if (_packageBundleMd5.ContainsKey(bundleType))
            {
                _packageBundleMd5.Remove(bundleType);
            }

            _packageBundleMd5.Add(bundleType, bundleMd5s);
        }

        public void SetLocalBundleMd5s(string bundleType, Dictionary<string, string> bundleMd5s)
        {
            lock (_localBundleMd5s)
            {
                if (_localBundleMd5s.ContainsKey(bundleType))
                {
                    _localBundleMd5s.Remove(bundleType);
                }

                _localBundleMd5s.Add(bundleType, bundleMd5s);
            }
        }

        public void RemoveUnusedLocalBundleMd5s(string bundleType)
        {
            lock (_localBundleMd5s)
            {
                if (_localBundleMd5s.ContainsKey(bundleType) && _remoteBundleMd5s.ContainsKey(bundleType))
                {
                    _removeList.Clear();
                    var remoteBundleDic = _remoteBundleMd5s[bundleType];
                    var localBundleDic = _localBundleMd5s[bundleType];
                    foreach (var pair in localBundleDic)
                    {
                        if (!remoteBundleDic.ContainsKey(pair.Key))
                        {
                            _removeList.Add(pair.Key);
                        }
                    }

                    foreach (var bundle in _removeList)
                    {
                        localBundleDic.Remove(bundle);
                    }

                    _removeList.Clear();
                }
            }
        }

        public void RemoveUnusedLocalBundleMd5s()
        {
            lock (_localBundleMd5s)
            {
                var allBundleType = AllBundleType;
                foreach (var bundleType in allBundleType)
                {
                    if (_localBundleMd5s.ContainsKey(bundleType) && _remoteBundleMd5s.ContainsKey(bundleType))
                    {
                        _removeList.Clear();
                        var remoteBundleDic = _remoteBundleMd5s[bundleType];
                        var localBundleDic = _localBundleMd5s[bundleType];
                        foreach (var pair in localBundleDic)
                        {
                            if (!remoteBundleDic.ContainsKey(pair.Key))
                            {
                                _removeList.Add(pair.Key);
                            }
                        }

                        foreach (var bundle in _removeList)
                        {
                            localBundleDic.Remove(bundle);
                        }

                        _removeList.Clear();
                    }
                }
            }
        }

        public void SetRemoteBundleMd5s(string bundleType, Dictionary<string, string> bundleInfos)
        {
            GetBundleListInfos(bundleInfos, out var bundleMd5s, out var bundleCrcCheckValues);

            if (_remoteBundleMd5s.ContainsKey(bundleType))
            {
                _remoteBundleMd5s.Remove(bundleType);
            }

            _remoteBundleMd5s.Add(bundleType, bundleMd5s);

            if (_remoteBundleCrcCheckValues.ContainsKey(bundleType))
            {
                _remoteBundleCrcCheckValues.Remove(bundleType);
            }

            _remoteBundleCrcCheckValues.Add(bundleType, bundleCrcCheckValues);
        }

        public void GetBundleListInfos(Dictionary<string, string> bundleInfos, out Dictionary<string, string> bundleMd5s, out Dictionary<string, uint> bundleCrcCheckValues,
            bool throwFailed = true)
        {
            bundleMd5s = new Dictionary<string, string>();
            bundleCrcCheckValues = new Dictionary<string, uint>();

            try
            {
                foreach (var pair in bundleInfos)
                {
                    if (AssetBundleConfig.Instance.TryGetBundleInfo(pair.Value, out var md5, out var crc))
                    {
                        bundleMd5s.Add(pair.Key, md5);
                        bundleCrcCheckValues.Add(pair.Key, uint.Parse(crc));
                    }
                    else
                    {
                        if (throwFailed)
                        {
                            NLogger.Error("GetBundleListInfos Failed");

                            LoadExistBundleFail fail = new LoadExistBundleFail("", "BundleList");
                            EventManager.Instance.TriggerEvent(fail);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NLogger.Error("GetBundleListInfos Exception:{0}", e.ToString());

                if (throwFailed)
                {
                    LoadExistBundleFail fail = new LoadExistBundleFail("", "BundleList");
                    EventManager.Instance.TriggerEvent(fail);
                }
            }
        }


        public void SetRemoteBundleSize(string bundleType, Dictionary<string, long> bundleSize)
        {
            if (_remoteBundleSize.ContainsKey(bundleType))
            {
                _remoteBundleSize.Remove(bundleType);
            }

            _remoteBundleSize.Add(bundleType, bundleSize);
        }

        public Dictionary<string, string> GetPackageBundleMd5s(string bundleType)
        {
            if (_packageBundleMd5.ContainsKey(bundleType))
            {
                return _packageBundleMd5[bundleType];
            }

            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetLocalBundleMd5s(string bundleType)
        {
            if (_localBundleMd5s.ContainsKey(bundleType))
            {
                return _localBundleMd5s[bundleType];
            }

            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetRemoteBundleMd5s(string bundleType)
        {
            if (_remoteBundleMd5s.ContainsKey(bundleType))
            {
                return _remoteBundleMd5s[bundleType];
            }

            return new Dictionary<string, string>();
        }

        public Dictionary<string, uint> GetRemoteBundleCrcCheckValues(string bundleType)
        {
            if (_remoteBundleCrcCheckValues.ContainsKey(bundleType))
            {
                return _remoteBundleCrcCheckValues[bundleType];
            }

            return new Dictionary<string, uint>();
        }


        public string GetPackageVersion(string bundleType)
        {
            try
            {
                string versionJson = AssetManager.Instance.IOtool.ReadStreamingAssetAsText(AssetManager.Instance.AssetPathProvider.BUNDLE_VERSION_FILE());

                if (!string.IsNullOrEmpty(versionJson))
                {
                    var localBundleVersion = AssetManager.Instance.DataParser.FromJson<Dictionary<string, string>>(versionJson);

                    return localBundleVersion[bundleType];
                }
            }
            catch (Exception e)
            {
                NLogger.Error("GetPackageVersion {0} Exception {1} ", bundleType, e.ToString());
            }

            return "0";
        }

        public void RemoteUnusedBundleFiles(string bundleType)
        {
            //???
        }

        public void AddDecompressing(string decompress)
        {
            _allDecompressing.Add(decompress);
        }

        public void RemoveDecompressing(string decompress)
        {
            _allDecompressing.Remove(decompress);
        }

        public bool IsDecompressing(string bundle)
        {
            return _allDecompressing.Contains(bundle);
        }

        public bool OpenDecompress
        {
            get { return _enableDecompress; }
            set { _enableDecompress = value; }
        }

        public List<string> GetSyncList(List<string> prefabs, List<string> inputParam = null)
        {
            List<string> results = inputParam;
            if (results == null)
            {
                results = new List<string>(4);
            }

            foreach (var prefab in prefabs)
            {
                var data = BundleAssetDataManager.Instance.GetAssetData(prefab);
                if (data != null && !string.IsNullOrEmpty(data.BundleName) && !results.Contains(data.BundleName))
                {
                    results.Add(data.BundleName);
                    var deps = BundleDependence.Instance.GetBundleDependence(data.BundleName);
                    if (deps != null)
                    {
                        foreach (var dep in deps.dependences)
                        {
                            if (!string.IsNullOrEmpty(dep) && !results.Contains(dep))
                            {
                                results.Add(dep);
                            }
                        }
                    }
                }
            }

            return results;
        }

        //R or G ...
        public string GetBundleType(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                return string.Empty;
            }

            if (bundleName.StartsWith("ota+"))
            {
                return "G";
            }

            if (bundleName.StartsWith("static+"))
            {
                return "R";
            }

            string result = string.Empty;
            if (!_allBundleType.TryGetValue(bundleName, out result))
            {
                NLogger.Warn("bundle {0} can not found in bundle type config file", bundleName);
            }

            return result;
        }

        public string GetBundleLocalPath(string bundleName, string hashName = null)
        {
            if (string.IsNullOrEmpty(hashName))
            {
                return AssetManager.Instance.AssetPathProvider.GetBundleLocalPath(GetBundleType(bundleName), bundleName);
            }

            return AssetManager.Instance.AssetPathProvider.GetBundleLocalPath(GetBundleType(bundleName), hashName);
        }

        public string GetPackageBundleListFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetPackageBundleListFilePath(bundleType);
        }

        public string GetPackageBundleSizeFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetPackageBundleSizeFilePath(bundleType);
        }

        public string GetBundlePath(string fileName, string hashName = null)
        {
            return AssetManager.Instance.AssetPathProvider.GetBundlePath(fileName, hashName);
        }

        public string GetLocalBundleListFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetLocalBundleListFilePath(bundleType);
        }

        public string GetPackageLocalBundleListFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetPackageLocalBundleListFilePath(bundleType);
        }

        public string GetLocalCacheBundleListFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetLocalCacheBundleListFilePath(bundleType);
        }

        public string GetLocalBundleSizeFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetLocalBundleSizeFilePath(bundleType);
        }

        public string GetPackageLocalBundleSizeFilePath(string bundleType)
        {
            return AssetManager.Instance.AssetPathProvider.GetPackageLocalBundleSizeFilePath(bundleType);
        }

        //http://rob-dev.funplusgame.com/assets/bundle/0.0.1/R/Android/bundle_list.json
        public string GetRemoteBundleListUrl(string bundleType, string version)
        {
            return GetRemoteBundleUrl(bundleType, version, "bundle_list.json");
        }

        public string GetRemoteBundleSizeUrl(string bundleType, string version)
        {
            return GetRemoteBundleUrl(bundleType, version, "bundle_size.json");
        }

        public string GetRemoteBundleUrl(string bundleType, string version, string bundleName)
        {
            return AssetManager.Instance.AssetPathProvider.GetRemoteBundleUrl(RemoteCDN, bundleType, version, bundleName);
        }

        public string GetRemoteBundleUrl(string bundleType, string version, string bundleName, string plateform)
        {
            return AssetManager.Instance.AssetPathProvider.GetRemoteBundleUrl(RemoteCDN, bundleType, version, bundleName, plateform);
        }

        public string GetRemoteBundleListFilePath(string bundleType, string version)
        {
            return AssetManager.Instance.AssetPathProvider.GetRemoteBundleListFilePath(bundleType, version);
        }

        public string GetRemoteBundleSizeFilePath(string bundleType, string version)
        {
            return AssetManager.Instance.AssetPathProvider.GetRemoteBundleSizeFilePath(bundleType, version);
        }

        public string GetRemoteBundleTypeFilePath(string bundleType, string version)
        {
            return AssetManager.Instance.AssetPathProvider.GetRemoteBundleTypeFilePath(bundleType, version);
        }

        public int GetDownloadingBundlesCount()
        {
            return _allDownloadingBundle.Count;
        }

        private bool _enableFirebaseReport = false;
        public void EnableFirebaseReport(bool v)
        {
#if UNITY_EDITOR
            NLogger.Error($"AssetBundleSynchro.EnableFirebaseReport:{v}");
#endif
            _enableFirebaseReport = v;
        }
        private bool _enableForceResetAllCache = false;
        public void EnableForceResetAllCache(bool v)
        {
#if UNITY_EDITOR
            NLogger.Error($"AssetBundleSynchro.EnableForceResetAllCache:{v}");
#endif
            _enableForceResetAllCache = v;
        }
    }
}
