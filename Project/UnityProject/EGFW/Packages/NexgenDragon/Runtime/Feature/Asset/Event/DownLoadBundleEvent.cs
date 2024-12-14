using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;

public class DownLoadBundleEvent : IEvent
{
    private string _bundleName;
    public const string NAME = "DOWNLOAD__BUNDLE_EVENT";

    public DownLoadBundleEvent(string bundleName)
    {
        _bundleName = bundleName;
    }

    public string GetEventType()
    {
        return NAME;
    }

    public string ErrorBundleName
    {
        get { return _bundleName; }
    }
}
