using UnityEngine;
using NexgenDragon;

public class GameObjectHandle
{
    private readonly GameObjectCreateHelper _helper = GameObjectCreateHelper.Create();
    
    private GameObjectRequestCallback _action;
    private string _prefabName;
    private object _userData;
    private bool _requesting = false;

    public string PrefabName => _prefabName;

    public void Create(string prefabPath, Transform parent, GameObjectRequestCallback action, object userData = null, int priority = 0)
    {
        var samePath = _prefabName == prefabPath;
        _action = action;
        _prefabName = prefabPath;
        _userData = userData;
        if (!samePath)
        {
            _helper.CreateGameObject(_prefabName, OnLoad, priority, true);
            _requesting = true;
        }
    }
    
    public void Create(string prefabPath, Transform parent)
    {
        var samePath = _prefabName == prefabPath;
        _action = null;
        _prefabName = prefabPath;
        _userData = null;
        if (Idle && !samePath)
        {
            var go = GameObjectCreateHelper.CreateTemplateGameObject(_prefabName, parent);
            OnLoad(go);    
        }
    }

    public void Delete()
    {
        if (Asset != null)
        {
            GameObjectCreateHelper.DestoryGameObject(Asset);
            Asset = null;
        }

        _helper.CancelAllCreate();
        _requesting = false;
        _prefabName = null;
        _action = null;
    }

    public bool Idle => Asset == null && _requesting == false;

    public GameObject Asset { get; private set; }

    private void OnLoad(GameObject go)
    {
        Asset = go;
        _requesting = false;
        _action?.Invoke(go,_userData);
    }
}
