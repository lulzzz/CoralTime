﻿using CoralTime.Common.Exceptions;
using CoralTime.DAL.Cache;
using CoralTime.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using CoralTime.DAL.Interfaces;

namespace CoralTime.DAL.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected int CurrentClientId;
        private DbContext _db;
        private readonly DbSet<T> _dbSet;
        private readonly IMemoryCache _memoryCache;
        protected readonly ICacheManager CacheManager;
        private static readonly object LockObject = new object();
        private readonly string _userId;

        protected BaseRepository(AppDbContext context, IMemoryCache memoryCache, string userId)
        {
            _db = context;
            _dbSet = _db.Set<T>();
            _memoryCache = memoryCache;
            CacheManager = CacheMemoryFactory.CreateCacheMemory(_memoryCache);

            _userId = userId;
        }

        #region GetQuery.

        public virtual IQueryable<T> GetIncludes(IQueryable<T> query)
        {
            return query;
        }
        
        public virtual IQueryable<T> GetQueryWithIncludes()
        {
            return GetIncludes(_dbSet);
        }

        public virtual IQueryable<T> GetQueryWithoutIncludes()
        {
            return _dbSet;
        }

        public virtual IQueryable<T> GetQueryAsNoTraking()
        {
            return _dbSet.AsNoTracking();
        }

        public virtual IQueryable<T> GetQueryAsNoTrakingWithIncludes()
        {
            return GetIncludes(GetQueryAsNoTraking());
        }

        public virtual T GetQueryByIdWithIncludes(int id)
        {
            return null;
        }

        #endregion

        #region LinkedCache. 

        public virtual T LinkedCacheGetByName(string name)
        {
            return null;
        }

        public virtual T LinkedCacheGetById(int Id)
        {
            return null;
        }

        public virtual List<T> LinkedCacheGetList()
        {
            try
            {
                var key = GenerateCacheKey();
                var items = CacheManager.CachedListGet<T>(key);
                if (items != null) 
                    return items;
                
                lock (LockObject)
                {
                    var cachedItems = CacheManager.CachedListGet<T>(key);
                    if (cachedItems != null)
                        return cachedItems;
                        
                    items = GetQueryAsNoTrakingWithIncludes().ToList();
                    CacheManager.LinkedPutList(key, items);
                }

                return items;
            }
            catch (Exception seq)
            {
                throw new CoralTimeDangerException(seq.Message, seq);
            }
        }

        public virtual void LinkedCacheClear()
        {
            CacheManager.LinkedCacheClear<T>();
        }

        #endregion

        #region Single Cache.

        protected int DefaultCacheTime { get; set; } = 800;

        //public virtual List<TEntity> GetCachedList(Func<List<TEntity>> getListFunc)
        //{
        //    string key = GenerateClientUniqueCacheKey();
        //    return CacheManager.Get(key, getListFunc);
        //}

        protected virtual string GenerateCacheKey()
        {
            var entityName = typeof(T).Name;
            var key = $"{entityName}_CacheKey";
            return key;
        }

        public string GenerateClientUniqueCacheKey(string userName)
        {
            var entityName = typeof(T).Name;
            var key = $"{userName}_{entityName}_CacheKey";
            return key;
        }

        public void ClearEntityCache()
        {
            var key = GenerateCacheKey();
            CacheManager.Remove(key);
        }

        #endregion

        #region CRUD.

        public virtual T GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual void Insert(T entity)
        {
            if (entity is ILogChanges entityILogChange)
            {
                entityILogChange.CreatorId = _userId;
                entityILogChange.CreationDate = DateTime.Now;

                entityILogChange.LastEditorUserId = _userId;
                entityILogChange.LastUpdateDate = DateTime.Now;

                entity = (T)entityILogChange;
            }

            _dbSet.Add(entity);
        }

        public virtual void InsertRange(IEnumerable<T> entities)
        {
            if (entities is IEnumerable<ILogChanges> entitiesILogChange)
            {
                foreach (var entityILogChange in entitiesILogChange)
                {
                    entityILogChange.CreatorId = _userId;
                    entityILogChange.CreationDate = DateTime.Now;

                    entityILogChange.LastUpdateDate = DateTime.Now;
                    entityILogChange.LastEditorUserId = _userId;
                }

                entities = (IEnumerable<T>)entitiesILogChange;
            }

            _dbSet.AddRange(entities);
        }

        public virtual void Update(T entity)
        {
            if (entity is ILogChanges entityILogChange)
            {
                //var t = Db.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified);
                //var canWrite = t.Any(x => ((DateTime)x.CurrentValues["LastUpdateDate"]).ToString("G") > ((DateTime)x.OriginalValues["LastUpdateDate"]).ToString("G"));
                entityILogChange.LastEditorUserId = _userId;
                entityILogChange.LastUpdateDate = DateTime.Now;

                entity = (T)entityILogChange;
            }

            _dbSet.Update(entity);
        }

        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            if (entities is IEnumerable<ILogChanges> entitiesILogChange)
            {
                foreach (var entityILogChange in entitiesILogChange)
                {
                    entityILogChange.LastEditorUserId = _userId;
                    entityILogChange.LastUpdateDate = DateTime.Now;
                }

                entities = (IEnumerable<T>)entitiesILogChange;
            }

            _dbSet.UpdateRange(entities);
        }

        public virtual void Delete(object id)
        {
            var entityToDelete = _dbSet.Find(id);
            Delete(entityToDelete);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (_db.Entry(entityToDelete).State == EntityState.Detached)
            {
                _dbSet.Attach(entityToDelete);
            }
            _dbSet.Remove(entityToDelete);
        }

        public virtual void DeleteRange(IEnumerable<T> entitiesToDelete)
        {
            foreach (var entityToDelete in entitiesToDelete)
            {
                if (_db.Entry(entityToDelete).State == EntityState.Detached)
                {
                    _dbSet.Attach(entityToDelete);
                }

                _dbSet.Remove(entityToDelete);
            }
        }

        public int ExecuteSqlCommand(string command, params object[] parameters)
        {
            return _db.Database.ExecuteSqlCommand(command, parameters);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) 
                return;
            if (_db == null) 
                return;
            _db.Dispose();
            _db = null;
        }

    }
}