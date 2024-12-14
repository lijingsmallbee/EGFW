namespace NexgenDragon
{
    public class RtmHttpAsyncOperation : IHttpAsyncOperation
    {
        public bool IsDone { get; set; }
        public byte Priority { get; set; }
        public float Progress { get; set; }
        public void Abort()
        {
            
        }
    }
}