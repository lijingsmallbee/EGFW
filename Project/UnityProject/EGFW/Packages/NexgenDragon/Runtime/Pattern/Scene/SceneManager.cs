using System.Collections.Generic;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	public class SceneManager : Singleton<SceneManager>, IManager, ITicker
	{
		private readonly Dictionary<string, IScene> _sceneDict = new Dictionary<string, IScene>();

		public IScene CurrentScene { get; private set; }

		public void Initialize(NexgenObject configParam)
		{
			if (configParam is SceneManagerConfig config)
			{
				foreach (var pair in config.Scenes)
				{
					_sceneDict.Add(pair.Key, pair.Value);
				}
			}

			GameFacade.Instance.AddTicker(this);
		}

		public void AddScene(string name, IScene scene)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (!_sceneDict.ContainsKey(name))
				{
					_sceneDict[name] = scene;
				}
				else
				{
					RemoveScene(name);
					_sceneDict[name] = scene;
				}
			}
		}

		public void RemoveScene(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (_sceneDict.ContainsKey(name))
				{
					_sceneDict.Remove(name);
				}
			}
		}

		public void Reset()
		{
			foreach (var pair in _sceneDict)
			{
				pair.Value.Reset();
			}

			CurrentScene = null;
		}

		public override void Release()
		{
			GameFacade.Instance.RemoveTicker(this);
			_sceneDict.Clear();

			CurrentScene = null;
		}

		public void EnterScene(string scene, object para = null)
		{
			if (!_sceneDict.TryGetValue(scene, out var targetScene))
			{
				return;
			}

			CurrentScene = targetScene;
			CurrentScene.EnterScene(para);
			EventManager.Instance.TriggerEvent(new SceneEnterEvent(scene));
		}

		public void ExitScene(string scene, object para = null)
		{
			if (CurrentScene.GetUniqueName() != scene)
			{
				return;
			}

			CurrentScene.ExitScene(para);
			EventManager.Instance.TriggerEvent(new SceneExitEvent(scene));
		}

		public IScene GetScene(string scene)
		{
			_sceneDict.TryGetValue(scene, out var targetScene);
			return targetScene;
		}

		public void Tick(float delta)
		{
			Perf.BeginSample("VinayGao:SceneManager.Tick");
			CurrentScene?.Tick(delta);
			Perf.EndSample();
		}
	}
}