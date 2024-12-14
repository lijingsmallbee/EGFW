using System;

namespace NexgenDragon
{
    public interface IAsyncOperation
	{
		bool IsDone {get;}
		byte Priority {get;set;}
		float Progress {get;}
	}
}

