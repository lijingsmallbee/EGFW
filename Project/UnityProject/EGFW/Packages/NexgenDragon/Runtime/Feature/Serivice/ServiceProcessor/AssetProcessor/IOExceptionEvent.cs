namespace NexgenDragon
{
    public class IOExceptionEvent : IEvent
    {
        public const string NAME = "IOExceptionEvent";
        public enum Exception
        {
            Write,
            Read
        }

        public Exception Exce
        {
            get;
            set;
        }

        public System.Exception innerException { private set; get; }

        public IOExceptionEvent(Exception exception)
        {
            Exce = exception;
        }
        
        public IOExceptionEvent(Exception exception, System.Exception ex)
        {
            Exce = exception;
            this.innerException = ex;
        }

        public string GetEventType( )
        {
            return NAME;
        }
    }
}