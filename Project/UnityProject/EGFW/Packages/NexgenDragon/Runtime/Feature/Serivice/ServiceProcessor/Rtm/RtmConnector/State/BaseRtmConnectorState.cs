namespace NexgenDragon
{
	public partial class RtmConnector 
	{
		public class BaseRtmConnectorState : State
		{
			protected readonly RtmConnector Context;

            protected BaseRtmConnectorState(RtmConnector context)
			{
				Context = context;
			}

			public override void Enter ()
			{
                
			}

			public override void Tick (float delta)
			{
				
			}

			public override void Exit ()
			{
                
			}
		}
	}
}

