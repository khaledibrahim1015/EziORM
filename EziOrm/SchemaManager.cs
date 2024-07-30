using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EziOrm
{


    /// <summary>
    /// This schema manager can automatically create tables based on entity definitions.
    ///  It maps C# types to SQL types and handles constraints like primary keys and nullability.
    ///    The CreateSchemaAsync method checks if the table exists before creating it, preventing errors on subsequent runs.
    /// </summary>
    public class SchemaManager
    {

        private readonly ConnectionManager _connectionManager;

        private readonly RelationshipManager _relationshipManager;
        public SchemaManager(ConnectionManager connectionManager, RelationshipManager relationshipManager)
        {
            _connectionManager = connectionManager;
            _relationshipManager = relationshipManager;
        }

        public async Task CreateSchemaAsync(Type entityType)
        {
            var mapperType = typeof(EntityMapper).MakeGenericType(entityType);
            var mapper = Activator.CreateInstance(mapperType) as dynamic;

            var columns = ((IEnumerable<PropertyInfo>)mapper.Properties).Select(p =>
                $"{mapper.ColumnMappings[p]} {GetSqlType(p)} {GetConstraints(p)}");

            var createTableSql = $@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{mapper.TableName}' AND xtype='U')
            CREATE TABLE {mapper.TableName} (
                {string.Join(",\n", columns)}
            )";


            using DbConnection connection = await _connectionManager.GetConnectionAsync();
            using DbCommand command = connection.CreateCommand(); command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync();

            // Apply relationships
            await ApplyRelationshipsAsync(entityType);
        }
        private async Task ApplyRelationshipsAsync(Type entityType)
        {
            var relationships = _relationshipManager.GetRelationships(entityType);

            foreach (var relationship in relationships)
            {
                if (relationship.RelationshipType == RelationshipType.OneToMany)
                {
                    await CreateForeignKeyAsync(
                        relationship.ParentTableName,
                        relationship.ChildTableName,
                        relationship.ParentNavigationProperty.Name + "Id"
                    );
                }
                else if (relationship.RelationshipType == RelationshipType.ManyToMany)
                {
                    await CreateJoinTableAsync(
                        relationship.JoinTableName,
                        relationship.ParentTableName + "Id",
                        relationship.ChildTableName + "Id"
                    );
                }
            }
        }

        private async Task CreateForeignKeyAsync(string parentTable, string childTable, string foreignKeyColumn)
        {
            var sql = $@"
            IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'FK_{childTable}_{parentTable}') AND parent_object_id = OBJECT_ID(N'{childTable}'))
            ALTER TABLE {childTable}
            ADD CONSTRAINT FK_{childTable}_{parentTable} FOREIGN KEY ({foreignKeyColumn})
            REFERENCES {parentTable}(Id)";

            using DbConnection connection = await _connectionManager.GetConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateJoinTableAsync(string joinTableName, string column1, string column2)
        {
            var sql = $@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{joinTableName}' AND xtype='U')
            CREATE TABLE {joinTableName} (
                {column1} INT NOT NULL,
                {column2} INT NOT NULL,
                PRIMARY KEY ({column1}, {column2})
            )";

            using DbConnection connection = await _connectionManager.GetConnectionAsync();
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }







        // Notes 
        //    Nullable.GetUnderlyingType() only works for nullable value types(like int?, double?, etc.), not for reference types.
        //  In C# 8.0 and later, with nullable reference types enabled, string? is not actually a nullable type in the same way as int?. It's still a reference type, just with a nullable annotation.
        // Therefore, Nullable.GetUnderlyingType() returns null for string?, and your Where clause filters out all properties.
        // To correctly identify nullable properties in this scenario, you need to check for both nullable value types and nullable reference types. Here's a corrected version:

        private string GetSqlType(PropertyInfo property)
        {
            Type type;
            bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null ||
                              property.PropertyType == typeof(string) && property.GetCustomAttribute<NullableStringAttribute>() != null;

            type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            string sqlType = Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => "BIT",
                TypeCode.Byte => "TINYINT",
                TypeCode.Int16 => "SMALLINT",
                TypeCode.Int32 => "INT",
                TypeCode.Int64 => "BIGINT",
                TypeCode.Single => "REAL",
                TypeCode.Double => "FLOAT",
                TypeCode.Decimal => "DECIMAL(18,2)",
                TypeCode.DateTime => "DATETIME2",
                TypeCode.String => "NVARCHAR(MAX)",
                _ => throw new NotSupportedException($"Type {type} is not supported.")
            };

            return isNullable ? sqlType : sqlType;
        }

        private string GetConstraints(PropertyInfo property)
        {
            StringBuilder constraints = new();

            var primaryKeyAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
            if (primaryKeyAttr != null)
            {
                constraints.Append(" PRIMARY KEY ");

                if (primaryKeyAttr.Identity)
                {
                    if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long))
                    {
                        constraints.Append(" IDENTITY");

                        if (primaryKeyAttr.Seed != 1 || primaryKeyAttr.Increment != 1)
                        {
                            constraints.Append($"({primaryKeyAttr.Seed},{primaryKeyAttr.Increment})");
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("IDENTITY can only be specified on int or long properties.");
                    }
                }

            }

            if (!property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                constraints.Append(" NULL ");
            else
                constraints.Append(" NOT NULL ");

            return constraints.ToString();

        }


    }

}
