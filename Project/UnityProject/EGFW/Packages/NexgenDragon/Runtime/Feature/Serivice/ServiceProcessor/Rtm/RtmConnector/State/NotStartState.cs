using System;

namespace NexgenDragon
{
	public partial class RtmConnector
	{
        private class NotStartState : BaseRtmConnectorState
		{
			public NotStartState(RtmConnector context):base(context)
			{
				
			}

			public override void Enter ()
			{
                NLogger.TraceChannel("RTM", "[NotStartState]Enter");

                base.Enter();

				if(Context.RtmApi.State != RtmState.Unconnected)
				{
					Context.RtmApi.Disconnect(DisconnectReason.Normal_DisconnectBeforeConnect);
				}
			}
		}
	}
}

