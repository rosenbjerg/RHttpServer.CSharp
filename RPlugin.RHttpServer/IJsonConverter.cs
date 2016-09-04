namespace RPlugin.RHttpServer
{
    /// <summary>
    ///     Interface for pluginCollection that is used for Json serialization and deserialization
    /// </summary>
    public interface IJsonConverter
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string jsonData);
    }
}