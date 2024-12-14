namespace NexgenDragon
{
	public class BaseCondition : ICondition
	{
        protected StateMachine _sm;
        public virtual bool IsSatisfied ()
        {
            return false;
        }
        public void SetSM(StateMachine sm)
		{
			_sm = sm;
		}
	}
}