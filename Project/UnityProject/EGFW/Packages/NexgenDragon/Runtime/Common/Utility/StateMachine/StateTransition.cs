namespace NexgenDragon
{
	public class StateTransition : NexgenObject
	{
		protected StateMachine _machine;
		protected ICondition _condition;
		protected State _targetState;

		public void SetTransition(StateMachine machine, ICondition condition, State target)
		{
			_machine = machine;
			_condition = condition;
            _condition.SetSM(machine);
			_targetState = target; 
		}

		public virtual bool TrySwitch()
		{
            if (!_condition.IsSatisfied())
            {
                return false;
            }
            
            _machine.ChangeState(_targetState.GetType());

            NLogger.TraceChannel("StateTransition", "Transition: Condition = {0}, State = {1}", 
                _condition.GetType(), _targetState.GetType());

            return true;
        }

		public override void Release ()
		{
			_machine = null;
			_condition = null;
			_targetState = null;

			base.Release ();
		}
	}
}
