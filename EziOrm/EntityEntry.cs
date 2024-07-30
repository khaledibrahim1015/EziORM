using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    /// <summary>
    /// The EntityEntry class captures the original values of an entity to detect changes.
    /// </summary>
    public class EntityEntry
    {
        public object Entity { get; }
        public EntityState State { get; set; }

        /// <summary>
        ///  on level of one entity to store prop name and its value 
        /// </summary>
        private readonly Dictionary<string, object?> _originalValues = new Dictionary<string, object?>();


        public EntityEntry(object entity)
        {
            Entity = entity;
            State = EntityState.Unchanged;
            _originalValues = Entity
                                    .GetType().GetProperties()
                                    .ToDictionary(prop => prop.Name, prop => prop.GetValue(entity));
        }

        public bool HasChanges()
        {

            return Entity.GetType().GetProperties().Any(prop =>
            {

                var originalValue = _originalValues[prop.Name];
                var currentValue = prop.GetValue(Entity);
                return !Equals(currentValue, originalValue);

            }
            );
        }






    }
}
