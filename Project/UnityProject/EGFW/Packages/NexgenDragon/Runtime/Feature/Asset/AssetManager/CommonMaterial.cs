using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
    public class CommonMaterialCache:Singleton<CommonMaterialCache>
    {
//        private int maxCount = 10;
        private Dictionary<string,Material> cachePrefabIndex = new Dictionary<string, Material>();
        private Queue<AssetHandle> cachedPrefabs = new Queue<AssetHandle>();
        public Material GetMaterial(string materialPath)
        {
            Material findRet = null;
            if(cachePrefabIndex.TryGetValue(materialPath,out findRet))
            {
                return findRet;
            }
            else
            {
                AssetHandle handle = AssetManager.Instance.LoadAsset(materialPath);
                if (handle != null && handle.asset)
                {
                    //添加新的资源
                    cachedPrefabs.Enqueue(handle);
                    Material mat = handle.asset as Material;
                    cachePrefabIndex.Add(materialPath, mat);
                    return mat;
                }
                else
                {
                    return null;
                }
            }
        }
        
        public void Clear()
        {
            cachedPrefabs.Clear();
            cachePrefabIndex.Clear();
        }
    }
}
