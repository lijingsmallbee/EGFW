using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NexgenDragon
{
    internal class AssetHttpApi : IHttpApi
    {
        private const string PathFormat = "{0}.download{1}";

        private bool _isWindowPlayer = (Application.platform == RuntimePlatform.WindowsPlayer) 
                                       || (Application.platform == RuntimePlatform.WindowsEditor);
        
        private struct Response
        {
            public string savePath;
            public string downloadPath;
            public uint crcCheckValue;
            public Action<HttpResponseData> callback;
            public bool isRetry;
        }

        private readonly Dictionary<AssetHttpOperation, Response> _operations =
            new Dictionary<AssetHttpOperation, Response>();

        private readonly List<AssetHttpOperation> _removals = new List<AssetHttpOperation>();

        private Crc32Algorithm crcChecker = new Crc32Algorithm();

        public bool IsThreadSupported
        {
            get { return false; }
        }

        public bool EnableProxy { get; set; }
        public string ProxyIP { get; set; }
        public int ProxyPort { get; set; }

        public IHttpAsyncOperation Send(HttpRequestData requestData, Action<HttpResponseData> callback)
        {
            string downloadPath;
            var downloader = MakeUniqueDownloadPath(requestData.savePath, 20, requestData.isRetry, out downloadPath);
            if (downloader == null) // 创建文件失败
            {
                var responseData = new HttpResponseData
                {
                    responseCode = -100,
                    url = requestData.url,
                    savePath = requestData.savePath,
                };

                if (null != callback)
                {
                    callback(responseData);
                }

                return null;
            }
            string newUrl = requestData.url;
            if (_isWindowPlayer)
            {
                newUrl = requestData.url.Replace("\\", "/");
            }
            
            var request = new UnityWebRequest {url = newUrl};

            switch (requestData.method)
            {
                case HttpMethod.Get:
                    request.method = UnityWebRequest.kHttpVerbGET;
                    break;

                case HttpMethod.Post:
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    break;
            }

            request.uploadHandler = new UploadHandlerRaw(requestData.requestContent);
            request.disposeUploadHandlerOnDispose = true;
            
            request.downloadHandler = downloader;
            request.disposeDownloadHandlerOnDispose = true;

            foreach (var header in requestData.headersDict)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            var operation = new AssetHttpOperation(request.SendWebRequest());
            _operations.Add(operation, new Response
            {
                savePath = requestData.savePath,
                downloadPath = downloadPath,
                crcCheckValue = requestData.crcCheckValue,
                callback = callback,
                isRetry = requestData.isRetry
            });

            if (requestData.isRetry)
            {
                NLogger.Error("[AssetHttpApi]Send Retry: URL = {0}, DownloadPath = {1}, SavePath = {2}", requestData.url, downloadPath, requestData.savePath);
            }

            return operation;
        }

        private static DownloadHandlerFile MakeUniqueDownloadPath(string savePath, int maxRetry, bool isRetry, out string downloadPath)
        {
            var index = 0;
            
            do
            {
                downloadPath = string.Format(PathFormat, savePath, index++);
                try
                {
                    if (isRetry && File.Exists(downloadPath))
                    {
                        continue;
                    }
                    
                    var downloader = new DownloadHandlerFile(downloadPath) {removeFileOnAbort = true};
                    return downloader;
                }
                catch
                {
                    // 创建文件失败
                }
            } while (index < maxRetry);

            return null;
        }

        public void Release()
        {

        }

        public void Reset()
        {
            NLogger.TraceChannel("AssetHttpApi", "[AssetHttpApi]Reset");

            foreach (var pair in _operations)
            {
                var operation = pair.Key;
                if (!operation.IsDone)
                {
                    operation.Abort();
                }

                operation.Request.Dispose();
            }

            _operations.Clear();
            _removals.Clear();
        }

        public void Tick(float dt)
        {
            _removals.Clear();

            foreach (var pair in _operations)
            {
                var operation = pair.Key;
                var response = pair.Value;

                var error = operation.Request.error;
                if (!string.IsNullOrEmpty(error))
                {
                    var responseData = new HttpResponseData
                    {
                        responseCode = -100,
                        responseContent = Encoding.ASCII.GetBytes(operation.Request.error),
                        url = operation.Request.url,
                        savePath = response.savePath,
                        downloadedBytes = operation.Request.downloadedBytes
                    };

                    if (error.Contains("Unable to write data"))
                    {
                        EventManager.Instance.TriggerEvent(new IOExceptionEvent(IOExceptionEvent.Exception.Write));
                    }
                    
                    if (response.isRetry)
                    {
                        NLogger.Error("[AssetHttpApi]Update operation Request error: URL = {0}, DownloadPath = {1}, SavePath = {2}, responseCode={3}",
                            responseData.url, response.downloadPath, response.savePath, responseData.responseCode);
                    }
                    
                    // 如果下载失败，文件不会被自动删除，这里需要手动删除
                    if (File.Exists(response.downloadPath))
                    {
                        File.Delete(response.downloadPath);
                    }

                    if (null != response.callback)
                    {
                        response.callback(responseData);
                    }

                    operation.Request.Dispose();
                    _removals.Add(operation);
                }
                else if (operation.IsDone)
                {
                    var responseData = new HttpResponseData
                    {
                        responseCode = operation.Request.responseCode,
                        responseContent = null, // 内容已经写入硬盘
                        url = operation.Request.url,
                        savePath = response.savePath,
                        downloadedBytes = operation.Request.downloadedBytes
                    };

                    if (File.Exists(response.savePath))
                    {
                        File.Delete(response.savePath);
                    }
                    
                    if (response.isRetry)
                    {
                        NLogger.Error("[AssetHttpApi]Update operation IsDone: URL = {0}, DownloadPath = {1}, SavePath = {2}, responseCode={3}",
                            responseData.url, response.downloadPath, response.savePath, responseData.responseCode);
                    }

                    if (response.crcCheckValue > 0)
                    {
                        // TaskManager.Instance.RunAsync(() =>
                        var taskPool = TaskPoolManager.Instance.Default;
                        taskPool.RunAsync(() =>
                        {
                            uint downloadCrcValue = 0;
                            lock (crcChecker)
                            {
                                crcChecker.Initialize();
                                downloadCrcValue = crcChecker.ComputeCrcValue(response.downloadPath);
                            }

                            // do in main queue
                            // TaskManager.Instance.QueueOnMainThread(() =>
                            taskPool.QueueOnMainThread(() =>
                            {
                                if (operation.IsAborted)
                                {
                                    NLogger.Error("[AssetHttpApi]Update operation IsAborted: URL = {0}, DownloadPath = {1}, SavePath = {2}",
                                        responseData.url, response.downloadPath, response.savePath);
                                }
                                else
                                {
                                    if (downloadCrcValue == response.crcCheckValue)
                                    {
                                        ProcessOnOperationDone(response, responseData);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (File.Exists(response.downloadPath))
                                            {
                                                File.Delete(response.downloadPath);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            NLogger.Error("[AssetHttpApi]Update: URL = {0}, DownloadPath = {1}, SavePath = {2}, Exception = {3}", responseData.url, response.downloadPath, response.savePath,
                                                e.ToString());
                                        }
                                    
                                        responseData.responseCode = -200;

                                        NLogger.Error(
                                            "[AssetHttpApi]Update: URL = {0}, DownloadPath = {1}, SavePath = {2}, DownloadCrc = {3}, CrcCheck = {4}",
                                            responseData.url, response.downloadPath, response.savePath, downloadCrcValue, response.crcCheckValue);
                                    
                                        if (null != response.callback)
                                        {
                                            response.callback(responseData);
                                        }
                                    }
                                }
                            });
                        });
                    }
                    else
                    {
                        ProcessOnOperationDone(response, responseData);
                    }

                    operation.Request.Dispose();
                    _removals.Add(operation);
                }
                else if (operation.IsAborted)
                {
                    _removals.Add(operation);
                }
            }

            foreach (var operation in _removals)
            {
                _operations.Remove(operation);
            }
        }


        private void ProcessOnOperationDone(Response response, HttpResponseData responseData)
        {
            if (AssetConfig.BundleNeedDecompress(response.downloadPath)) // IsBundle(response.downloadPath) && AssetBundleSynchro.Instance.OpenDecompress)
            {
                var tempDepressPath = response.downloadPath + "_temp";

                AssetUtils.DecompressLzma(response.downloadPath, tempDepressPath, (success) =>
                {
                    if (!success)
                    {
                        if (File.Exists(tempDepressPath))
                        {
                            File.Delete(tempDepressPath);
                        }

                        responseData.responseCode = -100;

                        NLogger.Error("[AssetHttpApi]not success: URL = {0}, DownloadPath = {1}, SavePath = {2}", responseData.url, response.downloadPath, response.savePath);

                        AssetServiceRequest.IOExceptionTimes++;
                    }
                    else
                    {
                        try
                        {
                            File.Move(tempDepressPath, response.savePath);
                        }
                        catch (Exception e)
                        {
                            responseData.responseCode = -100;

                            NLogger.Error("[AssetHttpApi]Move fail: URL = {0}, DownloadPath = {1}, SavePath = {2}, Exception = {3}", responseData.url, response.downloadPath, response.savePath,
                                e.ToString());

                            AssetServiceRequest.IOExceptionTimes++;
                        }
                    }

                    try
                    {
                        File.Delete(response.downloadPath);
                    }
                    catch // (Exception e)
                    {
                        NLogger.Error("[AssetHttpApi]Delete failed:DownloadPath = {0}", response.downloadPath);
                    }
                    
                    if (response.isRetry)
                    {
                        NLogger.Error("[AssetHttpApi]ProcessOnOperationDone Retry: URL = {0}, DownloadPath = {1}, SavePath = {2}, responseCode={3}", responseData.url, response.downloadPath,
                            response.savePath, responseData.responseCode);
                    }

                    if (null != response.callback)
                    {
                        response.callback(responseData);
                    }
                });

                /*    var filename = Path.GetFileNameWithoutExtension(response.savePath);

                    
                    if (AssetBundleSynchro.Instance.IsDecompressing(filename))
                    {
                        NLogger.Error("bundle {0} is decompressing,error happened",filename);
                    }

                    var all = AssetBundle.GetAllLoadedAssetBundles();
                    foreach (var loaded in all)
                    {
                        if (loaded.name == filename)
                        {
                            NLogger.Error("will decompress a loaded bundle {0}",filename);
                        }
                    }
                    AssetBundleSynchro.Instance.AddDecompressing(filename);
                    
                    var tempDepressPath = response.downloadPath + "_temp";
                    
                    var bundleOperation = AssetBundle.RecompressAssetBundleAsync(response.downloadPath,
                        tempDepressPath,
                        BuildCompression.Uncompressed, 0, ThreadPriority.High);
                                            
                    

                    bundleOperation.completed += delegate(AsyncOperation asyncOperation)
                    {
                        if (!bundleOperation.success)
                        {
                            if (File.Exists(tempDepressPath))
                            {
                                File.Delete(tempDepressPath);
                            }
                            
                            responseData.responseCode = -100;

                            NLogger.Error(
                                "[AssetHttpApi]Update: URL = {0}, DownloadPath = {1}, SavePath = {2}, Exception = {3}",
                                responseData.url, response.downloadPath, response.savePath, bundleOperation.humanReadableResult);
                        }
                        else
                        {
                            AssetBundleSynchro.Instance.RemoveDecompressing(filename);
                            try
                            {
                                if (AssetBundleSynchro.Instance.IsDecompressing(filename))
                                {
                                    NLogger.Error("Move file error,{0} is decompressing",filename);
                                }
                                File.Move(tempDepressPath, response.savePath);
                            }
                            catch (Exception e)
                            {
                                responseData.responseCode = -100;

                                NLogger.Error(
                                    "[AssetHttpApi]Update: URL = {0}, DownloadPath = {1}, SavePath = {2}, Exception = {3}",
                                    responseData.url, response.downloadPath, response.savePath, e.ToString());
                            } 
                        }

                        try
                        {
                            File.Delete(response.downloadPath);
                        }
                        catch (Exception e)
                        {
                            NLogger.Error(response.downloadPath + "Deleted failed");
                        }
                        
                        if (null != response.callback)
                        {
                            response.callback(responseData);                                
                        }                                                                                
                    };    */
            }
            else
            {
                try
                {
                    File.Move(response.downloadPath, response.savePath);
                }
                catch (Exception e)
                {
                    responseData.responseCode = -100;

                    var downloadExist = File.Exists(response.downloadPath);
                    var saveExist = File.Exists(response.savePath);

                    NLogger.Error("[AssetHttpApi]Update: URL = {0}, DownloadPath = {1}, SavePath = {2}, Exception = {3}, downloadExist = {4}, saveExist = {5}", responseData.url, response.downloadPath,
                        response.savePath, e.ToString(), downloadExist, saveExist);

                    AssetServiceRequest.IOExceptionTimes++;
                }
                
                if (response.isRetry)
                {
                    var downloadExist = File.Exists(response.downloadPath);
                    var saveExist = File.Exists(response.savePath);

                    NLogger.Error("[AssetHttpApi]ProcessOnOperationDone Retry: URL = {0}, DownloadPath = {1}, SavePath = {2}, responseCode={3}, downloadExist = {4}, saveExist = {5}", responseData.url,
                        response.downloadPath,
                        response.savePath, responseData.responseCode, downloadExist, saveExist);
                }

                if (null != response.callback)
                {
                    response.callback(responseData);
                }
            }
        }
        
        private bool IsBundle(string name)
        {            
            return name.Contains("ota+") || name.Contains("static+");
        }
    }
}