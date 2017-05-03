using System;
using System.Collections.Generic;
using System.Linq;
using com.LoanTek.Caching;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Quoting.Mortgage.Common;
using CacheManager.Core.Internal;

namespace com.LoanTek.API.Instances
{
    public class CacheInstance : IInstance
    {
        public CacheInstance() { }

        public CacheInstance(string uri, Quoting.Zillow.Cache cache) : base(uri)
        {
            this.Uri = uri;
            this.Name = cache.Name;
            foreach (var cacheCacheHandle in cache.Cache.CacheHandles)
            {
                var name = cacheCacheHandle.Configuration.Name;
                var config = cache.GetConfig().CacheHandleConfigs?.FirstOrDefault(x => x.Name == cacheCacheHandle.Configuration.Name);
                this.Handles.Add(new CacheInstanceHandle()
                {   
                    Name = name,
                    CacheHandleType = config?.CacheHandleType ?? CacheHandleType.InProcess, //use the default if no handle configs
                    CacheSystemType = config?.CacheSystemType ?? CacheSystemType.Dictionary,
                    ExpirationModeType = (ExpirationModeType)cacheCacheHandle.Configuration.ExpirationMode,
                    ExpirationTime = cacheCacheHandle.Configuration.ExpirationTimeout,
                    ItemCount = cache.Count(CacheStatsCounterType.Items, name),
                    HitCount = cache.Count(CacheStatsCounterType.Hits, name),
                    MissCount = cache.Count(CacheStatsCounterType.Misses, name),
                    GetCount = cache.Count(CacheStatsCounterType.GetCalls, name),
                    PutCount = cache.Count(CacheStatsCounterType.PutCalls, name),
                    ClearCount = cache.Count(CacheStatsCounterType.ClearCalls, name),
                    ClearRegionCount = cache.Count(CacheStatsCounterType.ClearRegionCalls, name)
                });
            }
        }

        public CacheInstance(string uri, Quoting.Common.Cache cache) : base(uri)
        {
            this.Uri = uri;
            this.Name = cache.Name;    
            foreach (var cacheCacheHandle in cache.Cache.CacheHandles)
            {
                var name = cacheCacheHandle.Configuration.Name;
                var config = cache.GetConfig().CacheHandleConfigs?.FirstOrDefault(x => x.Name == cacheCacheHandle.Configuration.Name);
                this.Handles.Add(new CacheInstanceHandle()
                {   
                    Name = name,
                    CacheHandleType = config?.CacheHandleType ?? CacheHandleType.InProcess, //use the default if no handle configs
                    CacheSystemType = config?.CacheSystemType ?? CacheSystemType.Dictionary,
                    ExpirationModeType = (ExpirationModeType) cacheCacheHandle.Configuration.ExpirationMode,
                    ExpirationTime = cacheCacheHandle.Configuration.ExpirationTimeout,
                    ItemCount = cache.Count(CacheStatsCounterType.Items, name),
                    HitCount = cache.Count(CacheStatsCounterType.Hits, name),
                    MissCount = cache.Count(CacheStatsCounterType.Misses, name),
                    GetCount = cache.Count(CacheStatsCounterType.GetCalls, name),
                    PutCount = cache.Count(CacheStatsCounterType.PutCalls, name),
                    ClearCount = cache.Count(CacheStatsCounterType.ClearCalls, name),
                    ClearRegionCount = cache.Count(CacheStatsCounterType.ClearRegionCalls, name)
                });
            }
        }

        public CacheInstance(string uri, RegionIntCache<IMortgageForm, ProcessSubmissionWrapper> cache) : base(uri)
        {
            this.Uri = uri;
            this.Name = cache.Name;
            foreach (var cacheCacheHandle in cache.Cache.CacheHandles)
            {
                var name = cacheCacheHandle.Configuration.Name;
                var config = cache.GetConfig().CacheHandleConfigs?.FirstOrDefault(x => x.Name == cacheCacheHandle.Configuration.Name);
                this.Handles.Add(new CacheInstanceHandle()
                {
                    Name = name,
                    CacheHandleType = config?.CacheHandleType ?? CacheHandleType.InProcess, //use the default if no handle configs
                    CacheSystemType = config?.CacheSystemType ?? CacheSystemType.Dictionary,
                    ExpirationModeType = (ExpirationModeType)cacheCacheHandle.Configuration.ExpirationMode,
                    ExpirationTime = cacheCacheHandle.Configuration.ExpirationTimeout,
                    ItemCount = cache.Count(CacheStatsCounterType.Items, name),
                    HitCount = cache.Count(CacheStatsCounterType.Hits, name),
                    MissCount = cache.Count(CacheStatsCounterType.Misses, name),
                    GetCount = cache.Count(CacheStatsCounterType.GetCalls, name),
                    PutCount = cache.Count(CacheStatsCounterType.PutCalls, name),
                    ClearCount = cache.Count(CacheStatsCounterType.ClearCalls, name),
                    ClearRegionCount = cache.Count(CacheStatsCounterType.ClearRegionCalls, name)
                });
            }
        }
        
        public string Name { get; set; }

        private List<CacheInstanceHandle> handles = new List<CacheInstanceHandle>();
        public List<CacheInstanceHandle> Handles
        {
            get { return handles; }
            set { handles = value; }
        }

        public class CacheInstanceHandle
        {
            public string Name { get; set; }
            public CacheHandleType CacheHandleType { get; set; }
            public CacheSystemType CacheSystemType { get; set; }      
            public ExpirationModeType ExpirationModeType { get; set; }
            public TimeSpan? ExpirationTime { get; set; }
            public long ItemCount { get; set; }
            public long HitCount { get; set; }
            public long MissCount { get; set; }
            public long GetCount { get; set; }
            public long PutCount { get; set; }
            public long ClearCount { get; set; }
            public long ClearRegionCount { get; set; }
        }
    }
}