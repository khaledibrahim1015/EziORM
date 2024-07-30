using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EziOrm
{
    /// <summary>
    /// This change tracker maintains a dictionary of tracked entities and their states.
    //  The EntityEntry class captures the original values of an entity to detect changes.
    //  DetectChanges method checks for modifications in unchanged entities.
    //  This implementation allows for automatic change detection and more granular control over entity states.
    /// </summary>
    public class ChangeTracker
    {
        /// <summary>
        /// on level of all entities store entity and its entityentry 
        /// </summary>
        private readonly Dictionary<object, EntityEntry> _trackedEntities = new Dictionary<object, EntityEntry>();

        public void TrackEntity(object entity, EntityState state)
        {

            if (!_trackedEntities.TryGetValue(entity, out var enityEntry))
            {
                enityEntry = new EntityEntry(entity);
                _trackedEntities[entity] = enityEntry;
            }
            enityEntry.State = state;
        }

        public EntityState GetEntityState(object entity)
            => _trackedEntities.TryGetValue(entity, out var enityEntry) ? enityEntry.State : EntityState.Unchanged;

        public IEnumerable<EntityEntry> GetEntityEntries() => _trackedEntities.Values;

        public void DetectChanges()
        {
            foreach (EntityEntry entityEntry in _trackedEntities.Values)
            {
                if (entityEntry.State == EntityState.Unchanged )
                    if (entityEntry.HasChanges())
                        entityEntry.State = EntityState.Modified;
            }
        }


    }
}
