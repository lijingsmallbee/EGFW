using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;
using System;
using UnityEditor;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public class GameObjectCreateHelper
{
    static HashSet<WeakReference<GameObjectCreateHelper>> _allHelpers = new HashSet<WeakReference<GameObjectCreateHelper>>();
    public static Action<string> onCreate;
    class GlobalGameObjectManager : ISecondTicker
    {
        private GlobalGameObjectManager()
        {
        }

        private static GlobalGameObjectManager _instance = null;

        public static GlobalGameObjectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalGameObjectManager();
                    _instance.Init();
                }

                return _instance;
            }
        }

        private void Init()
        {
            GameFacade.Instance.AddTicker(this);

            AssetManagerEvent.AddBeforeUnloadUnusedListener(BeforeUnloadUnused);
        }

        public List<AssetCacheRefInfo> DumpAllHandlers()
        {
            List<AssetCacheRefInfo> infos = new List<AssetCacheRefInfo>(128);
            foreach (var objs2Handle in _Objs2Handles)
            {
                AssetCacheRefInfo info = new AssetCacheRefInfo();
                info.infoType = "id2handler";
                info.assetPath = objs2Handle.Value.cacheIndex;
                infos.Add(info);
            }

            foreach (var handle2Obj in _allCreatedObjects)
            {
                AssetCacheRefInfo info = new AssetCacheRefInfo();
                info.infoType = "handler2obj";
                info.assetPath = handle2Obj.Key.cacheIndex;
                info.reasons = new List<string>(2);
                info.reasons.Add(handle2Obj.Value.name);
                infos.Add(info);
            }

            return infos;
        }

        private Dictionary<int, AssetHandle> _Objs2Handles = new Dictionary<int, AssetHandle>();

        private Dictionary<AssetHandle, GameObject> _allCreatedObjects = new Dictionary<AssetHandle, GameObject>();
 
        public void AddCreateGameObject(AssetHandle handle, GameObject created)
        {
            if (_allCreatedObjects.ContainsKey(handle))
            {
                NLogger.Error("add same game object {0}", created.name);
            }
            else
            {
                _allCreatedObjects.Add(handle, created);
                AddCreateGameobject2AssetHandleMap(created.GetInstanceID(), handle);
                onCreate?.Invoke(handle.asset.name);
#if UNITY_DEBUG
             //   NLogger.Log("pool create game object {0}",created);
#endif
            }
        }

        private void AddCreateGameobject2AssetHandleMap(int instanceId, AssetHandle handle)
        {
            if (_Objs2Handles.ContainsKey(instanceId))
            {
                NLogger.Error("add same game object {0}", instanceId);
            }
            else
            {
                _Objs2Handles.Add(instanceId, handle);
            }
        }

        public AssetHandle GetAssetHandle(int instanceId)
        {
            AssetHandle handle = null;
            _Objs2Handles.TryGetValue(instanceId, out handle);
            return handle;
        }

        public bool ContainsKey(AssetHandle handle)
        {
            return _allCreatedObjects.ContainsKey(handle);
        }


        private List<AssetHandle> deleteList = new List<AssetHandle>(32);
        private List<int> deleteInstanceIds = new List<int>(32);
        private string vinayGaoClassType = null;
        public string VinayGaoGetClassType
        {
            get
            {
                if (vinayGaoClassType == null)
                {
                    vinayGaoClassType ="VinayGao:" + GetType().Name;
                }

                return vinayGaoClassType;
            }
        }
        public void Tick(float delta)
        {
            Perf.BeginSample("VinayGao:GameObjectCreateHelper.Tick");
            //check null game object
            deleteList.Clear();
            deleteInstanceIds.Clear();
            var checkIt = _allCreatedObjects.GetEnumerator();
            while (checkIt.MoveNext())
            {
                if (checkIt.Current.Value == null)
                {
                    deleteList.Add(checkIt.Current.Key);
                    deleteInstanceIds.Add(checkIt.Current.Value.GetInstanceID());
                }
            }

            checkIt.Dispose();

            var deleteIt = deleteList.GetEnumerator();
            while (deleteIt.MoveNext())
            {
                _allCreatedObjects.Remove(deleteIt.Current);
                AssetManager.Instance.UnloadAsset(deleteIt.Current);
            }

            deleteIt.Dispose();
            foreach (var delete in deleteInstanceIds)
            {
                _Objs2Handles.Remove(delete);
            }
            Perf.EndSample();
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            AssetHandle handle = null;
            _Objs2Handles.TryGetValue(gameObject.GetInstanceID(), out handle);
            if (handle != null)
            {
                _Objs2Handles.Remove(gameObject.GetInstanceID());
                if (_allCreatedObjects.ContainsKey(handle))
                {
                    _allCreatedObjects.Remove(handle);
                    AssetManager.Instance.UnloadAsset(handle);
                }
            }

        }

        public void Clear()
        {
            #if UNITY_DEBUG
            
            foreach (var pair in _allCreatedObjects)
            {
                if (pair.Value)
                {
                    var name = pair.Value.name;
                    if (name.StartsWith("child_") || name.StartsWith("ui_"))
                    {
                        continue;
                    }
                //    NLogger.Error("game object {0} not be destroy when reset",pair.Value.name);
                }
            }
            
            #endif
            _allCreatedObjects.Clear();
            _Objs2Handles.Clear();
            deleteList.Clear();
            deleteInstanceIds.Clear();
        }

        private void BeforeUnloadUnused()
        {
            Tick(0f);
        }
    }

    //防止外部创建，只能内部创建并管理
    private GameObjectCreateHelper()
    {
        
    }

    public static List<AssetCacheRefInfo> DumpAllHandlers()
    {
        return GlobalGameObjectManager.Instance.DumpAllHandlers();
    }

    public static GameObjectCreateHelper Create()
    {
        var helper = new GameObjectCreateHelper();
        _allHelpers.Add(new WeakReference<GameObjectCreateHelper>(helper));
        return helper;
    }

    public static void Clear()
    {
        GlobalGameObjectManager.Instance.Clear();
        foreach (var weak in _allHelpers)
        {
            if (weak.TryGetTarget(out var helper))
            {
                helper.CancelAllCreate();
            }
        }
    }

    private HashSet<AssetHandle> _allLoadingHandler = new HashSet<AssetHandle>();

    public void CreateGameObject(string prefabPath, Action<GameObject> callback, int priority = 999, bool asap = true)
    {
        AssetHandle handle = AssetManager.Instance.LoadAsset(prefabPath, (success, assetHandle) =>
        {
            //创建完毕，假如在加载列表中，就移除
            if (_allLoadingHandler.Contains(assetHandle))
            {
                _allLoadingHandler.Remove(assetHandle);
            }

            if (success && assetHandle.asset)
            {
                GameObject newObject = GameObject.Instantiate(assetHandle.asset) as GameObject;
                if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                {
                    NLogger.Assert(newObject != null, $"assetHandle.asset.Instantiate failed:{prefabPath}" + assetHandle.asset);
                }

                GlobalGameObjectManager.Instance.AddCreateGameObject(assetHandle, newObject);
                if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                {
                    //			AddPrefabLinker(newObject, assetHandle.asset);                
                }

                if (callback != null)
                {
                    callback(newObject);
                }
            }
            else
            {
                if (callback != null)
                {
                    callback(null);
                }
            }
        }, false, priority, asap);

        //假如是加载完毕的资源，会立即调用callback，加入到已经创建的dictionary中，这种情况下不需要再加入加载列表中了
        if (!GlobalGameObjectManager.Instance.ContainsKey(handle))
        {
            _allLoadingHandler.Add(handle);
        }
    }

    public void CreateGameObject(string prefabPath, Transform parent, Action<GameObject> callback, int priority = 999, bool asap = true)
    {
        AssetHandle handle = AssetManager.Instance.LoadAsset(prefabPath, (success, assetHandle) =>
        {
            //创建完毕，假如在加载列表中，就移除
            if (_allLoadingHandler.Contains(assetHandle))
            {
                _allLoadingHandler.Remove(assetHandle);
            }

            if (success && assetHandle.asset)
            {
                var source = assetHandle.asset as GameObject;
                GameObject newObject = GameObject.Instantiate(assetHandle.asset) as GameObject;
                if (parent != null)
                {
                    SetLayer(newObject, parent.gameObject.layer);
                    newObject.transform.SetParent(parent);
                    newObject.transform.localPosition = source.transform.localPosition;
                    newObject.transform.localScale = source.transform.localScale;
                }

                GlobalGameObjectManager.Instance.AddCreateGameObject(assetHandle, newObject);


                if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                {
                    AddPrefabLinker(newObject, assetHandle.asset);
                }

                if (callback != null)
                {
                    callback(newObject);
                }
            }
            else
            {
                if (callback != null)
                {
                    callback(null);
                }
            }
        }, false, priority, asap);

        //假如是加载完毕的资源，会立即调用callback，加入到已经创建的dictionary中，这种情况下不需要再加入加载列表中了
        if (!GlobalGameObjectManager.Instance.ContainsKey(handle))
        {
            _allLoadingHandler.Add(handle);
        }
    }

    public static GameObject CreateUIGameObject(string prefabPath)
    {
        var handle = AssetManager.Instance.LoadAsset(prefabPath);
        var prefab = handle.asset as GameObject;
        if (!prefab)
        {
            return null;
        }

        var go = Object.Instantiate(prefab);
        GlobalGameObjectManager.Instance.AddCreateGameObject(handle, go);
        if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
        {
            AddPrefabLinker(go, handle.asset);
        }

        return go;
    }

    public static AssetHandle CreateUIGameObject(string prefabPath, Action<GameObject> callback)
    {
        return AssetManager.Instance.LoadAsset(prefabPath, (success, assetHandle) =>
        {
            GameObject go = null;
            if (success && assetHandle.asset)
            {
                go = Object.Instantiate((GameObject)assetHandle.asset);
                GlobalGameObjectManager.Instance.AddCreateGameObject(assetHandle, go);
                if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
                    AddPrefabLinker(go, assetHandle.asset);
            }
            callback?.Invoke(go);
        });
    }

    public static GameObject CreateTemplateGameObject(string prefabPath, Transform parent = null)
    {
        if (AssetManager.Instance.EnvironmentVariable.UNITY_DEBUG())
        {
            var ischild = (prefabPath.Contains("template_") || prefabPath.Contains("Template_"));
            if (!ischild)
            {
#if UNITY_EDITOR
                NLogger.Warn("prefab {0} is not a UI Child,name not contain child_ or Child_", prefabPath);
#endif
            }
        }

        AssetHandle handle = AssetManager.Instance.LoadAsset(prefabPath);
        if (handle.asset != null)
        {
            var source = handle.asset as GameObject;
            GameObject newObject = GameObject.Instantiate(handle.asset) as GameObject;
            if (null != parent)
            {
                SetLayer(newObject, parent.gameObject.layer);
                newObject.transform.SetParent(parent);
                newObject.transform.localPosition = source.transform.localPosition;
                newObject.transform.localScale = source.transform.localScale;
            }

            GlobalGameObjectManager.Instance.AddCreateGameObject(handle, newObject);
            if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
            {
                AddPrefabLinker(newObject, handle.asset);
            }

            return newObject;
        }

        return null;
    }

    public static GameObject Instantiate(GameObject template)
    {
//	    if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
//	    {
//	        var t = UnityEditor.PrefabUtility.GetPrefabType(template);
//	        //如果不是一个game object的instance，会报错
//	        if(t == UnityEditor.PrefabType.ModelPrefab || t == UnityEditor.PrefabType.Prefab )
//	        {
//	            NLogger.Error("this function is not allowed parameter is a prefab");
//	        }
//	    }
//		
        return GameObject.Instantiate(template);
    }

    public static GameObject CreateUIChild(string prefabPath, Transform parent = null)
    {
//        #if UNITY_DEBUG
//        var ischild = (prefabPath.Contains("child_") || prefabPath.Contains("Child_"));
//        if(!ischild)
//        {
//            NLogger.Error("prefab {0} is not a UI Child,name not contain child_ or Child_",prefabPath);
//        }
//        #endif
        AssetHandle handle = AssetManager.Instance.LoadAsset(prefabPath);
        if (handle.asset != null)
        {
            var source = handle.asset as GameObject;
            GameObject newObject = GameObject.Instantiate(handle.asset) as GameObject;
            if (null != parent)
            {
                SetLayer(newObject, parent.gameObject.layer);
                newObject.transform.SetParent(parent, false);
                newObject.transform.localPosition = source.transform.localPosition;
                newObject.transform.localScale = source.transform.localScale;
            }

            GlobalGameObjectManager.Instance.AddCreateGameObject(handle, newObject);

            if (AssetManager.Instance.EnvironmentVariable.UNITY_EDITOR())
            {
                AddPrefabLinker(newObject, handle.asset);
            }

            return newObject;
        }

        return null;
    }

    public static void DestoryGameObject(GameObject deleteObj)
    {
        if (deleteObj)
        {
            GlobalGameObjectManager.Instance.RemoveGameObject(deleteObj);
            GameObject.Destroy(deleteObj);
        }
    }
    
    public static void DestroyGameObject(GameObject deleteObj)
    {
        DestoryGameObject(deleteObj);
    }

    public static void CheckGameObjectValidNow()
    {
        GlobalGameObjectManager.Instance.Tick(0f);
    }

    public void CancelAllCreate()
    {
        //正在加载中还没有创建出的资源，unload去掉callback同时减少引用计数
        var handleIt = _allLoadingHandler.GetEnumerator();
        while (handleIt.MoveNext())
        {
            AssetManager.Instance.UnloadAsset(handleIt.Current);
        }

        _allLoadingHandler.Clear();
        handleIt.Dispose();
    }

    public static void SetLayer(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;

        Transform t = gameObject.transform;

        for (int i = 0, imax = t.childCount; i < imax; ++i)
        {
            Transform child = t.GetChild(i);
            SetLayer(child.gameObject, layer);
        }
    }

    internal static void AddPrefabLinker(GameObject go, UnityEngine.Object prefab)
    {
        var pl = go.AddComponent<PrefabLinker>();
        pl.prefab = prefab as GameObject;
    }
}
