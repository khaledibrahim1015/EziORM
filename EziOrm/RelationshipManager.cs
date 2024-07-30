using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    public class RelationshipManager
    {
        private readonly Dictionary<Type, List<RelationshipMetadata>> _relationships = new Dictionary<Type, List<RelationshipMetadata>>();

        public void ConfigureOneToMany<TParent, TChild>(
            Expression<Func<TParent, ICollection<TChild>>> navigationProperty,
            Expression<Func<TChild, TParent>> inverseProperty)
        {
            var parentMapper = new EntityMapper(typeof(TParent));
            var childMapper = new EntityMapper(typeof(TChild));

            var metadata = new RelationshipMetadata
            {
                RelationshipType = RelationshipType.OneToMany,
                ParentType = typeof(TParent),
                ChildType = typeof(TChild),
                ParentTableName = parentMapper.TableName,
                ChildTableName = childMapper.TableName,
                ParentNavigationProperty = GetPropertyInfo(navigationProperty),
                ChildNavigationProperty = GetPropertyInfo(inverseProperty)
            };

            AddRelationship(typeof(TParent), metadata);
            AddRelationship(typeof(TChild), metadata);
        }

        public void ConfigureManyToMany<T1, T2>(
            Expression<Func<T1, ICollection<T2>>> navigationProperty1,
            Expression<Func<T2, ICollection<T1>>> navigationProperty2,
            string joinTableName)
        {
            var mapper1 = new EntityMapper(typeof(T1));
            var mapper2 = new EntityMapper(typeof(T2));

            var metadata = new RelationshipMetadata
            {
                RelationshipType = RelationshipType.ManyToMany,
                ParentType = typeof(T1),
                ChildType = typeof(T2),
                ParentTableName = mapper1.TableName,
                ChildTableName = mapper2.TableName,
                ParentNavigationProperty = GetPropertyInfo(navigationProperty1),
                ChildNavigationProperty = GetPropertyInfo(navigationProperty2),
                JoinTableName = joinTableName
            };

            AddRelationship(typeof(T1), metadata);
            AddRelationship(typeof(T2), metadata);
        }

        private PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Expression is not a member access", nameof(propertyLambda));

            return member.Member as PropertyInfo;
        }
        private void AddRelationship(Type type, RelationshipMetadata metadata)
        {
            if (!_relationships.ContainsKey(type))
            {
                _relationships[type] = new List<RelationshipMetadata>();
            }
            _relationships[type].Add(metadata);
        }

        public IEnumerable<RelationshipMetadata> GetRelationships(Type type)
        {
            return _relationships.TryGetValue(type, out var relationships) ? relationships : Enumerable.Empty<RelationshipMetadata>();
        }
        public IEnumerable<RelationshipMetadata> GetAllRelationships()
        {
            return _relationships.Values.SelectMany(r => r);
        }
    }

    public class RelationshipMetadata
    {
        public RelationshipType RelationshipType { get; set; }
        public Type ParentType { get; set; }
        public Type ChildType { get; set; }
        public string ParentTableName { get; set; }
        public string ChildTableName { get; set; }
        public PropertyInfo ParentNavigationProperty { get; set; }
        public PropertyInfo ChildNavigationProperty { get; set; }
        public string JoinTableName { get; set; }
    }

    public enum RelationshipType
    {
        OneToMany,
        ManyToMany
    }

}
