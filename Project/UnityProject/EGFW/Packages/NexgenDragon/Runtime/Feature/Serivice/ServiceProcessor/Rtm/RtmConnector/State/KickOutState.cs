namespace NexgenDragon
{
	public partial class RtmConnector
	{
        private class KickOutState : BaseRtmConnectorState
		{
			public KickOutState(RtmConnector context) : base(context)
			{

			}

			public override void Enter ()
			{
                NLogger.TraceChannel("RTM", "[KickOutState]Enter");

                base.Enter();

                Context.OnFireKickOutEvent();
            }
		}
	}
}
