using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    public class DbSet<TEntity> : IQueryable<TEntity> where TEntity : class
    {
        private readonly DbContext _context;
        private readonly QueryBuilder<TEntity> _queryBuilder;
        private readonly EntityMapper _entityMapper;

        public Expression Expression { get; private set; }
        public Type ElementType => typeof(TEntity);
        public IQueryProvider Provider { get; }

        internal DbSet(DbContext context)
        {
            _context = context;
            _queryBuilder = new QueryBuilder<TEntity>();
            _entityMapper = new EntityMapper(typeof(TEntity));
            Expression = Expression.Constant(this);
            Provider = new CustomQueryProvider(this);
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<TEntity>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            _queryBuilder.Where(predicate);
            return new DbSet<TEntity>(_context)
            {
                Expression = Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new[] { typeof(TEntity) },
                    Expression,
                    Expression.Quote(predicate))
            };
        }

        public IQueryable<TEntity> OrderBy(Expression<Func<TEntity, object>> keySelector)
        {
            _queryBuilder.OrderBy(keySelector);
            return new DbSet<TEntity>(_context)
            {
                Expression = Expression.Call(
                    typeof(Queryable),
                    "OrderBy",
                    new[] { typeof(TEntity), typeof(object) },
                    Expression,
                    Expression.Quote(keySelector))
            };
        }

        public IQueryable<TEntity> OrderByDescending(Expression<Func<TEntity, object>> keySelector)
        {
            _queryBuilder.OrderByDesending(keySelector);
            return new DbSet<TEntity>(_context)
            {
                Expression = Expression.Call(
                    typeof(Queryable),
                    "OrderByDescending",
                    new[] { typeof(TEntity), typeof(object) },
                    Expression,
                    Expression.Quote(keySelector))
            };
        }

        public IQueryable<TEntity> Skip(int count)
        {
            _queryBuilder.Skip(count);
            return new DbSet<TEntity>(_context)
            {
                Expression = Expression.Call(
                    typeof(Queryable),
                    "Skip",
                    new[] { typeof(TEntity) },
                    Expression,
                    Expression.Constant(count))
            };
        }

        public IQueryable<TEntity> Take(int count)
        {
            _queryBuilder.Take(count);
            return new DbSet<TEntity>(_context)
            {
                Expression = Expression.Call(
                    typeof(Queryable),
                    "Take",
                    new[] { typeof(TEntity) },
                    Expression,
                    Expression.Constant(count))
            };
        }
        public void Add(TEntity entity)
        {
            _context.ChangeTracker.TrackEntity(entity, EntityState.Added);
        }

        public void Update(TEntity entity)
        {
            _context.ChangeTracker.TrackEntity(entity, EntityState.Modified);
        }

        public void Remove(TEntity entity)
        {
            _context.ChangeTracker.TrackEntity(entity, EntityState.Deleted);
        }

        public async Task<List<TEntity>> ToListAsync()
        {
            string sql = _queryBuilder.BuildQuery();
            using var connection = await _context.GetConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var results = new List<TEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapReaderToEntity(reader));
            }
            return results;
        }

        private TEntity MapReaderToEntity(DbDataReader reader)
        {
            var entity = Activator.CreateInstance<TEntity>();
            foreach (var prop in _entityMapper.Properties)
            {
                var columnName = _entityMapper.ColumnMappings[prop];
                if (!reader.IsDBNull(reader.GetOrdinal(columnName)))
                {
                    prop.SetValue(entity, reader[columnName]);
                }
            }
            return entity;
        }

        private class CustomQueryProvider : IQueryProvider
        {
            private readonly DbSet<TEntity> _dbSet;

            public CustomQueryProvider(DbSet<TEntity> dbSet)
            {
                _dbSet = dbSet;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new DbSet<TEntity>(_dbSet._context) { Expression = expression };
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return (IQueryable<TElement>)CreateQuery(expression);
            }

            public object Execute(Expression expression)
            {
                return Execute<TEntity>(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                // This is where you would implement the actual query execution
                // For now, we'll just call ToListAsync and return the result
                var task = _dbSet.ToListAsync();
                task.Wait();
                return (TResult)(object)task.Result;
            }
        }
    }
}