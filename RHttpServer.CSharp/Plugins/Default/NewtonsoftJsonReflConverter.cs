namespace RHttpServer.Plugins.Default
{
    /// <summary>
    /// Very simple JsonConverter plugin using Newtonsoft.Json generic methods
    /// </summary>
    internal sealed class NewtonsoftJsonReflConverter : RPlugin, IJsonConverter
    {
        public string Serialize<T>(T obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public T Deserialize<T>(string jsonData)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}