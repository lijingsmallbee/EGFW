namespace NexgenDragon
{
	public delegate void AsyncCallback();

	/// <summary>
	/// Synchronize Load Interface
	/// </summary>
	public interface ILoadable
	{
		void Load();
		void Unload();
	}

	/// <summary>
	/// Aynchronous Load Interface
	/// </summary>
	public interface IAsyncLoadable
	{
		void LoadAsync(AsyncCallback callback);
		void Unload();
	}
}

