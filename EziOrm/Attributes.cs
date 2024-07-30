using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{

    // Custom Attribute for Mapping Entity Modeles in Db



    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; }
        public TableAttribute(string name) => Name = name;
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public ColumnAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public bool Identity { get; set; }
        public long Seed { get; set; }
        public long Increment { get; set; }

        public PrimaryKeyAttribute(bool identity = true, long seed = 1, long increment = 1)
        {
            Identity = identity;
            Seed = seed;
            Increment = increment;
        }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Property)]
    public class NullableStringAttribute : Attribute { }





}
