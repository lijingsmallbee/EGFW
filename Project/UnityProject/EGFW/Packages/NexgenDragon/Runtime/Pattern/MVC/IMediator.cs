using System.Collections.Generic;

namespace NexgenDragon
{
    public interface IMediator : IObject
    {
		string Name { get; }

        void OnRegister ();

        void OnRemove ();
    }
}
