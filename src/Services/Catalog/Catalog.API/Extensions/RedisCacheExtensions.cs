using System.IO;

using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;

namespace Catalog.API.Extensions
{
    public static class RedisCacheExtensions
    {
        public static readonly string RedisKeys = "KEYS";

        public static async Task<T> TryGetAsync<T>(this IDistributedCache cache, string key)
        {
            var value = await cache.GetAsync(key);

            if (value != null)
            {
                var CacheValue = FromByteArray<T>(value);
                return CacheValue;
            }
            return default(T);
        }

        public static async Task TrySetAsync<T>(this IDistributedCache cache, string key, T value)
        {
            if (value == null)
                return;

            T temp = await cache.TryGetAsync<T>(key);

            if (temp == null || temp.Equals(default(T)))
            {
                var array = ToByteArray<T>(value);
                cache.SetKeysAsync(key);
                await cache.SetAsync(key, array);
            }
        }

        public static async Task<bool> TryResetAsync<T>(this IDistributedCache cache, string key, T value)
        {
            T temp = await cache.TryGetAsync<T>(key);

            if (temp != null && !temp.Equals(default(T)))
            {
                await cache.RemoveAsync(key);
            }

            var array = ToByteArray<T>(value);
            await cache.TrySetAsync(key, value);

            return true;
        }

        public static async Task<bool> RemovePartialAsync<T>(this IDistributedCache cache, List<string> dismissList)
        {
            var values = await cache.TryGetAsync<List<string>>(RedisKeys);
            return await Task.Run(async () => {
                values.ForEach(async k =>
                {
                    if (!dismissList.Contains(k))
                    {
                        T temp = await cache.TryGetAsync<T>(k);

                        if (temp != null && !temp.Equals(default(T)))
                        {
                            await cache.RemoveAsync(k);
                        }
                    }
                });
                await cache.RemoveAsync(RedisKeys);
                return true;
            });            
        }

        private static byte[] ToByteArray<T>(T obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
                return default(T);

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                return (T)bf.Deserialize(ms);
            }
        }

        private static async void SetKeysAsync(this IDistributedCache cache, string key)
        {
            var values = await cache.TryGetAsync<List<string>>(RedisKeys);

            if (values == null)
                values = new List<string>();

            if (!values.Contains(key))
            {
                values.Add(key);
            }

            var array = ToByteArray<List<string>>(values);
            await cache.SetAsync(RedisKeys, array);
        }
    }
}
