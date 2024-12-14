using System.Collections.Generic;

namespace NexgenDragon
{
	public abstract class State : NexgenObject
	{
		protected StateMachine _stateMachine;
		protected LinkedList<StateTransition> _transitions = new LinkedList<StateTransition>();

		public abstract void Enter();
		public abstract void Tick(float delta);
		public abstract void Exit();
        public virtual void ReEnter (){}
        public virtual string GetName()
        {
            return this.GetType().Name;
        }
        public virtual List<State> GetDependences()
        {
	        return null;
        }

		public void SetStateMachine(StateMachine stateMachine)
		{
			_stateMachine = stateMachine;
		}

		public void AddTransition(StateTransition transition)
		{
			_transitions.AddLast(transition);
		}
		
		public void AddTransition(State toState, ICondition condition)
		{
			var transition = new StateTransition();
			transition.SetTransition(_stateMachine, condition, toState);
			AddTransition(transition);
		}

		public void ClearAllTransitions()
		{
			LinkedListNode<StateTransition> node = _transitions.First;

			while(node != null)
			{
				node.Value.Release();
				node = node.Next;
			}

			_transitions.Clear();
		}

		public bool TrySwitch()
		{
			LinkedListNode<StateTransition> node = _transitions.First;

			while(node != null)
			{
				if(node.Value.TrySwitch())
				{
					return true;
				}

				node = node.Next;
			}

			return false;
		}

		public override void Release ()
		{
			ClearAllTransitions();
			_stateMachine = null;

			base.Release ();
		}

		public StateMachine StateMachine
		{
			get
			{
				return _stateMachine;
			}
		}
	}
}