using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RHttpServer.Plugins.Default
{
    internal class SimpleFileCacheManager : RPlugin, IFileCacheManager
    {
        private readonly ConcurrentDictionary<string, byte[]> _cachedPages = new ConcurrentDictionary<string, byte[]>();
        private readonly object _lock = new object();
        private long _size;

        public long MaxFileSizeBytes { get; set; } = 0x4000;
        public long MaxCacheSizeBytes { get; set; } = 0x3200000;

        public long MaxCacheSizeMegaBytes
        {
            set { MaxCacheSizeBytes = value*0x100000; }
        }

        public long MaxFileSizeMegaBytes
        {
            set { MaxFileSizeBytes = value*0x100000; }
        }

        private void IncrementSize(long toAdd)
        {
            lock (_lock)
            {
                _size += toAdd;
            }
        }

        public void EmptyCache()
        {
            _cachedPages.Clear();
            _size = 0;
        }

        public bool CanAdd(long filesizeBytes, string filename)
        {
            if (filesizeBytes > MaxFileSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filename)))
                return false;
            return _size + filesizeBytes <= MaxCacheSizeBytes;
        }

        public HashSet<string> CacheAllowedFileExtension { get; } =
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
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

        public bool TryGetFile(string filepath, out byte[] content)
        {
            return _cachedPages.TryGetValue(filepath, out content);
        }

        public bool TryAdd(string filepath, byte[] content)
        {
            var len = content.Length;
            if (len > MaxFileSizeBytes) return false;
            if (_size + len > MaxCacheSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)))
                return false;
            var added = _cachedPages.TryAdd(filepath, content);
            if (added) IncrementSize(len);
            return added;
        }

        public void Configure(int maxFileSizeBytes, long maxCacheSizeBytes)
        {
            MaxFileSizeBytes = maxFileSizeBytes;
            MaxCacheSizeBytes = maxCacheSizeBytes;
        }

        public bool TryAddFile(string filepath)
        {
            var len = new FileInfo(filepath).Length;
            if (len > MaxFileSizeBytes) return false;
            if (_size + len > MaxCacheSizeBytes) return false;
            if (!CacheAllowedFileExtension.Contains(Path.GetExtension(filepath)))
                return false;
            var added = _cachedPages.TryAdd(filepath, File.ReadAllBytes(filepath));
            if (added) IncrementSize(len);
            return true;
        }
    }
}