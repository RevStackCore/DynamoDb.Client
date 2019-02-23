using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack;

namespace RevStackCore.DynamoDb.Client
{
    public interface IMeta
    {
        Dictionary<string, string> Meta { get; set; }
    }

    public interface IQuery : IMeta
    {
        /// <summary>
        /// How many results to skip
        /// </summary>
        int? Skip { get; set; }

        /// <summary>
        /// How many results to return
        /// </summary>
        int? Take { get; set; }

        /// <summary>
        /// List of fields to sort by, can order by multiple fields and inverse order, e.g: Id,-Amount
        /// </summary>
        string OrderBy { get; set; }

        /// <summary>
        /// List of fields to sort by descending, can order by multiple fields and inverse order, e.g: -Id,Amount
        /// </summary>
        string OrderByDesc { get; set; }

        /// <summary>
        /// Include aggregate data like Total, COUNT(*), COUNT(DISTINCT Field), Sum(Amount), etc
        /// </summary>
        string Include { get; set; }

        /// <summary>
        /// The fields to return
        /// </summary>
        string Fields { get; set; }
    }

    public interface IQueryData : IQuery { }

    public interface IDataQuery
    {
        IQueryData Dto { get; }
        Dictionary<string, string> DynamicParams { get; }
        List<DataConditionExpression> Conditions { get; }
        OrderByExpression OrderBy { get; }
        HashSet<string> OnlyFields { get; }
        int? Offset { get; }
        int? Rows { get; }
        bool HasConditions { get; }

        Tuple<Type, PropertyInfo> FirstMatchingField(string name);

        void Select(string[] fields);
        void Join(Type joinType, Type type);
        void LeftJoin(Type joinType, Type type);
        void And(string field, QueryCondition condition, string value);
        void Or(string field, QueryCondition condition, string value);
        void AddCondition(QueryTerm defaultTerm, PropertyInfo field, QueryCondition condition, object value);
        void OrderByFields(string[] fieldNames);
        void OrderByFieldsDescending(string[] fieldNames);
        void OrderByPrimaryKey();
        void Limit(int? skip, int? take);
    }

    public abstract class FilterExpression
    {
        public abstract IEnumerable<T> Apply<T>(IEnumerable<T> source);
    }

    public class OrderByExpression : FilterExpression
    {
        public string[] FieldNames { get; private set; }
        public GetMemberDelegate[] FieldGetters { get; private set; }
        public bool[] OrderAsc { get; private set; }

        public OrderByExpression(string fieldName, GetMemberDelegate fieldGetter, bool orderAsc = true)
            : this(new[] { fieldName }, new[] { fieldGetter }, new[] { orderAsc }) { }

        public OrderByExpression(string[] fieldNames, GetMemberDelegate[] fieldGetters, bool[] orderAsc)
        {
            this.FieldNames = fieldNames;
            this.FieldGetters = fieldGetters;
            this.OrderAsc = orderAsc;
        }

        class OrderByComparator<T> : IComparer<T>
        {
            readonly GetMemberDelegate[] getters;
            readonly bool[] orderAsc;

            public OrderByComparator(GetMemberDelegate[] getters, bool[] orderAsc)
            {
                this.getters = getters;
                this.orderAsc = orderAsc;
            }

            public int Compare(T x, T y)
            {
                for (int i = 0; i < getters.Length; i++)
                {
                    var getter = getters[i];
                    var xVal = getter(x);
                    var yVal = getter(y);
                    var cmp = CompareTypeUtils.CompareTo(xVal, yVal);
                    if (cmp != 0)
                        return orderAsc[i] ? cmp : cmp * -1;
                }

                return 0;
            }
        }

        public override IEnumerable<T> Apply<T>(IEnumerable<T> source)
        {
            var to = source.ToList();
            to.Sort(new OrderByComparator<T>(FieldGetters, OrderAsc));
            return to;
        }
    }

    public class DataConditionExpression
    {
        public QueryTerm Term { get; set; }
        public QueryCondition QueryCondition { get; set; }
        public PropertyInfo Field { get; set; }
        public GetMemberDelegate FieldGetter { get; set; }
        public object Value { get; set; }

        public object GetFieldValue(object instance)
        {
            if (Field == null || FieldGetter == null)
                return null;

            return FieldGetter(instance);
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> source, IEnumerable<T> original)
        {
            if (Term != QueryTerm.Or)
            {
                var to = new List<T>();
                foreach (var item in source)
                {
                    var fieldValue = GetFieldValue(item);
                    if (QueryCondition.Match(fieldValue, Value))
                        to.Add(item);
                }
                return to;
            }
            else
            {
                var to = new List<T>(source);
                foreach (var item in original)
                {
                    var fieldValue = GetFieldValue(item);
                    if (QueryCondition.Match(fieldValue, Value) && !to.Contains(item))
                        to.Add(item);
                }
                return to;
            }
        }
    }
}
