using UnityEngine;
using System.Collections.Generic;

namespace NexgenDragon
{
    public abstract class NexgenBehaviour : MonoBehaviour, IObject
    {
        public abstract void Release();
    }
}