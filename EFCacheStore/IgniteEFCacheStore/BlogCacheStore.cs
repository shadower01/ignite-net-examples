﻿using System;
using System.Collections;
using System.Data.Entity.Migrations;
using System.Linq;
using Apache.Ignite.Core.Cache.Store;
using Apache.Ignite.Core.Common;

namespace IgniteEFCacheStore
{
    /// <summary>
    /// Ignite Cache Store for <see cref="Blog"/> entities.
    /// </summary>
    public class BlogCacheStore : ICacheStore
    {
        public void LoadCache(Action<object, object> act, params object[] args)
        {
            Console.WriteLine("{0}.LoadCache() called.", GetType().Name);

            // Load everything from DB to Ignite
            using (var ctx = GetDbContext())
            {
                foreach (var blog in ctx.Blogs)
                {
                    act(blog.BlogId, blog);
                }
            }
        }

        public object Load(object key)
        {
            Console.WriteLine("{0}.Load({1}) called.", GetType().Name, key);

            using (var ctx = GetDbContext())
            {
                return ctx.Blogs.Find(key);
            }
        }

        public IDictionary LoadAll(ICollection keys)
        {
            using (var ctx = GetDbContext())
            {
                return keys.Cast<int>().ToDictionary(key => key, key => ctx.Blogs.Find(key));
            }
        }

        public void Write(object key, object val)
        {
            Console.WriteLine("{0}.Write({1}, {2}) called.", GetType().Name, key, val);

            using (var ctx = GetDbContext())
            {
                ctx.Blogs.AddOrUpdate((Blog) val);

                ctx.SaveChanges();
            }
        }

        public void WriteAll(IDictionary entries)
        {
            using (var ctx = GetDbContext())
            {
                foreach (var blog in entries.Values.OfType<Blog>())
                {
                    ctx.Blogs.AddOrUpdate(blog);

                    ctx.SaveChanges();
                }
            }
        }

        public void Delete(object key)
        {
            Console.WriteLine("{0}.Delete({1}) called.", GetType().Name, key);

            using (var ctx = GetDbContext())
            {
                var blog = ctx.Blogs.Find(key);

                if (blog != null)
                {
                    ctx.Blogs.Remove(blog);

                    ctx.SaveChanges();
                }
            }
        }

        public void DeleteAll(ICollection keys)
        {
            foreach (var key in keys)
            {
                Delete(key);
            }
        }

        public void SessionEnd(bool commit)
        {
            // No-op.
        }

        private static BloggingContext GetDbContext()
        {
            return new BloggingContext
            {
                Configuration =
                {
                    // Disable EF proxies so that Ignite serialization works.
                    // https://apacheignite-net.readme.io/docs/serialization#using-entity-framework-pocos
                    ProxyCreationEnabled = false
                }
            };
        }
    }

    [Serializable]
    public class BlogCacheStoreFactory : IFactory<ICacheStore>
    {
        public ICacheStore CreateInstance()
        {
            return new BlogCacheStore();
        }
    }
}
