namespace NexgenDragon
{
    public interface IModule : IObject
    {
		// 当模块注册到DatabaseManager的时候触发
        void OnRegister ();

		// 当模块从DatabaseManager移除的时候触发
        void OnRemove ();

        string GetName();
    }
}