using UnityEngine;

namespace NexgenDragon
{
	public interface IAttachable
	{
		void Attach (Transform parent);
		GameObject gameObject { get; }
	}
}