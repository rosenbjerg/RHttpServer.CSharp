using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite;

namespace RHttpServer.Plugins.External
{
    public class SimpleSQLiteDatatase : RPlugin, IDisposable
    {
        private readonly SQLiteConnection _dbCon;
        private readonly object _dbLock = new object();

        public SimpleSQLiteDatatase(string dbPath)
        {
            _dbCon = new SQLiteConnection(dbPath);
        }

        /// <summary>
        /// Returns a queryable interface to the table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetTable<T>()
            where T : new()
        {
            lock (_dbLock)
            {
                return _dbCon.Table<T>();
            }
        }

        /// <summary>
        /// Attemps to retrieve the object using the primary key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public T Get<T>(object primaryKey)
            where T : new()
        {
            lock (_dbLock)
            {
                return _dbCon.Get<T>(primaryKey);
            }
        }

        /// <summary>
        /// Returns the first object that satisfies predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public T FindOne<T>(Expression<Func<T, bool>> predicate)
            where T : new()
        {
            lock (_dbLock)
            {
                return _dbCon.Find(predicate);
            }
        }

        /// <summary>
        /// Returns all objects that satisfies predicate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<T> Find<T>(Expression<Func<T, bool>> predicate)
            where T : new()
        {
            lock (_dbLock)
            {
                return _dbCon.Table<T>().Where(predicate);
            }
        }

        /// <summary>
        /// Inserts all input objects atomically
        /// </summary>
        /// <returns>Number of changes to table</returns>
        public int Insert<T>(params T[] toBeInserted)
        {
            lock (_dbLock)
            {
                return _dbCon.InsertAll(toBeInserted, typeof(T));
            }
        }

        /// <summary>
        /// Updates all input objects atomically
        /// </summary>
        /// <returns>Number of changes to table</returns>
        public int Update<T>(params T[] toBeUpdated)
        {
            lock (_dbLock)
            {
                return _dbCon.UpdateAll(toBeUpdated);
            }
        }

        /// <summary>
        /// Deletes all input objects atomically
        /// </summary>
        /// <returns>Number of changes to table</returns>
        public int Delete<T>(params object[] primaryKeys)
        {
            var ret = 0;
            lock (_dbLock)
            {
                foreach (var pk in primaryKeys)
                {
                    ret += _dbCon.Delete<T>(pk);
                }
            }
            return ret;
        }

        /// <summary>
        /// Creates a table for the type, if not already exists
        /// </summary>
        public void CreateTable<T>()
        {
            lock (_dbLock)
            {
                _dbCon.CreateTable<T>();
            }
        }

        /// <summary>
        /// Drops the table for a specified type, if exists
        /// </summary>
        public void DropTable<T>()
        {
            lock (_dbLock)
            {
                _dbCon.DropTable<T>();
            }
        }

        public void Dispose()
        {
            lock (_dbLock)
            {
                _dbCon.Close();
            }
        }
    }
}
