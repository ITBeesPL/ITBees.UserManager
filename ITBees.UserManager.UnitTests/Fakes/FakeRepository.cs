using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ITBees.Interfaces.Repository;

namespace ITBees.UserManager.UnitTests.Fakes
{
    /// <summary>In-memory stand-in for the read and write repositories; the list plays the table.</summary>
    public class FakeRepository<T> : IReadOnlyRepository<T>, IWriteOnlyRepository<T> where T : class
    {
        public List<T> Items { get; }
        public int UpdateCalls { get; private set; }

        public FakeRepository(params T[] items)
        {
            Items = items.ToList();
        }

        public bool HasData(Expression<Func<T, bool>> predicate) => Items.Any(predicate.Compile());

        public T GetFirst(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
            => Items.FirstOrDefault(predicate.Compile());

        public ICollection<T> GetData(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
            => Items.Where(predicate.Compile()).ToList();

        public PaginatedResult<T> GetDataPaginated(Expression<Func<T, bool>> predicate, int page, int elementsPerPage,
            string sortColumn, SortOrder sortOrder, params Expression<Func<T, object>>[] includeProperties)
            => throw new NotSupportedException();

        public PaginatedResult<T> GetDataPaginated(Expression<Func<T, bool>> predicate, SortOptions sortOptions,
            params Expression<Func<T, object>>[] includeProperties)
            => throw new NotSupportedException();

        public int GetDataCount(Expression<Func<T, bool>> predicate) => Items.Count(predicate.Compile());

        public IQueryable<T> GetDataQueryable(Expression<Func<T, bool>> predicate)
            => Items.AsQueryable().Where(predicate);

        public IQueryable<T> GetDataQueryable(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
            => Items.AsQueryable().Where(predicate);

        public ICollection<T> GetDataFromStoredProcedure(string procedureName, params object[] procedureArgument)
            => throw new NotSupportedException();

        public ICollection<T2> Sql<T2>(string sql) where T2 : class => throw new NotSupportedException();

        public T InsertData(T entity)
        {
            Items.Add(entity);
            return entity;
        }

        public ICollection<T> InsertData(ICollection<T> includeProperty)
        {
            Items.AddRange(includeProperty);
            return includeProperty;
        }

        public ICollection<T> UpdateData(Expression<Func<T, bool>> predicate, Action<T> updateAction,
            params Expression<Func<T, object>>[] includeProperties)
        {
            UpdateCalls++;
            var matched = Items.Where(predicate.Compile()).ToList();
            matched.ForEach(updateAction);
            return matched;
        }

        public int DeleteData(Expression<Func<T, bool>> predicate)
            => Items.RemoveAll(new Predicate<T>(predicate.Compile()));

        public void DeleteData(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
            => Items.RemoveAll(new Predicate<T>(predicate.Compile()));

        void IWriteOnlyRepository<T>.Sql(string sql) => throw new NotSupportedException();
    }
}
