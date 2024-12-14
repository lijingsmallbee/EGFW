using System.Collections.Generic;

namespace NexgenDragon
{
	public partial class RtmConnector
	{
        private class WorkingState : BaseRtmConnectorState
		{
			public WorkingState(RtmConnector context):base(context)
			{

			}

			public override void Enter ()
			{
                NLogger.TraceChannel("RTM", "[WorkingState]Enter");

                base.Enter();

                Context.RtmApi.OnDisconnected += OnDisconnected;
                Context.RtmApi.OnReceived += OnReceived;
                Context.RtmApi.OnKickedOut += OnKickedOut;
                Context.OnFireConnectionEvent();
            }

            public override void Exit()
            {
                Context.RtmApi.OnDisconnected -= OnDisconnected;
                Context.RtmApi.OnReceived -= OnReceived;
                Context.RtmApi.OnKickedOut -= OnKickedOut;
            }

            private void OnReceived(long msgId, byte[] received,string receivedString, bool isHistory, int codeType)
            {
                Context.OnFireReceivedEvent(msgId, received,receivedString, isHistory, codeType);
            }
            
            private void OnKickedOut()
            {
                _stateMachine.ChangeState(typeof(KickOutState));
            }

            private void OnDisconnected()
            {
                // 断线重连
                Context.OnFireDisconnectEvent();
                _stateMachine.ChangeState(typeof(ConnectState));
            }
		}
	}
}

