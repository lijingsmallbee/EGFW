using System;
using System.Collections.Generic;

namespace NexgenDragon
{
	public sealed class GameFacadeConfig : NexgenObject
	{
		//Setup all managers, initialize and relesse them with specific order. Reset all manager when restart.
		public List<IManager> Managers {get;set;}
		public List<NexgenObject> ManagerConfigs {get;set;}

		//Setup all states
		public List<State> States {get;set;}
		public Type StartupStateType {get;set;}
	}
}
