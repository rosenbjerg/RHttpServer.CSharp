namespace RHttpServer.Plugins
{
    public abstract class SimplePlugin
    {
        private SimplePlugins _plugins;

        internal void SetPlugins(SimplePlugins plugins)
        {
            _plugins = plugins;
        }

        public TPluginInterface UsePlugin<TPluginInterface>()
        {
            return _plugins.Use<TPluginInterface>();
        }
    }
}