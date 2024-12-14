using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;
public class LoadExistBundleFail : IEvent
{
    private string _bundleName;
    private string _assetName;
    public const string NAME = "LOAD_EXIST_BUNDLE_FAIL";
    public LoadExistBundleFail(string bundleName,string assetName)
    {
        _bundleName = bundleName;
        _assetName = assetName;
    }
    public string GetEventType()
    {
        return NAME;
    }

    public string ErrorBundleName
    {
        get { return _bundleName; }
    }
    
    public string ErrorAssetName
    {
        get { return _assetName; }
    }
}
