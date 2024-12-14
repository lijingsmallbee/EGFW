using System;
using System.Collections;
using System.Collections.Generic;
using NexgenDragon;
using UnityEngine;

public class AssetManagerHelper : MonoBehaviour {

    public bool NeedClear
    {
        get;set;
    }

    private int _againStartFrame = -1;

    private void LateUpdate()
    {
        if(NeedClear)
        {
            NeedClear = false;
            _againStartFrame = Time.frameCount;
            AssetManager.Instance.UnloadUnusedImmediate();
        }

        if (_againStartFrame != -1)
        {
            if (Time.frameCount - _againStartFrame > 5)
            {
                _againStartFrame = -1;
                AssetManager.Instance.UnloadUnusedImmediate();
            }
        }
    }
}
