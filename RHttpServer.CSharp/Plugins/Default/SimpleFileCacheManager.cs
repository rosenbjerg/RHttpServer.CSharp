using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RHttpServer.Plugins.Default
{
    internal class SimpleFileCacheManager : RPlugin, IFileCacheManager
    {
        private readonly ConcurrentDictionary<string, byte[]> _cachedPages = new ConcurrentDictionary<string, byte[]>();
        
        public int MaxFileSizeBytes { get; set; } = 0x4000;
        public long MaxCacheSizeBytes { get; set; } = 0x3200000;
        private long _size = 0;
        

        public void EmptyCache()
        {
            _cachedPages.Clear();
            _size = 0;
        }

        public bool CanAdd(long filesizeBytes, string filename)
        {
            if (filesizeBytes > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filename)?.ToLowerInvariant() ?? "")) return false;
            return _size + filesizeBytes <= MaxFileSizeBytes;
        }
        
        public IReadOnlyList<string> CacheAllowedFileExtension { get; } = new List<string>
        {
            ".html", ".htm", ".xhtml",
            ".ecs",  ".js",  ".css",
            ".php",  ".txt", ".xml",
            ".csv",  ".json"
        };

        public bool TryGetFile(string filepath, out byte[] content)
        {
            return _cachedPages.TryGetValue(filepath, out content);
        }

        public bool TryAdd(string filepath, byte[] content)
        {
            var len = content.Length;
            if (len > MaxCacheSizeBytes) return false;
            if (_size + len > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)?.ToLowerInvariant() ?? "")) return false;
            var added = _cachedPages.TryAdd(filepath, content);
            if (added) _size += len;
            return added;
        }

        public void Configure(int maxFileSizeBytes, long maxCacheSizeBytes)
        {
            MaxFileSizeBytes = maxFileSizeBytes;
            MaxCacheSizeBytes = maxCacheSizeBytes;
        }
    }
}