using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace XSP.Engine
{
    internal class XspFileCache
	{
		private readonly MemoryCache cache;

        public XspFileCache(string cacheName = "XspFileCache")
        {
			cache = new(cacheName);
		}

		public void Clear()
        {
			var keys = cache.Select(e => e.Key).ToList();
			foreach(var key in keys)
            {
				cache.Remove(key);
            }
        }

		public Action<String>? FileChangedHandler { get; set; } = null;

		public XspFileSource<T>? Get<T>(string fileName)
        {
			if (this.cache[fileName] is XspFileSource<T> cacheValue)
            {
				return cacheValue;
            }

			return default;
		}

		public void Set<T>(string fileName, T value, IEnumerable<string> sources, int seconds = 60)
        {
			CacheItemPolicy policy = new();
			policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds);
			if (sources.Any())
            {
				policy.ChangeMonitors.Add(new HostFileChangeMonitor(sources.ToList()));
			}

			var cacheItem = new XspFileSource<T>(value, sources);
			cache.Set(fileName, cacheItem, policy);
		}

		public XspResult<T> GetOrAdd<T>(string fileName, Func<string, T> creator, int seconds = 60)
		{
			return GetOrAdd<T>(fileName, creator, out _, seconds);
		}

		public XspResult<T> GetOrAdd<T>(string fileName, Func<string, T> creator, out IEnumerable<string> sources, int seconds = 60)
		{
			if (this.cache[fileName] is XspFileSource<T> cacheValue)
			{
				sources = cacheValue.Sources;

				return new XspResult<T>(cacheValue.Value);
			}

			var result = XspResult<T>.SafeCall(() => creator(fileName));
			if (result.Error != null)
			{
				sources = Array.Empty<string>();
				return result;
			}

			CacheItemPolicy policy = new();
			policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds);
			policy.ChangeMonitors.Add(GetFileChangeMonitor(fileName));

			if (FileChangedHandler != null)
			{
				policy.RemovedCallback = args =>
				{
					if (args.RemovedReason == CacheEntryRemovedReason.ChangeMonitorChanged)
					{
						FileChangedHandler(args.CacheItem.Key);
					}
				};
			}

			var cacheItem = new XspFileSource<T>(result.Value!, new[] { fileName });
			cache.Set(fileName, cacheItem, policy);
			sources = cacheItem.Sources;

			return result;

		}

		public XspResult<T> GetOrAdd<T>(string fileName, Func<string, Tuple<T, List<string>>> creator, int seconds = 60)
		{
			return GetOrAdd<T>(fileName, creator, out _, seconds);
		}

		public XspResult<T> GetOrAdd<T>(string fileName, Func<string, Tuple<T, List<string>>> creator, out IEnumerable<string> sources, int seconds = 60)
        {
			if (this.cache[fileName] is XspFileSource<T> cacheValue)
			{
				sources = cacheValue.Sources;
				return new XspResult<T>(cacheValue.Value);
			}

			var result = XspResult<Tuple<T, List<string>>>.SafeCall(() => creator(fileName));
			if (result.Error != null)
			{
				sources = Array.Empty<string>();
				return new XspResult<T>(result.Error);
			}

			CacheItemPolicy policy = new();
			policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds);
			policy.ChangeMonitors.Add(new HostFileChangeMonitor(result.Value!.Item2));

			if (FileChangedHandler != null)
			{
				policy.RemovedCallback = args =>
				{
					if (args.RemovedReason == CacheEntryRemovedReason.ChangeMonitorChanged)
					{
						FileChangedHandler(args.CacheItem.Key);
					}
				};
			}

			var cacheItem = new XspFileSource<T>(result.Value!.Item1, result.Value!.Item2.ToArray());
			cache.Set(fileName, cacheItem, policy);
			sources = cacheItem.Sources;

			return cacheItem.Value!;

		}

		private HostFileChangeMonitor GetFileChangeMonitor(string fileName)
		{
			List<string> filePaths = new() { fileName };
			return new HostFileChangeMonitor(filePaths);
		}
	}
}

