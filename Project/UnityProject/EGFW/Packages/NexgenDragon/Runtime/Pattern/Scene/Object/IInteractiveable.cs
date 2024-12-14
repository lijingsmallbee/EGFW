using System;
using UnityEngine;

namespace NexgenDragon
{
	public interface IInteractiveable
	{
        void SetReceiver(IInteractiveableReceiver receiver);
	}

    public interface IInteractiveableReceiver
    {
		void OnClick(GameObject go);
		void OnDrag(GameObject go, Vector3 delta);
        void OnDragEnd(GameObject go);
        void OnHover(GameObject go);
	    void OnMouseDown(GameObject go);
        void RegisterObject(GameObject go);
    }
}
