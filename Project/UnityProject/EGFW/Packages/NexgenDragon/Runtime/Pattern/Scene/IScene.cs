namespace NexgenDragon
{
	/// <summary>
	/// Scene interface
	/// 1.Manage all scene objects in this scene
	/// 2.Manage all camera in this scene
    /// 3.Implement the specific scene logic
	/// </summary>
	public interface IScene : ITicker
	{
		ICamera MainCamera { get; }
        bool IsReady { get; }
        void EnterScene(object para = null);
        void ExitScene(object para = null);
		void SetVisible(bool visible);
		void SetRootVisible(bool visible);
		void Reset();
		string GetUniqueName();
		// for lua get camera add new function
		UnityEngine.Camera GetSceneCamera();
		
		bool Visible { get; }
		void Set3DListeners();

	}
}

