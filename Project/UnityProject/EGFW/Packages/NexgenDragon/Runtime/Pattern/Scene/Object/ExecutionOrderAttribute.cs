using System;

namespace NexgenDragon
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ExecutionOrderAttribute : Attribute
	{
		public byte Order { get; set; }

		public ExecutionOrderAttribute (byte order)
		{
			this.Order = order;
		}

		public ExecutionOrderAttribute ()
		{
		}
	}
}

