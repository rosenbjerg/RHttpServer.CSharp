namespace RHttpServer.Plugins
{
    public interface IJsonConverter
    {
        string Serialize(object obj);
        object Deserialize(string jsonData);
    }
}