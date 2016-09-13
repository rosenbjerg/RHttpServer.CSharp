using ServiceStack.Text;

namespace RHttpServer.Plugins.Default
{
    /// <summary>
    ///     Very simple XmlConverter plugin using ServiceStact.Text generic methods
    /// </summary>
    internal sealed class ServiceStackXmlConverter : RPlugin, IXmlConverter
    {
        public string Serialize<T>(T obj)
        {
            return XmlSerializer.SerializeToString(obj);
        }

        public T Deserialize<T>(string jsonData)
        {
            return XmlSerializer.DeserializeFromString<T>(jsonData);
        }
    }
}