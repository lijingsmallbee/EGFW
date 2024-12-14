using System.Collections.Generic;
using UnityEngine;

namespace NexgenDragon
{
    public class GameObjectPoolManager : Singleton<GameObjectPoolManager>, IManager
    {
        private GameObject _rootNode;
        private readonly Dictionary<string, GameObjectPool> _pools = new Dictionary<string, GameObjectPool>();

        public void Initialize(NexgenObject configParam)
        {
            _rootNode = new GameObject("GameObjectPoolManager");
        }

        public GameObjectPool GetPool(string usage)
        {
            GameObjectPool pool;
            _pools.TryGetValue(usage, out pool);
            if (pool == null)
            {
                pool = new GameObjectPool(usage);
                pool.Initialize(_rootNode.transform);
                GameFacade.Instance.AddTicker(pool);
                _pools.Add(usage, pool);
            }

            return pool;
        }

        public void Reset()
        {
            foreach (var pair in _pools)
            {
                pair.Value.ClearCache();
            }
        }

        public override void Release()
        {
            Reset();

            foreach (var pair in _pools)
            {
                GameFacade.Instance.RemoveTicker(pair.Value);
            }

            if (_rootNode)
            {
                Object.Destroy(_rootNode);
                _rootNode = null;
            }
        }
    }
}