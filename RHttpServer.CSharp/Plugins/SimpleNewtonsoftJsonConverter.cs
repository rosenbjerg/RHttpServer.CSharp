namespace RHttpServer.Plugins
{
    public class SimpleNewtonsoftJsonConverter : SimplePlugin, IJsonConverter
    {
        public string Serialize(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public object Deserialize(string jsonData)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);
        }
    }
}