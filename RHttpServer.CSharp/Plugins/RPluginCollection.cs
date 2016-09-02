using System;
using System.Collections.Concurrent;
using RHttpServer.Core;

namespace RHttpServer.Plugins
{
    public class RPluginCollection
    {
        private readonly ConcurrentDictionary<Type, object> _plugins = new ConcurrentDictionary<Type, object>();

        internal void Add(Type pluginInterface, object plugin)
        {
            if (!_plugins.TryAdd(pluginInterface, plugin)) throw new RHttpServerException("You can only register one plugin to a plugin interface");
        }

        public bool IsRegistered<TPluginInterface>() => _plugins.ContainsKey(typeof(TPluginInterface));

        public TPluginInterface Use<TPluginInterface>()
        {
            object obj;
            if (_plugins.TryGetValue(typeof(TPluginInterface), out obj)) return (TPluginInterface) obj;
            throw new RHttpServerException($"You must have registered a plugin the implements '{typeof(TPluginInterface).Name}'");
        }
    }
}