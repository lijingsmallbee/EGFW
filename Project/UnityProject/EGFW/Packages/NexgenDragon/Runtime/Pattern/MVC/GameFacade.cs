using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace NexgenDragon
{
	/// <summary>
	/// Game facade.
	/// 1.The manager of all managers.
	/// 2.The entrance(facade) of the framework.
	/// 3.The update trigger of all object of this framework.
	/// 4.Inject by outside logic.
	/// </summary>
	public sealed class GameFacade : Singleton<GameFacade>, IManager
	{
		private GameFacadeConfig _config = null;

        private readonly StateMachineV2 _stateMachine = new StateMachineV2();

        public StateMachineV2 GameStateMachine
        {
            get { return _stateMachine; }
        }

        private byte _tickCounter = 0;
        
		//not applicationPause , just logic pause
		public bool GamePause { get; set; }
		
		public bool Initialized { get; private set; }

		public void Initialize (NexgenObject configParam)
		{
			_config = configParam as GameFacadeConfig;

			NLogger.Assert(_config != null,"GameFacade config is null. You must setup config with yourself.");

			// process GameFacadeConfig
		
			// config all sub-manager to system. ( Maybe could change it to specific state )
			// initilize all managers
			List<IManager> managers = _config.Managers;
			List<NexgenObject> configs = _config.ManagerConfigs;
			if(managers != null)
			{
				NLogger.Assert(configs != null && configs.Count == managers.Count, "GameFacase's config must have same number managers and manager configs.");

				for(int i = 0; i < managers.Count; i++)
				{
					AddManager(managers[i],configs[i]);
				}
			}

			// config state machine from config parameter.
			List<State> states = _config.States;

			NLogger.Assert(states != null && states.Count > 0 ,"GameFacade config has no states. You must setup states with yourself.");

			for(int i = 0; i < states.Count; i++)
			{
				_stateMachine.AddState(states[i]);
			}

			_stateMachine.AllowReEnter = true;
			if (_config.StartupStateType != null)
			{
				_stateMachine.ChangeState(_config.StartupStateType);	
			}
			GamePause = false;
			Initialized = true;
		}

		public event Action<bool> onApplicationPause;
		public event Action<bool> onApplicationFocus;
		public event Action onApplicationQuit;

		public void OnApplicationPause(bool pause)
		{
			NLogger.Log ("GameFacade OnApplicationPause: " + pause);

			if (onApplicationPause != null)
			{
				onApplicationPause (pause);
			}
		}
		
		public void OnApplicationFocus(bool focus)
		{
			NLogger.Log ("GameFacade OnApplicationFocus: " + focus);

			if (onApplicationFocus != null)
			{
				onApplicationFocus (focus);
			}
		}
		
		public void OnApplicationQuit()
		{
			NLogger.Log ("GameFacade OnApplicationQuit");

			if (onApplicationQuit != null)
			{
				onApplicationQuit ();
			}
		}

		public override void Release ()
		{
			_tickerList.Clear();
			_gapTickerList.Clear();
			_secondTickerList.Clear();
            _timeScaleIgnoredTickerList.Clear();

			_stateMachine.Release();

			foreach(var manager in _managerDict.Values)
			{
				manager.Release();
			}	

			_managerDict.Clear();

			_config.Release();
			_config = null;
			onApplicationPause = null;
		}

		public void Reset()
		{
			foreach(var manager in _managerDict.Values)
			{
				try
				{
					manager.Reset();
				}
				catch (Exception e)
				{
					NLogger.Error(manager + " Reset Error: " + e);
				}				
			}

			// Change to startup state
			_stateMachine.ChangeState(_config.StartupStateType);

			_tickCounter = 0;
			onApplicationPause = null;
			onApplicationFocus = null;
			onApplicationQuit = null;
		}

		#region About managers

		private readonly Dictionary<Type,IManager> _managerDict = new Dictionary<Type, IManager>();

		public T GetManager<T>() where T : class, IManager 
		{
			IManager manager = null;

			_managerDict.TryGetValue(typeof(T),out manager);

			return manager as T;
		}

		public void AddManager(IManager manager, NexgenObject config = null)
		{
			_managerDict.Add(manager.GetType(),manager);
			manager.Initialize(config);
		}

		public bool RemoveManager(IManager manager)
		{
			manager.Release();
			//这个地方虽然写错了，但是不改了，避免出问题，在下边加个能用的
            return _managerDict.Remove(typeof(IManager));
        }

        public bool RemoveManagerByType(Type managerT)
        {
			if(_managerDict.TryGetValue(managerT, out var manager))
			{
                manager.Release();
            }          
            return _managerDict.Remove(managerT);
        }

        #endregion

        #region About Ticker

        [NonSerialized] public double frameTime;
        private Stopwatch _stopwatch = new Stopwatch();
        
        private readonly TickerList _tickerList = new TickerList(false);
		private readonly TickerList _gapTickerList = new TickerList(false);
		private readonly TickerList _secondTickerList = new TickerList(false);
        private readonly TickerList _timeScaleIgnoredTickerList = new TickerList(true);
        private readonly TickerList _lateUpdateTickerList = new TickerList(false);

		public void AddTicker(ITicker ticker)
		{
			TickerList list = GetListFromTicker(ticker);
			list.Add(ticker);
		}

		public void RemoveTicker(ITicker ticker)
		{
			TickerList list = GetListFromTicker(ticker);
			list.Remove(ticker);
		}

        public void Tick(float delta)
        {
	        _stopwatch.Reset();
	        _stopwatch.Start();
	        
            if (GamePause)
            {
	            _stopwatch.Stop();
	            return;
            }

            //Tick all top-level ticker
            _stateMachine.Tick(delta);
            _tickerList.Tick();
            _timeScaleIgnoredTickerList.Tick();
            if (_tickCounter >= 2)
            {
	            _tickCounter = 0;
                _gapTickerList.Tick();
            }
            ++_tickCounter;
            if (TimeUtils.Time - _secondTickerList.LastTickTime > 1.0f)
            {
                _secondTickerList.Tick();
            }
            _stopwatch.Stop();
        }

        public void LateUpdateTick(float delta)
        {
	        _stopwatch.Start();
	        _lateUpdateTickerList.Tick();
	        _stopwatch.Stop();
	        frameTime = _stopwatch.Elapsed.TotalMilliseconds;
        }

        private TickerList GetListFromTicker(ITicker ticker)
        {
            if(ticker is ISecondTicker)
			{
				return _secondTickerList;
			}

            if(ticker is IGapTicker)
            {
                return _gapTickerList;
            }

            if (ticker is ITimeScaleIgnoredTicker)
            {
                return _timeScaleIgnoredTickerList;
            }

            if (ticker is ILateUpdateTicker)
            {
	            return _lateUpdateTickerList;
            }

            return _tickerList;
        }

        public void DumpTickers()
        {
            _secondTickerList.DumpList();
            _gapTickerList.DumpList();
            _tickerList.DumpList();
            _timeScaleIgnoredTickerList.DumpList();
        }

        private void RemoveLuaTickerFromTickerList(TickerList tickerList)
        {
	        var updateList = tickerList.GetAllUpdateList();
	        foreach (var ticker in updateList)
	        {
		        var typeName = ticker.GetType().Name;
		        if (typeName.StartsWith("Lua"))
		        {
			        tickerList.Remove(ticker);
		        }
	        }
        }

        public void RemoveLuaTickers()
        {
	        RemoveLuaTickerFromTickerList(_secondTickerList);
	        RemoveLuaTickerFromTickerList(_gapTickerList);
	        RemoveLuaTickerFromTickerList(_lateUpdateTickerList);
	        RemoveLuaTickerFromTickerList(_tickerList);
        }

		#endregion
	}
}
