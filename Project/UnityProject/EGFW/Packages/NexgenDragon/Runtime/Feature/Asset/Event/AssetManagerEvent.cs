using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class AssetManagerEvent
{
    private static event Action OnBeforeClearEvent;
    private static event Action OnAfterClearEvent;
    private static event Action OnBeforeUnloadUnusedEvent;
	// Use this for initialization
	
    public static void AddBeforeClearListener(Action listener)
    {
        OnBeforeClearEvent -= listener;
        OnBeforeClearEvent += listener;
    }

    public static void AddAfterClearListener(Action listener)
    {
        OnAfterClearEvent -= listener;
        OnAfterClearEvent += listener;
    }

    public static void AddBeforeUnloadUnusedListener(Action listener)
    {
        OnBeforeUnloadUnusedEvent -= listener;
        OnBeforeUnloadUnusedEvent += listener;
    }

    public static void InvokeBeforeClear()
    {
        if(OnBeforeClearEvent != null)
        {
            OnBeforeClearEvent();
        }
    }

    public static void InvokeAfterClear()
    {
        if(OnAfterClearEvent != null)
        {
            OnAfterClearEvent();
        }
    }

    public static void InovkeBeforeUnloadUnused()
    {
        if(OnBeforeUnloadUnusedEvent!=null)
        {
            OnBeforeUnloadUnusedEvent();
        }
    }
}
