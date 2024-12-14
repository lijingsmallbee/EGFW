namespace NexgenDragon
{
    public class SceneExitEvent : IEvent
    {
        public const string Name = "SceneExitEvent";
        public string SceneType { private set; get; }

        public SceneExitEvent(string scene)
        {
            SceneType = scene;
        }

        public string GetEventType()
        {
            return Name;
        }
    }
}