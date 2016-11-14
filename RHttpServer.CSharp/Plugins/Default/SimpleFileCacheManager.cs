using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RHttpServer.Plugins.Default
{
    internal class SimpleFileCacheManager : RPlugin, IFileCacheManager
    {
        private readonly ConcurrentDictionary<string, MemoryStream> _cachedPages = new ConcurrentDictionary<string, MemoryStream>();
        private long _size;

        public long MaxFileSizeBytes { get; set; } = 0x4000;
        public long MaxCacheSizeBytes { get; set; } = 0x3200000;

        public long MaxCacheSizeMegaBytes
        {
            set { MaxCacheSizeBytes = value * 0x100000; }
        }

        public long MaxFileSizeMegaBytes
        {
            set { MaxFileSizeBytes = value * 0x100000; }
        }

        public void EmptyCache()
        {
            _cachedPages.Clear();
            _size = 0;
        }

        public bool CanAdd(long filesizeBytes, string filename)
        {
            if (filesizeBytes > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filename)?.ToLowerInvariant() ?? ""))
                return false;
            return _size + filesizeBytes <= MaxFileSizeBytes;
        }

        public HashSet<string> CacheAllowedFileExtension { get; } = new HashSet<string>
        {
            ".html",
            ".htm",
            ".xhtml",
            ".ecs",
            ".js",
            ".css",
            ".php",
            ".txt",
            ".xml",
            ".csv",
            ".json"
        };

        public bool TryGetFile(string filepath, out MemoryStream content)
        {
            return _cachedPages.TryGetValue(filepath, out content);
        }

        //public bool TryGetFileStream(string filepath, out byte[] content)
        //{
        //    return _cachedPages.TryGetValue(filepath, out content);
        //}

        public bool TryAdd(string filepath, Stream content)
        {
            var len = content.Length;
            if (len > MaxCacheSizeBytes) return false;
            if (_size + len > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)?.ToLowerInvariant() ?? ""))
                return false;
            var mem = new MemoryStream();
            content.CopyTo(mem);
            var added = _cachedPages.TryAdd(filepath, mem);
            if (added) _size += len;
            return added;
        }

        public bool TryAdd(string filepath, byte[] content)
        {
            var len = content.Length;
            if (len > MaxCacheSizeBytes) return false;
            if (_size + len > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)?.ToLowerInvariant() ?? ""))
                return false;
            var mem = new MemoryStream(content);
            var added = _cachedPages.TryAdd(filepath, mem);
            if (added) _size += len;
            return added;
        }


        public async Task<bool> TryAddAsync(string filepath, Stream content)
        {
            var len = content.Length;
            if (len > MaxCacheSizeBytes) return false;
            if (_size + len > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)?.ToLowerInvariant() ?? ""))
                return false;
            var mem = new MemoryStream();
            await content.CopyToAsync(mem);
            var added = _cachedPages.TryAdd(filepath, mem);
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