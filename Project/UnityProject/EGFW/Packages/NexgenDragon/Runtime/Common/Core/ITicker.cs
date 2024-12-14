using System;

namespace NexgenDragon
{
	// We could use Attribute to identify different interface. But performance less than current solution.

	/// <summary>
	/// Normal ticker
	/// Tick be invoked every frame
	/// </summary>
	public interface ITicker
	{
		void Tick(float delta);
	}

	/// <summary>
	/// the logic ticker.
	/// Tick be invoked every two frames
	/// </summary>
	public interface IGapTicker : ITicker
	{
		
	}

	/// <summary>
	/// The second ticker
	/// Tick be invoked every second
	/// </summary>
	public interface ISecondTicker : ITicker
	{
		
	}

    public interface ITimeScaleIgnoredTicker : ITicker
    {
        
    }

    public interface ILateUpdateTicker : ITicker
    {
	    
    }
}

