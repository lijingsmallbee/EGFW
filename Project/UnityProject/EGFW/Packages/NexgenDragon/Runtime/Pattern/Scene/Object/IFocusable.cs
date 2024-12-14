using System;
using UnityEngine;

namespace NexgenDragon
{
	public interface IFocusable
	{
        Vector3 ObjInScenePosition { get; }
        GameObject ObjInUI { get; }
        //添加回调 判断是否定位成功
        void SetIsFocusDone(bool ret);
        
        bool IsNeedFocus { get; }
	}
}
