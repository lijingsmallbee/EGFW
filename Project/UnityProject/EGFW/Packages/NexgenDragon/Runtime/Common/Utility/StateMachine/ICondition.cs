namespace NexgenDragon
{
	public interface ICondition
	{
		bool IsSatisfied();
        void SetSM(StateMachine sm);
	}
}