using System;

namespace NexgenDragon
{
	public enum RtmError
	{
		DispatchError,
		SocketConnectionError,
		RtmConnectionError,
		AuthError,
		InvalidOperationError,
		SendError,
		SetConfigError,
		GetGroupHistoryError,
		GetP2pHistoryError,
		CheckUnreadError,
	}
}