using System.Collections.Concurrent;

namespace Services
{
    /// <summary>
    /// Provides caching functionality for database metadata to improve performance
    /// and enable lazy loading of resources.
    /// </summary>
    public class DatabaseMetadataCache
    {
        private readonly ConcurrentDictionary<string, object> _cache = new();
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets a value from the cache if it exists, or adds it using the provided factory method.
        /// </summary>
        /// <typeparam name="T">The type of value to retrieve or create.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">A function that creates the value if it doesn't exist in the cache.</param>
        /// <param name="expiration">Optional custom expiration time. If not provided, the default expiration is used.</param>
        /// <returns>The cached or newly created value.</returns>
        public T GetOrAdd<T>(string key, Func<T> valueFactory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                return typedValue;
            }

            var value = valueFactory();
            _cache[key] = value!; // Use null-forgiving operator since we're adding it to the cache
            
            // Schedule expiration
            var expirationTime = expiration ?? _defaultExpiration;
            // Intentionally not awaited - we want this to run in the background
            #pragma warning disable CS4014
            Task.Delay(expirationTime).ContinueWith(_ =>
            {
                _cache.TryRemove(key, out object? removed);
            });
            
            return value;
        }

        /// <summary>
        /// Gets a value from the cache if it exists, or adds it using the provided async factory method.
        /// </summary>
        /// <typeparam name="T">The type of value to retrieve or create.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">An async function that creates the value if it doesn't exist in the cache.</param>
        /// <param name="expiration">Optional custom expiration time. If not provided, the default expiration is used.</param>
        /// <returns>The cached or newly created value.</returns>
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> valueFactory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                return typedValue;
            }

            var value = await valueFactory();
            _cache[key] = value!; // Use null-forgiving operator since we're adding it to the cache
            
            // Schedule expiration
            var expirationTime = expiration ?? _defaultExpiration;
            // Intentionally not awaited - we want this to run in the background
            #pragma warning disable CS4014
            Task.Delay(expirationTime).ContinueWith(_ =>
            {
                _cache.TryRemove(key, out object? removed);
            });
            
            return value;
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear() => _cache.Clear();

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets all keys in the cache.
        /// </summary>
        public IEnumerable<string> Keys => _cache.Keys;

        /// <summary>
        /// Removes a specific key from the cache.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and removed; otherwise, false.</returns>
        public bool Remove(string key)
        {
            return _cache.TryRemove(key, out object? removed);
        }
    }
}