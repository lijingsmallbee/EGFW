using System;
using System.Collections.Generic;

namespace NexgenDragon
{
    public class MediatorManager : Singleton<MediatorManager>, IManager
    {
        private readonly Dictionary<string, IMediator> _mediatorDict = new Dictionary<string, IMediator>();

        public void Initialize (NexgenObject configParam)
        {
            _mediatorDict.Clear();
        }

        public void Reset ()
        {
            Release();
        }

        public override void Release ()
        {
            foreach (var mediator in _mediatorDict.Values)
            {
                if (mediator != null)
                {
                    mediator.Release();
                }
            }
            _mediatorDict.Clear();
        }

        public IMediator RegisterMediator (IMediator mediator)
        {
            if (!_mediatorDict.ContainsKey(mediator.Name))
            {
                _mediatorDict.Add(mediator.Name, mediator);
                mediator.OnRegister();

                return mediator;
            }
            else
            {
                return _mediatorDict[mediator.Name];
            }
        }

        public IMediator RemoveMediator (string mediatorName)
        {             
            if (!_mediatorDict.ContainsKey(mediatorName)) return null;
            var mediator = _mediatorDict[mediatorName];
            _mediatorDict.Remove(mediatorName);
            mediator.OnRemove();
            return mediator;         
        }

        public IMediator RetrieveMediator (string mediatorName)
        {
            return _mediatorDict.ContainsKey(mediatorName) ? _mediatorDict[mediatorName] : null;
        }
    }
}