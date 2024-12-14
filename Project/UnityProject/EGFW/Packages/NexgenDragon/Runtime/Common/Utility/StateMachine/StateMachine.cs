using System;
using System.Collections.Generic;

namespace NexgenDragon
{
    /// <summary>
    /// State machine.
    /// Not implement ITicker but also has Tick method. Because state machine is split by it owner.
    /// So it must be managed by it's owner.
    /// </summary>
	public class StateMachine : NexgenObject
	{
        public event Action<State, State> OnStateChanged = null;

		protected Dictionary<Type, State> _stateDict = new Dictionary<Type, State>();
        protected Dictionary<string, Type> _nameDict = new Dictionary<string, Type>();
		protected State _currentState;

		//改变接口形式，是为了更好的迭代已有代码，这个接口在lua也可以使用
		//在lua中使用会当返回值用

		public State CurrentState
		{
			get { return _currentState; }
		}

		public virtual bool CurrentStateIs(string name)
		{
			return _currentState != null && _currentState.GetName() == name;
		}

        protected bool _allowReEnter = false;

		protected FSM.StateParamDic allParams = new FSM.StateParamDic(); 

        public FSM.StateParamDic StateParams
        {
            get{ return allParams;}
        }
        
        public virtual object UserData { get; }

        public bool AllowReEnter
        {
            set{ _allowReEnter = value;}
			get{ return _allowReEnter; }
        }

        public virtual State GetState(Type type)
        {
	        _stateDict.TryGetValue(type, out var state);
	        return state;
        }
        
        public virtual State GetState(string name)
        {
	        if (_nameDict.TryGetValue(name, out var stateType))
	        {
		        _stateDict.TryGetValue(stateType, out var state);
		        return state;
	        }
	        return null;
        }

        public virtual void RemoveState(string name)
        {
	        var state = GetState(name);
	        if (state != null)
	        {
		        _stateDict.Remove(state.GetType());
		        _nameDict.Remove(name);
	        }
        }

		public virtual void AddState(State state)
		{
            var type = state.GetType();
            var name = state.GetName();

            if(!_stateDict.ContainsKey(type) && !_nameDict.ContainsKey(name))
			{
                _stateDict[type] = state;
                _nameDict[name] = type;
                state.SetStateMachine(this);
			}
			else
			{
				NLogger.Warn("StateMachine already has the state : " + state);
			}
		}

		public virtual void SetStates(List<State> states)
		{
			_stateDict = new Dictionary<Type, State>(states.Count);
			_nameDict = new Dictionary<string, Type>(states.Count);
			_currentState = null;
			
			foreach (var state in states)
			{
				var type = state.GetType();
				var name = state.GetName();
				
				_stateDict[type] = state;
				_nameDict[name] = type;
				
				state.SetStateMachine(this);
			}
		}

		protected virtual void _AddTransition(State fromState, State toState, ICondition condition)
		{
			var transition = new StateTransition();
			transition.SetTransition(this, condition, toState);
			fromState.AddTransition(transition);
		}

		public virtual void AddTransition(Type stateFrom, Type stateTo, ICondition condition)
		{
			var fromState = GetState(stateFrom);
			if (fromState == null)
			{
				NLogger.Warn("Add transition from " + stateFrom + " to " + stateTo + " failed.");
				return;
			}

			var toState = GetState(stateTo);

			if (toState == null)
			{
				NLogger.Warn("Add transition from " + stateFrom + " to " + stateTo + " failed.");
				return;
			}

			_AddTransition(fromState,toState,condition);
		}
		
		public virtual void AddTransition(string stateFrom, string stateTo, ICondition condition)
		{
			var fromState = GetState(stateFrom);
			if (fromState == null)
			{
				NLogger.Warn("Add transition from " + stateFrom + " to " + stateTo + " failed.");
				return;
			}

			var toState = GetState(stateTo);

			if (toState == null)
			{
				NLogger.Warn("Add transition from " + stateFrom + " to " + stateTo + " failed.");
				return;
			}
			_AddTransition(fromState,toState,condition);
		}

		public void AddTransition<TStateFrom, TStateTo>(ICondition condition) where TStateFrom : State where TStateTo : State
		{
            var stateFromName = typeof(TStateFrom);
            var stateToName = typeof(TStateTo);
            AddTransition(stateFromName, stateToName, condition);
		}

		public virtual void ClearAllStates()
		{
			if(_currentState != null)
			{
				_currentState.Exit();
				_currentState = null;
			}

			foreach(var state in _stateDict.Values)
			{
				state.Release();
			}

			_stateDict.Clear();
            _nameDict.Clear();
        }

        public virtual void ChangeState(string name)
        {
            Type type;
            if (_nameDict.TryGetValue(name, out type))
            {
                ChangeState(type);
            }
        }

        public virtual void ChangeState(Type type)
        {
            State oldState = null;
            State newState = null;

            if (_currentState != null)
            {
                if (_currentState.GetType() == type)
                {
                    if (_allowReEnter)
                    {
                        _currentState.ReEnter();
                    }
                    return;
                }

                _currentState.Exit();

                oldState = _currentState;
                _currentState = null;
            }

            if (_stateDict.ContainsKey(type))
            {
                _currentState = _stateDict[type];
                _currentState.Enter();

                newState = _currentState;
            }
            else
            {
                NLogger.Error("state {0} not exist", type);
            }

            if (OnStateChanged != null)
            {
                OnStateChanged.Invoke(oldState, newState);
            }
        }

        public void Tick(float delta)
		{
			if(_currentState != null && !_currentState.TrySwitch())
			{
				_currentState.Tick(delta);
			}
		}

		public override void Release ()
		{
			ClearAllStates();
			base.Release ();
		}

        public void TriggerSwtich()
        {
            if (_currentState != null)
            {
                _currentState.TrySwitch();
            }
        }
	}
}