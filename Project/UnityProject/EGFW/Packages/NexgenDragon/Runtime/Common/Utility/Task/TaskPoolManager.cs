using System.Collections.Generic;

namespace NexgenDragon
{
    public class TaskPoolManager : Singleton<TaskPoolManager>, IManager
    {
        private readonly Dictionary<string, TaskPool> _pools = new Dictionary<string, TaskPool> ();
        
        // 常用pool
        private const string DEFAULT_KEY = "DEFAULT";
        private const string SERVICE_KEY = "SERVICE"; 
        private const string LOGGER_KEY = "LOGGER";
        private TaskPool _default;
        private TaskPool _service;
        private TaskPool _logger;

        public TaskPool Default => _default ??= GetPool(DEFAULT_KEY);
        public TaskPool Service => _service ??= GetPool(SERVICE_KEY);
        public TaskPool Logger => _logger ??= GetPool(LOGGER_KEY);

        // called on game start
        public void Initialize(NexgenObject configParam)
        {
            _default = GetPool(DEFAULT_KEY);
            _service = GetPool(SERVICE_KEY);
            _logger = GetPool(LOGGER_KEY);
        }

        // called on game exit
        public override void Release()
        {
            Reset ();
            
            foreach (var pair in _pools)
            {
                GameFacade.Instance.RemoveTicker (pair.Value);
            }
            
            _pools.Clear();
            _default = null;
            _service = null;
            _logger = null;
        }
        
        // called on game restart soft
        public void Reset()
        {
            foreach (var pair in _pools)
            {
                pair.Value.Reset();
            }
        }

        /// <summary>
        /// 暂不对外开放 外部直接使用创建好的几个pool
        /// 如果有需求单独创建时再根据情况修改
        /// </summary>
        private TaskPool GetPool(string usage)
        {
            TaskPool pool;
            _pools.TryGetValue (usage, out pool);
            if (pool == null)
            {
                pool = new TaskPool (usage);
                GameFacade.Instance.AddTicker (pool);
                _pools.Add (usage, pool);
            }
            return pool;
        }
        
        /*
        public void RemovePool(string usage)
        {
            _pools.TryGetValue (usage, out var pool);
            if (pool != null)
            {
                GameFacade.Instance.RemoveTicker (pool);
                _pools.Remove(usage);
            }
        }
        */
    }
}