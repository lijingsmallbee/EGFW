namespace NexgenDragon
{
    public interface IDataParser
    {
        T FromJson<T>(object source);
        string ToJson(object data);
    }
}