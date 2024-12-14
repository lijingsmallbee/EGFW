namespace NexgenDragon
{
	public partial class RtmConnector 
	{
		private class ConnectState : BaseRtmConnectorState
		{
			public ConnectState(RtmConnector context) : base(context)
			{
				
			}

			public override void Enter ()
			{
                NLogger.TraceChannel("RTM", "[ConnectState]Enter");

                base.Enter();

                Context.RtmApi.OnError += OnError;
                Context.RtmApi.OnConnected += OnConnected;
                Context.RtmApi.OnDisconnected += OnDisconnected;
                
                // 重置重试策略
                Context._connectStrategy.Reset();
			}

			public override void Exit()
			{
				base.Exit();
				
				Context.RtmApi.OnError -= OnError;
                Context.RtmApi.OnConnected -= OnConnected;
                Context.RtmApi.OnDisconnected -= OnDisconnected;
                Context._connectStrategy.Reset();
			}

            public override void Tick(float dt)
            {
                base.Tick(dt);

                var operation = Context._connectStrategy.GetConnectOperation(dt);
                switch (operation)
                {
                    case ConnectOperation.Connect:
                        Context.DoConnect();
                        break;
					
                    case ConnectOperation.HoldOn:
                        break;
					
                    case ConnectOperation.Break:
                        Context.OnFireConnectFailedEvent();
                        Context.Disconnect();
                        break;
                }
            }

			private void OnError(RtmError error)
			{
			    NLogger.TraceChannel("RTM", "[ConnectState]OnError: Error = {0}", error.ToString());
				Context._connectStrategy.OnError();
			}
			
            private void OnConnected()
            {
                _stateMachine.ChangeState(typeof(WorkingState));
            }

            private void OnDisconnected()
            {
                OnError(RtmError.RtmConnectionError);
            }
		}
	}
}

