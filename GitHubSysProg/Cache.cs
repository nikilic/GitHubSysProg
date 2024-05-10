namespace GitHubSysProg;

public static class Cache {
    private static readonly ReaderWriterLockSlim CacheLock = new();
    private static readonly Dictionary<string, CacheItem> CacheDict = new();

    public static bool Contains(string key){
        CacheLock.EnterReadLock();
        try{
            if (CacheDict.TryGetValue(key, out var cacheItem)){
                if (cacheItem.ExpirationTime < DateTime.Now){
                    CacheDict.Remove(key);
                    return false;
                }
                return true;
            }
            return false;
        }
        finally{
            CacheLock.ExitReadLock();
        }
    }
    
    public static List<Contributor>? ReadFromCache(string key){
        CacheLock.EnterReadLock();
        try{
            if (CacheDict.TryGetValue(key, out var cacheItem)){
                if (cacheItem.ExpirationTime < DateTime.Now){
                    CacheDict.Remove(key);
                    return null;
                }
                return cacheItem.Value;
            }
            return null;
        }
        finally{
            CacheLock.ExitReadLock();
        }
    }

    public static void WriteToCache(string key, List<Contributor>? value){
        CacheLock.EnterWriteLock();
        try{
            var expirationTime = DateTime.Now.AddHours(1);
            CacheDict[key] = new CacheItem(value, expirationTime);
        }
        finally{
            CacheLock.ExitWriteLock();
        }
    }
    
    private class CacheItem {
        public List<Contributor>? Value { get; }
        public DateTime ExpirationTime { get; }

        public CacheItem(List<Contributor>? value, DateTime expirationTime){
            Value = value;
            ExpirationTime = expirationTime;
        }
    }
}
