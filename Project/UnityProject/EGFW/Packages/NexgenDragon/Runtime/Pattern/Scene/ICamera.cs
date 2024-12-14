using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
	public interface ICamera : IObject
	{
		Camera UnityCamera { get; }
		List<Camera> UnityCameras { get; }
		Plane GetBasePlane();
		void LookAt(Vector3 position);
		Vector3 GetLookAtPosition();
		int Lod { get; }
	}
}
