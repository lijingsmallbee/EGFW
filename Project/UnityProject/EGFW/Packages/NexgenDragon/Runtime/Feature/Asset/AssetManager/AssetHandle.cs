using UnityEngine;
using System;
using System.Collections;

namespace NexgenDragon
{
    public class AssetHandle
    {
        private AssetCache _cache = null;
        //内部管理的唯一标示
		public string cacheIndex;
        //返还给外部使用的标示，与调用load asset的时候传入的path永远保持一致
        public string path;
        
        internal void SetAssetCache(AssetCache cache)
        {
            _cache = cache;
        }
		public UnityEngine.Object asset 
        {
            get 
            {
                if(_cache != null)
                {
                    return _cache.Asset;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                NLogger.Error("this function is decre");
            }
        }

        public bool IsValid
        {
            get { return _cache != null && _cache.Asset; }
        }
        public Action<bool, AssetHandle> callback;
    }
}
