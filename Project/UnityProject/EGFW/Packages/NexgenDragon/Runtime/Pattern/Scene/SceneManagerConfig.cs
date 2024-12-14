using System.Collections.Generic;

namespace NexgenDragon
{
	public sealed class SceneManagerConfig : NexgenObject
	{
        public Dictionary<string, IScene> Scenes = new Dictionary<string, IScene>();
	}
}
