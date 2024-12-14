using UnityEngine;

namespace NexgenDragon
{
	public abstract class VisualEffect : MonoBehaviour, IAttachable
	{
		public float duration = -1; // 
		public float delay = 0;
		public float simulateStartTime = 0f;
		public float speed = 1f;
		public Vector3 offset = Vector3.zero;
		public Vector3 rotation = Vector3.zero;
		public Vector3 scale = Vector3.one;
		public bool autoPlay;
		public bool autoDisable;
		public bool autoDelete;
        public bool updateLoopingOnAwake;
	    public bool loopingOnAwake;

	    public abstract void Play();
	    public abstract void Stop();
	    public abstract void Simulate(float dt);
	    public abstract void SetLooping(bool loop);
	    
	    public abstract bool IsPlaying();
	    
	    public void Attach(Transform parent)
	    {
		    var transform1 = transform;
		    transform1.SetParent(parent, false);
	        transform1.localPosition = offset;
	        transform1.localRotation = Quaternion.Euler(rotation);
	        transform1.localScale = scale;
	    }
	}
}