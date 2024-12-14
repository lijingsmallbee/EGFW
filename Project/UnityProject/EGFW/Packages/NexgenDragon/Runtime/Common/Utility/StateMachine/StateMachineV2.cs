using System;
using System.Collections.Generic;
using FSM;

namespace NexgenDragon
{
    /// <summary>
    /// State machine.
    /// Not implement ITicker but also has Tick method. Because state machine is split by it owner.
    /// So it must be managed by it's owner.
    /// </summary>
	public class StateMachineV2 : StateMachine
	{
        public event Action<State, State> OnStateChanged = null;

        //携带这个状态机的用户信息，例如是一个角色的状态机，可以把角色对象放进去，方便各个状态获取
        private object _userData = null;

		protected Dictionary<string, State> _stateDictionary = new Dictionary<string, State>();

		public void SetUserData(object userData)
        {
	        _userData = userData;
        }

        public override object UserData
        {
	        get { return _userData; }
        }

		public override void AddState(State state)
		{
			var name = state.GetName();

            if(!_stateDictionary.ContainsKey(name))
			{
				_stateDictionary[name] = state;
                state.SetStateMachine(this);
			}
			else
            {
	            var old = _stateDictionary[name];
	            old.Release();
	            _stateDictionary[name] = state;
	            state.SetStateMachine(this);
				NLogger.Warn("StateMachine already has the state : " + name);
			}
		}
		
		public override void RemoveState(string name)
		{
			if(_stateDictionary.ContainsKey(name))
			{
				_stateDictionary.Remove(name);
			}
			else
			{
				NLogger.Warn("StateMachine already has the state : " + name);
			}
		}

		public override void SetStates(List<State> states)
		{
			_stateDictionary = new Dictionary<string, State>(states.Count);
			_currentState = null;
			
			foreach (var state in states)
			{
				var name = state.GetName();
				_stateDictionary[name] = state;
				state.SetStateMachine(this);
			}
		}
		
		public override void ClearAllStates()
		{
			if(_currentState != null)
			{
				_currentState.Exit();
				_currentState = null;
			}

			foreach(var state in _stateDictionary.Values)
			{
				state.Release();
			}

			_stateDictionary.Clear();
		}

		public override State GetState(Type type)
		{
			var stateName = type.Name;
			_stateDictionary.TryGetValue(stateName, out var state);
			return state;
		}

		public override State GetState(string name)
		{
			_stateDictionary.TryGetValue(name, out var state);
			return state;
		}

		public override void ChangeState(string name)
        {
	        if (_stateDictionary.TryGetValue(name, out var oldState))
            {
	            State newState = null;

	            if (_currentState != null)
	            {
		            if (_currentState.GetName() == name)
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

	            var stateName = name;
	            _currentState = _stateDictionary[stateName];
	            _currentState.Enter();

	            newState = _currentState;

	            allParams.ResetTrigger();

	            if (OnStateChanged != null)
	            {
		            OnStateChanged.Invoke(oldState, newState);
	            }
            }
	        else
	        {
		        NLogger.Error("state {0} not exist", name);
	        }
        }

        public override void ChangeState(Type type)
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

            var stateName = type.Name;
            if (_stateDictionary.ContainsKey(stateName))
            {
                _currentState = _stateDictionary[stateName];
                _currentState.Enter();

                newState = _currentState;
            }
            else
            {
                NLogger.Error("state {0} not exist", type);
            }
            
            allParams.ResetTrigger();

            if (OnStateChanged != null)
            {
                OnStateChanged.Invoke(oldState, newState);
            }
        }
        
        protected override void _AddTransition(State fromState, State toState, ICondition condition)
        {
	        var transition = new StateTransitionV2();
	        transition.SetTransition(this, condition, toState);
	        fromState.AddTransition(transition);
        }
	}
    
    public class StateTransitionV2:StateTransition
    {
	    public override bool TrySwitch()
	    {
		    if (!_condition.IsSatisfied())
		    {
			    return false;
		    }
            
		    _machine.ChangeState(_targetState.GetName());

		    NLogger.TraceChannel("StateTransition", "Transition: Condition = {0}, State = {1}", 
			    _condition.GetType(), _targetState.GetName());

		    return true;
	    }
    }

}