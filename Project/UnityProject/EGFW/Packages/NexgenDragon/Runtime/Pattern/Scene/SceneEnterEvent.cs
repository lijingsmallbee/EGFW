namespace NexgenDragon
{
    public class SceneEnterEvent : IEvent
    {
        public const string Name = "SceneEnterEvent";
        public string SceneType { private set; get; }

        public SceneEnterEvent(string scene)
        {
            SceneType = scene;
        }

        public string GetEventType()
        {
            return Name;
        }
    }
}