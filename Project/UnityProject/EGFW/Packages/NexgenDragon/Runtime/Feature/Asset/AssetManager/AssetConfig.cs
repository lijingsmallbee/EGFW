using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Result = UnityEngine.Networking.UnityWebRequest.Result;

public class AssetConfig
{
    public const string ota_prefix = "ota+";
    public const string ota_cache_prefix = "ota+cache_";
    public const string ota_cache_custom = "ota+cache_custom_";
    public const string static_custom_lua = "static+custom_lua";
    public const string static_prefix = "static+";
    public static bool BundleNeedDecompress(string relativePath)
    {
        if ((readAssetBundleInStreamingAssets && IsAssetBundlePath(relativePath)) || (readFileInStreamingAssets && !IsAssetBundlePath(relativePath)))
            return false;

        return relativePath.Contains("static+");
        return relativePath.Contains("static+") || relativePath.Contains("ota+cache_");
    }

    public const float AssetProcessTimeOut = 10f;

    //-----------------------------------------------------------------------------

    public static string assetBundleExt = ".unity3d";
    public static bool readAssetBundleInStreamingAssets = true;
    public static bool readFileInStreamingAssets;

    //public static bool IsAssetBundlePath(string path) => path.Contains("ota+")|| path.Contains("static+");
    //为了兼容以前的资源，lzma 的流程无法修改，static 的必须还是 static
    public static bool IsAssetBundlePath(string path) => path.Contains("ota+");
    public static string RelativePathToPathInStreamingAssets(string relativePath) => $"{Application.streamingAssetsPath}/{relativePath}";
    public static byte[] WebGetData(string url, int timeout = 0) => WebGet(url, WebGetType.Data, timeout).data;

    static WebGetStats WebGet(string url, WebGetType typeMask, int timeout = 0) => _WebGet(url, typeMask, timeout);

    static WebGetStats _WebGet(string url, WebGetType typeMask, int timeout, DownloadHandler downloadHandler = null, long rangeStart = 0, long rangeEnd = 0)
    {
        using (var webReq = CreateWebReq(url, downloadHandler)) //注意：同步Get不支持DownloadHandlerScript，否则会阻塞进程
        {
            webReq.timeout = timeout;
            if (rangeStart > 0)
                webReq.SetRequestHeader("Range", rangeEnd > 0 ? $"bytes={rangeStart}-{rangeEnd}" : $"bytes={rangeStart}-");

            var asyncOp = webReq.SendWebRequest();
            while (!asyncOp.isDone) { }
            return new WebGetStats(webReq, typeMask);
        }
    }

    static UnityWebRequest CreateWebReq(string url, DownloadHandler downloadHandler = null) => downloadHandler != null ? new UnityWebRequest(url, "GET", downloadHandler, null) { disposeDownloadHandlerOnDispose = false } : UnityWebRequest.Get(url);

    enum TaskResult
    {
        None,
        RanToCompletion,
        Canceled,
        Faulted
    }

    [Flags]
    enum WebGetType
    {
        None = 0,

        Data = 0x0001,
        Text = 0x0002,
    }

    readonly struct WebGetStats
    {
        const string _AbortErr = "Request aborted";

        public readonly long downloadedBytes;
        public readonly float downloadProgress;
        public readonly long responseCode;
        public readonly Result result;
        public readonly string error;
        public readonly string dataProcessingError;
        public readonly byte[] data;
        public readonly string text;
        public readonly bool isValid;

        public bool isDone => result != Result.InProgress;
        public bool succeeded => result == Result.Success;
        public bool failed => isDone && !succeeded;
        public bool canceled => failed && error == _AbortErr; //注意：canceled包含在failed内

        public TaskResult taskResult => isDone ? (succeeded ? TaskResult.RanToCompletion : (canceled ? TaskResult.Canceled : TaskResult.Faulted)) : TaskResult.None;

        public WebGetStats(UnityWebRequest webReq, WebGetType typeMask = WebGetType.None)
        {
            downloadedBytes = (long)webReq.downloadedBytes;
            downloadProgress = webReq.downloadProgress;
            responseCode = webReq.responseCode;
            result = webReq.result;
            error = result != Result.InProgress ? webReq.error : null;
            dataProcessingError = result == Result.DataProcessingError ? webReq.downloadHandler?.error : null;
            data = result == Result.Success && (typeMask & WebGetType.Data) != 0 ? webReq.downloadHandler?.data : null;
            text = result == Result.Success && (typeMask & WebGetType.Text) != 0 ? webReq.downloadHandler?.text : null;
            isValid = true;

            if (isDone && responseCode >= 400)
                Debug.LogWarning($"[Net] {nameof(webReq.url)} = {webReq.url}, {nameof(responseCode)} = {responseCode}");
            if (failed && error != null)
                Debug.LogError($"[Net] {nameof(webReq.url)} = {webReq.url}, {nameof(error)} = {error}");
            if (failed && dataProcessingError != null)
                Debug.LogError($"[Net] {nameof(webReq.url)} = {webReq.url}, {nameof(dataProcessingError)} = {dataProcessingError}");
        }
    }
}
