namespace RHttpServer.Plugins
{
    /// <summary>
    ///     Interface for pluginCollection that is used for Json serialization and deserialization
    /// </summary>
    public interface IJsonConverter
    {
        /// <summary>
        /// Method to serialize JSON data
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string Serialize<T>(T obj);

        /// <summary>
        /// Method to deserialize JSON data
        /// </summary>
        /// <param name="jsonData"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(string jsonData);
    }
}