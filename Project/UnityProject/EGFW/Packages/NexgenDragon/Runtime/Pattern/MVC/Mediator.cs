namespace NexgenDragon
{
    public class Mediator: IMediator
    {
        public void Release ()
        {
            throw new System.NotImplementedException();
        }

        public const string NAME = "Mediator";

        protected string _mediatorName;

        protected object _viewComponent;

        public Mediator() : this(NAME, null)
        {
        }

        public Mediator(string mediatorName) : this(mediatorName, null)
        {
        }

        public Mediator(string mediatorName, object viewComponent)
        {
            _mediatorName = mediatorName ?? NAME;
            _viewComponent = viewComponent;
        }

        public virtual void OnRegister ()
        {
        }

        public virtual void OnRemove ()
        {
        }

        public virtual string Name
        {
            get { return _mediatorName; }
        }

        public virtual object ViewComponent
        {
            get { return _viewComponent; }
            set { _viewComponent = value; }
        }
    }
}