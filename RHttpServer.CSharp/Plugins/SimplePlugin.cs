namespace RHttpServer.Plugins
{
    /// <summary>
    /// Abstract class that all plugins must derive from
    /// </summary>
    public abstract class SimplePlugin
    {
        private SimplePlugins _plugins;

        internal void SetPlugins(SimplePlugins plugins)
        {
            _plugins = plugins;
        }

        /// <summary>
        /// Use a plugin registered to the server
        /// </summary>
        /// <typeparam name="TPluginInterface">The type the plugin implements</typeparam>
        /// <returns>The instance of the plugin</returns>
        protected TPluginInterface UsePlugin<TPluginInterface>()
        {
            return _plugins.Use<TPluginInterface>();
        }
    }
}