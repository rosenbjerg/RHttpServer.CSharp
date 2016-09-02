using ServiceStack.Text;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// Very simple JsonConverter plugin using ServiceStact.Text generic methods
    /// </summary>
    internal sealed class ServiceStackJsonConverter : RPlugin, IJsonConverter
    {
        public string Serialize<T>(T obj)
        {
            return TypeSerializer.SerializeToString<T>(obj);
        }

        public T Deserialize<T>(string jsonData)
        {
            return TypeSerializer.DeserializeFromString<T>(jsonData);
        }
    }
}