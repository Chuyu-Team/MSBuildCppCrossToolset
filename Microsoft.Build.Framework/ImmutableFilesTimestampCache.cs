using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Framework
{
    internal class ImmutableFilesTimestampCache
    {
        private readonly ConcurrentDictionary<string, DateTime> _cache = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public static ImmutableFilesTimestampCache Shared { get; } = new ImmutableFilesTimestampCache();


        public bool TryGetValue(string fullPath, out DateTime lastModified)
        {
            return _cache.TryGetValue(fullPath, out lastModified);
        }

        public void TryAdd(string fullPath, DateTime lastModified)
        {
            _cache.TryAdd(fullPath, lastModified);
        }
    }
}
