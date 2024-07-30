using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EziOrm
{
    /// <summary>
    ///     mapper uses reflection and custom attributes to provide more control over the mapping process.
    ///     TableAttribute allows specifying a custom table name.
    ///     ColumnAttribute allows specifying custom column names.
    ///     PrimaryKeyAttribute marks the primary key property.
    ///     IgnoreAttribute allows excluding properties from mapping.
    ///     The EntityMapper class now handles these attributes to create a more flexible mapping.
    /// </summary>
    /// <typeparam name="T"></typeparam>


    public class EntityMapper
    {
        // Get Inforamation of Current Entity Mapping 
        public string? TableName { get; }
        public PropertyInfo[] Properties { get; }
        public PropertyInfo PrimaryKey { get; }

        public Dictionary<PropertyInfo , string > ColumnMappings {  get; }




        public EntityMapper(Type entityType)
        {
            //Type type = typeof(entityType);
            TableName = GetTableName(entityType);
            Properties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToArray();
            PrimaryKey = Properties.FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
            ColumnMappings = Properties.ToDictionary(
                p => p,
                p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name
            );
        }

        private string? GetTableName(Type type)
        {
            TableAttribute? tableAttr = type.GetCustomAttribute<TableAttribute>();
            //  in case Not specify table attribute or passing table name get current model (class name )
            return tableAttr?.Name ?? type.Name +"s";   //  Pluraize Name => Convension 
        }
    }


}
