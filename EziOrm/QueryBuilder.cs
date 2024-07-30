using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EziOrm
{
    /// <summary>
    /// This query builder uses expression trees to allow for type-safe queries.
    /// It supports complex where clauses, ordering, pagination, and more.
    /// The TranslateExpression method converts C# expressions into SQL. This is a simplified version; a full ORM would need a more comprehensive expression visitor.
    /// This approach allows for queries like queryBuilder.Where(u => u.Age > 18).OrderBy(u => u.Name).Take(10).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public  class QueryBuilder<T>  where T : class 
    {
        private readonly List<Expression<Func<T, object>>> _orderByExpressions = new();
        private readonly List<Expression<Func<T, bool>>> _whereExpressions = new();
        private bool _isDescending = false;
        private int? _take;
        private int? _skip;

      

        public  QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            _whereExpressions.Add(predicate);
            return this;
        }

        public QueryBuilder<T> OrderBy(Expression<Func<T, object >> keySelector)
        {
            _orderByExpressions.Add(keySelector);
            _isDescending = false;
            return this;
        }
        public QueryBuilder<T> OrderByDesending(Expression<Func<T, object>> keySelector)
        {
            _orderByExpressions.Add(keySelector);
            _isDescending = true;
            return this;
        }
        public  QueryBuilder<T>Skip(int count)
        {
            _skip = count;
            return this;
        }
        public QueryBuilder<T> Take(int count)
        {
            _take = count;
            return this;
        }

        public string BuildQuery()
        {
            StringBuilder query =  new StringBuilder();
            query.Append($"Select *  from {new EntityMapper(typeof(T)).TableName}");

            if (_whereExpressions.Any())
                query.Append(" WHERE " + string.Join(" AND ", _whereExpressions.Select(TranslateExpression)));
            
            if(_orderByExpressions.Any())
            {
                query.Append(" ORDER BY " + string.Join(" , ", _orderByExpressions.Select(TranslateExpression)));
                if (_isDescending) query.Append( " DESC");
            }

            if (_skip.HasValue)
                query.Append( $" OFFSET {_skip.Value} ROWS ");

            if (_take.HasValue)
                query.Append($" FETCH NEXT {_take.Value} ROWS ONLY");

            return query.ToString() ;




        }

        private string TranslateExpression(Expression expression)
        {
            // This is a simplified translation. A real ORM would need a more comprehensive expression visitor.
            if (expression is LambdaExpression lambda)
            {
                return TranslateExpression(lambda.Body);
            }
            else if (expression is BinaryExpression binary)
            {
                return $"{TranslateExpression(binary.Left)} {GetOperator(binary.NodeType)} {TranslateExpression(binary.Right)}";
            }
            //else if (expression is MemberExpression member)
            //{
            //    return _mapper.ColumnMappings[member.Member as PropertyInfo];
            //}
            else if (expression is MemberExpression member)
            {
                return member.Member.Name;
            }
            else if (expression is ConstantExpression constant)
            {
                return constant.Value.ToString();
            }

            throw new NotSupportedException($"Expression type not supported: {expression.GetType()}");
        }

        private string GetOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "!=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                default: throw new NotSupportedException($"Operator not supported: {nodeType}");
            }
        }
    }
}
