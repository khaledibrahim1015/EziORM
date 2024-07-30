using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    public class DbContext : IDisposable
    {
        // dbset and its object declaration 
        private readonly Dictionary<Type , object > _sets = new Dictionary<Type , object >();

        private readonly ConnectionManager _connectionManager;
        private readonly SchemaManager _schemaManager;
        private readonly RelationshipManager _relationshipManager;

        public ChangeTracker ChangeTracker { get; private set; }

        public DbContext(string connectionString)
        {
            _connectionManager = new ConnectionManager(connectionString);

            _relationshipManager = new RelationshipManager();

            _schemaManager = new SchemaManager(_connectionManager , _relationshipManager);

            ChangeTracker = new ChangeTracker();
            IntializeDbSets();
            ConfigureRelationships();
        }

      

        /// <summary>
        /// IntializeDbSets Method Call When AppDbcontext Construct DbContext then Call DbContext Ctor
        /// So Current instanse will be AppDbContext Not DbContext 
        /// </summary>
        private void IntializeDbSets()
        {
            //  Call GetType To Return The Exact RunTime Type Of The Current Instance 
            var properties = GetType().GetProperties()
                                                  .Where(  prop => prop.PropertyType.IsGenericType 
                                                         && prop.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) );
          
            foreach ( PropertyInfo property in properties )
            {
                var entityType = property.PropertyType.GetGenericArguments()[0];
                // Create New Instance from DbSet<entityType>
                var dbSet = Set(entityType);
                property.SetValue(this, dbSet); //  this refer to current instance from dbcontext => appdbcontext 
            }
        }

        private object Set(Type entityType)
        {
            
            if(!_sets.TryGetValue(entityType, out var set))
            {
                var dbsetTypeObj  = (typeof(DbSet<>)).MakeGenericType(entityType);
                set = Activator.CreateInstance(dbsetTypeObj, this); //  this refer to  parameter context because dbset(appdbcontext dbcontext)
                _sets[entityType] = set;
            }
            return set; 
        }

        public DbSet<TEntity> Set<TEntity>(TEntity entity ) where TEntity : class
        {
            return (DbSet<TEntity>)Set(typeof(TEntity));
        }

        // to call OneToMany  and ManyToMany
        protected virtual void ConfigureRelationships() { }

        protected void OneToMany<TParent, TChild>(
            Expression<Func<TParent , ICollection<TChild>>> navigationProperty,
            Expression<Func<TChild , TParent>> inverseProperty
            )
        {
            _relationshipManager.ConfigureOneToMany(navigationProperty, inverseProperty);
        }
           protected void ManyToMany<T1, T2>(
            Expression<Func<T1, ICollection<T2>>> navigationProperty1,
            Expression<Func<T2, ICollection<T1>>> navigationProperty2,
            string joinTableName)
        {
            _relationshipManager.ConfigureManyToMany(navigationProperty1, navigationProperty2, joinTableName);
        }


        // Ensure create tables succesfully and its relationships 
        public async  Task EnsureCreatedAsync()
            =>   _sets.Keys
                      .ToList()
                        .ForEach( async entityType =>  await _schemaManager.CreateSchemaAsync(entityType));


        internal async Task<DbConnection> GetConnectionAsync()
               => await  _connectionManager.GetConnectionAsync();

        public async Task<int> SaveChangesAsync()
        {
            ChangeTracker.DetectChanges();
            int affectedRows = 0;

            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                foreach (var entry in ChangeTracker.GetEntityEntries())
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            affectedRows += await InsertEntityAsync(connection, transaction, entry.Entity);
                            break;
                        case EntityState.Modified:
                            affectedRows += await UpdateEntityAsync(connection, transaction, entry.Entity);
                            break;
                        case EntityState.Deleted:
                            affectedRows += await DeleteEntityAsync(connection, transaction, entry.Entity);
                            break;
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return affectedRows;
        }

        private async Task<int> InsertEntityAsync(DbConnection connection, DbTransaction transaction, object entity)
        {
            var entityType = entity.GetType();
            var mapper = new EntityMapper(entityType);
            var columns = string.Join(", ", mapper.ColumnMappings.Values);
            var values = string.Join(", ", mapper.ColumnMappings.Keys.Select(p => "@" + p.Name));
            var sql = $"INSERT INTO {mapper.TableName} ({columns}) VALUES ({values})";

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            foreach (var prop in mapper.Properties)
            {
                command.Parameters.Add(new SqlParameter("@" + prop.Name, prop.GetValue(entity) ?? DBNull.Value));
            }

            return await command.ExecuteNonQueryAsync();
        }

        private async Task<int> UpdateEntityAsync(DbConnection connection, DbTransaction transaction, object entity)
        {
            var entityType = entity.GetType();
            var mapper = new EntityMapper(entityType);
            var setClause = string.Join(", ", mapper.ColumnMappings.Select(kvp => $"{kvp.Value} = @{kvp.Key.Name}"));
            var sql = $"UPDATE {mapper.TableName} SET {setClause} WHERE {mapper.ColumnMappings[mapper.PrimaryKey]} = @Id";

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            foreach (var prop in mapper.Properties)
            {
                command.Parameters.Add(new SqlParameter("@" + prop.Name, prop.GetValue(entity) ?? DBNull.Value));
            }

            return await command.ExecuteNonQueryAsync();
        }

        private async Task<int> DeleteEntityAsync(DbConnection connection, DbTransaction transaction, object entity)
        {
            var entityType = entity.GetType();
            var mapper = new EntityMapper(entityType);
            var sql = $"DELETE FROM {mapper.TableName} WHERE {mapper.ColumnMappings[mapper.PrimaryKey]} = @Id";

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            command.Parameters.Add(new SqlParameter("@Id", mapper.PrimaryKey.GetValue(entity)));

            return await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
           _connectionManager.Dispose();
        }
    }
}
