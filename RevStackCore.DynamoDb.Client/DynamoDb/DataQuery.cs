using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RevStackCore.DataAnnotations;
using ServiceStack;

namespace RevStackCore.DynamoDb.Client
{
    public class DataQuery<T> : IDataQuery
    {
        private static PropertyInfo PrimaryKey;

        private QueryDataContext context;

        public IQueryData Dto { get; private set; }
        public Dictionary<string, string> DynamicParams { get; private set; }
        public List<DataConditionExpression> Conditions { get; set; }
        public OrderByExpression OrderBy { get; set; }
        public HashSet<string> OnlyFields { get; set; }
        public int? Offset { get; set; }
        public int? Rows { get; set; }

        static DataQuery()
        {
            var pis = TypeProperties<T>.Instance.PublicPropertyInfos;
            PrimaryKey = pis.FirstOrDefault(x => x.HasAttribute<PrimaryKeyAttribute>())
                ?? pis.FirstOrDefault(x => x.HasAttribute<AutoIncrementAttribute>())
                ?? pis.FirstOrDefault(x => x.Name == IdUtils.IdField)
                ?? pis.FirstOrDefault();
        }

        public DataQuery(QueryDataContext context)
        {
            this.context = context;
            this.Dto = context.Dto;
            this.DynamicParams = context.DynamicParams;
            this.Conditions = new List<DataConditionExpression>();
        }

        public virtual bool HasConditions => Conditions.Count > 0;

        public virtual void Limit(int? skip, int? take)
        {
            this.Offset = skip;
            this.Rows = take;
        }

        public void Take(int take)
        {
            this.Rows = take;
        }

        public virtual void Select(string[] fields)
        {
            this.OnlyFields = fields == null || fields.Length == 0
                ? null //All Fields
                : new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
        }

        public virtual Tuple<Type, PropertyInfo> FirstMatchingField(string field)
        {
            var pi = typeof(T).GetProperties()
                .FirstOrDefault(x => string.Equals(x.Name, field, StringComparison.OrdinalIgnoreCase));
            return pi != null
                ? Tuple.Create(typeof(T), pi)
                : null;
        }

        public virtual void OrderByFields(params string[] fieldNames)
        {
            OrderByFieldsImpl(fieldNames, x => x[0] != '-');
        }

        public virtual void OrderByFieldsDescending(params string[] fieldNames)
        {
            OrderByFieldsImpl(fieldNames, x => x[0] == '-');
        }

        void OrderByFieldsImpl(string[] fieldNames, Func<string, bool> orderFn)
        {
            var getters = new List<GetMemberDelegate>();
            var orderAscs = new List<bool>();
            var fields = new List<string>();

            foreach (var fieldName in fieldNames)
            {
                if (string.IsNullOrEmpty(fieldName))
                    continue;

                var getter = TypeProperties<T>.Instance.GetPublicGetter(fieldName.TrimStart('-'));
                if (getter == null)
                    continue;

                var orderAsc = orderFn(fieldName);

                fields.Add(fieldName);
                getters.Add(getter);
                orderAscs.Add(orderAsc);
            }

            if (getters.Count > 0)
            {
                OrderBy = new OrderByExpression(fields.ToArray(), getters.ToArray(), orderAscs.ToArray());
            }
        }

        public virtual void OrderByPrimaryKey()
        {
            OrderBy = new OrderByExpression(PrimaryKey.Name, TypeProperties<T>.Instance.GetPublicGetter(PrimaryKey));
        }

        public virtual void Join(Type joinType, Type type)
        {
        }

        public virtual void LeftJoin(Type joinType, Type type)
        {
        }

        public virtual void AddCondition(QueryTerm term, PropertyInfo field, QueryCondition condition, object value)
        {
            this.Conditions.Add(new DataConditionExpression
            {
                Term = term,
                Field = field,
                FieldGetter = TypeProperties<T>.Instance.GetPublicGetter(field),
                QueryCondition = condition,
                Value = value,
            });
        }

        public virtual void And(string field, QueryCondition condition, string value)
        {
            AddCondition(QueryTerm.And, TypeProperties<T>.Instance.GetPublicProperty(field), condition, value);
        }

        public virtual void Or(string field, QueryCondition condition, string value)
        {
            AddCondition(QueryTerm.Or, TypeProperties<T>.Instance.GetPublicProperty(field), condition, value);
        }
    }
}
