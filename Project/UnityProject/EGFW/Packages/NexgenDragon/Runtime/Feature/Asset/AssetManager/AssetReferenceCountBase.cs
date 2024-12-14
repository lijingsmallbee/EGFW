using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AssetReferenceCountBase 
{
    protected int _referenceCount = 0;
    protected float _zeroTime = float.MaxValue;
    public virtual void Increase()
    {
        _referenceCount++;
    }

    public void ClearReference()
    {
        _referenceCount = 0;
    }

    public virtual bool Decrease()
    {
        _referenceCount--;
        if (_referenceCount <= 0)
        {
            _referenceCount = 0;
            _zeroTime = Time.time;
            return true;
        }
        return false;
    }

    public int ReferenceCount
    {
        get{ return _referenceCount;}
    }

    public abstract void Release ();
}
