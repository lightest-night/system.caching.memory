using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utf8Json;

namespace LightestNight.System.Caching.Memory
{
    public class Cache : ICache
    {
        private static readonly IDictionary<string, string> KeyedCache = new Dictionary<string, string>();
        private static readonly IDictionary<string, List<string>> TaggedKeys = new Dictionary<string, List<string>>();

        private readonly ICacheItemFactory _cacheItemFactory = new CacheItemFactory();
        
        public Task Save<TItem>(string key, TItem item, DateTime? expiry = null, params string[]? tags) where TItem : notnull
        {
            ExpiryCheck();
            
            var cacheKey = GenerateKey<TItem>(key);
            var cacheItem = _cacheItemFactory.Create(cacheKey, item, expiry, tags);

            if (!CacheItemIsValid(cacheItem))
                return Task.CompletedTask;
            
            KeyedCache[cacheKey] = JsonSerializer.ToJsonString(cacheItem);

            if (tags == null)
                return Task.CompletedTask;

            if (!tags.Any()) 
                return Task.CompletedTask;
            
            foreach (var tag in tags)
            {
                var tagKeyItem = TaggedKeys.ContainsKey(tag)
                    ? TaggedKeys[tag] ?? new List<string>()
                    : new List<string>();
                
                if (!tagKeyItem.Contains(cacheKey))
                    tagKeyItem.Add(cacheKey);

                TaggedKeys[tag] = tagKeyItem;
            }

            return Task.CompletedTask;
        }

        public Task<TItem> Get<TItem>(string key)
        {
            ExpiryCheck();
            
            var cacheKey = GenerateKey<TItem>(key);
            return GetFromCache<TItem>(cacheKey);
        }

        public async Task<IEnumerable<TItem>> GetByTag<TItem>(string tag)
        {
            ExpiryCheck();
            
            if (!TaggedKeys.ContainsKey(tag))
                return Enumerable.Empty<TItem>();

            var taggedItems = TaggedKeys[tag];
            if (!taggedItems.Any())
                return Enumerable.Empty<TItem>();

            var cachedItems = await Task.WhenAll(taggedItems.Select(GetFromCache<TItem>));
            return cachedItems;
        }

        public Task Delete<TItem>(string key)
        {
            var cacheKey = GenerateKey<TItem>(key);
            if (KeyedCache.ContainsKey(cacheKey))
            {
                var cacheResult = JsonSerializer.Deserialize<CacheItem<TItem>>(KeyedCache[cacheKey]);
                cacheResult.Expiry = DateTime.UtcNow.AddHours(-1);

                KeyedCache[cacheKey] = JsonSerializer.ToJsonString(cacheResult);
            }

            ExpiryCheck();
            return Task.CompletedTask;
        }
        
        private static Task<TItem> GetFromCache<TItem>(string cacheKey)
        {
            if (!KeyedCache.ContainsKey(cacheKey)) 
                return Task.FromResult(default(TItem)!);
            
            var cacheItem = KeyedCache[cacheKey];
            var cacheResult = JsonSerializer.Deserialize<CacheItem<TItem>>(cacheItem);

            return Task.FromResult(CacheItemIsValid(cacheResult) 
                ? cacheResult.Value 
                : default);
        }

        private static bool CacheItemIsValid(CacheItem cacheItem)
            => !cacheItem.Expiry.HasValue || cacheItem.Expiry >= DateTime.UtcNow;

        private static void ExpiryCheck()
        {
            var cacheItems = KeyedCache.Values.Select(JsonSerializer.Deserialize<CacheItem>)
                .Where(item => item.Expiry < DateTime.UtcNow);
            var emptyTags = TaggedKeys.Where(taggedKey => !taggedKey.Value.Any()).Select(taggedKey => taggedKey.Key);

            foreach (var item in cacheItems)
            {
                KeyedCache.Remove(item.Key);

                if (item.Tags == null) 
                    continue;
                
                foreach (var tag in item.Tags)
                {
                    if (!TaggedKeys.ContainsKey(tag))
                        continue;

                    var taggedItem = TaggedKeys[tag];
                    taggedItem.Remove(item.Key);
                }
            }

            foreach (var emptyTag in emptyTags)
                TaggedKeys.Remove(emptyTag);
        }
        
        private static string GenerateKey<T>(string key)
            => $"{typeof(T).Name}:{key}";
    }
}