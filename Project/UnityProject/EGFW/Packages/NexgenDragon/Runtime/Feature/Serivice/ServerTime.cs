using NexgenDragon;

public class ServerTime : Singleton<ServerTime>
{
	// 单精度浮点数精度不够，因此使用双精度浮点数。
	private double _syncRealTimeSinceStartup;
	private double _syncServerTimestampInSeconds;
	private long _syncServerTimestampInMilliseconds;
	private long _syncLocalTimestampInMilliseconds;

    public void SetServerTime(long timestamp)
    {
		NLogger.LogChannel("ServerTime", string.Format("Sync Server Time: current local time: {0}, server sync time: {1}， gap is: {2}", ServerTimestampInMilliseconds, timestamp, timestamp - ServerTimestampInMilliseconds));
    
		if (_syncServerTimestampInMilliseconds >= timestamp)
		{
			return;
		}

		_syncLocalTimestampInMilliseconds = TimeUtils.LocalTimestamp;
		_syncServerTimestampInMilliseconds = timestamp;

		_syncRealTimeSinceStartup = TimeUtils.RealtimeSinceStartup;
		_syncServerTimestampInSeconds = timestamp / 1000.0;
    }

    public void Reset()
    {
	    _syncRealTimeSinceStartup = 0;
	    _syncServerTimestampInSeconds = 0;
	    _syncServerTimestampInMilliseconds = 0;
	    _syncLocalTimestampInMilliseconds = 0;
    }
    
	// 服务端时间为基准的时间戳（单位：毫秒）
    public long ServerTimestampInMilliseconds
    {
        get
        {
			return _syncServerTimestampInMilliseconds + (TimeUtils.LocalTimestamp - _syncLocalTimestampInMilliseconds);
        }
    }

    // 服务端时间为基准的游戏运行时间戳（单位：毫秒）
    public long RealServerTimestampInMilliseconds
    {
	    get
	    {
		    return (long) (_syncServerTimestampInMilliseconds +
		                   (TimeUtils.RealtimeSinceStartup - _syncRealTimeSinceStartup) * 1000.0);
	    }
    }

	// 服务端时间为基准的时间戳（单位：秒）
	public long ServerTimestampInSeconds
	{
		get
		{
			return ServerTimestampInMilliseconds / 1000L;
		}
	}

	private const long MilliSecondsInDay = 24 * 60 * 60 * 1000;
	public long ServerTimestampInDays
	{
		get
		{
			return ServerTimestampInMilliseconds / MilliSecondsInDay;
		}
	}

	// 服务端时间为基准的时间戳（单位：秒）
	public double UpdateTimestampInSeconds
	{
		get
		{
			return _syncServerTimestampInSeconds + (TimeUtils.RealtimeSinceStartup - _syncRealTimeSinceStartup);
		}
	}

	public System.DateTime UtcNow
	{
		get { return TimeUtils.TIME19700101.AddMilliseconds(ServerTimestampInMilliseconds); }
	}
}
