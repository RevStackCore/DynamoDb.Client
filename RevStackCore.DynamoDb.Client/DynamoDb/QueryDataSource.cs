using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;

namespace RevStackCore.DynamoDb.Client
{

    public interface IQueryDataSource<T> : IQueryDataSource { }
    public interface IQueryDataSource : IDisposable
    {
        IDataQuery From<T>();
        List<Into> LoadSelect<Into, From>(IDataQuery q);
        int Count(IDataQuery q);

        object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args);
    }

    public abstract class QueryDataSource<T> : IQueryDataSource<T>
    {
        private readonly QueryDataContext context;

        protected QueryDataSource(QueryDataContext context)
        {
            this.context = context;
        }

        public virtual IDataQuery From<TSource>()
        {
            return new DataQuery<TSource>(context);
        }

        public abstract IEnumerable<T> GetDataSource(IDataQuery q);

        public virtual IEnumerable<T> ApplyConditions(IEnumerable<T> data, IEnumerable<DataConditionExpression> conditions)
        {
            var source = data;
            var i = 0;
            foreach (var condition in conditions)
            {
                if (i++ == 0)
                    condition.Term = QueryTerm.And; //First condition always filters

                source = condition.Apply(source, data);
            }

            return source;
        }

        public virtual List<Into> LoadSelect<Into, From>(IDataQuery q)
        {
            var data = GetDataSource(q);
            var source = ApplyConditions(data, q.Conditions);
            source = ApplySorting(source, q.OrderBy);
            source = ApplyLimits(source, q.Offset, q.Rows);

            var to = new List<Into>();

            foreach (var item in source)
            {
                var into = typeof(From) == typeof(Into)
                    ? (Into)(object)item
                    : item.ConvertTo<Into>();

                //ConvertTo<T> short-circuits to instance cast when types match, we to mutate a copy instead
                if (typeof(From) == typeof(Into) && q.OnlyFields != null)
                {
                    into = typeof(Into).CreateInstance<Into>();
                    into.PopulateWith(item);
                }

                to.Add(into);

                if (q.OnlyFields == null)
                    continue;

                foreach (var entry in TypeProperties<Into>.Instance.PropertyMap)
                {
                    var propType = entry.Value;
                    if (q.OnlyFields.Contains(entry.Key))
                        continue;

                    var setter = propType.PublicSetter;
                    if (setter == null)
                        continue;

                    var defaultValue = propType.PropertyInfo.PropertyType.GetDefaultValue();
                    setter(into, defaultValue);
                }
            }

            return to;
        }

        public virtual IEnumerable<T> ApplySorting(IEnumerable<T> source, OrderByExpression orderBy)
        {
            return orderBy != null ? orderBy.Apply(source) : source;
        }

        public virtual IEnumerable<T> ApplyLimits(IEnumerable<T> source, int? skip, int? take)
        {
            if (skip != null)
                source = source.Skip(skip.Value);
            if (take != null)
                source = source.Take(take.Value);
            return source;
        }

        public virtual int Count(IDataQuery q)
        {
            var source = ApplyConditions(GetDataSource(q), q.Conditions);
            return source.Count();
        }

        public virtual object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args)
        {
            name = name?.ToUpper() ?? throw new ArgumentNullException(nameof(name));

            if (name != "COUNT" && name != "MIN" && name != "MAX" && name != "AVG" && name != "SUM"
                && name != "FIRST" && name != "LAST")
                return null;

            var query = (DataQuery<T>)q;

            var argsArray = args.ToArray();
            var firstArg = argsArray.FirstOrDefault();
            string modifier = null;
            if (firstArg != null && firstArg.StartsWithIgnoreCase("DISTINCT "))
            {
                modifier = "DISTINCT";
                firstArg = firstArg.Substring(modifier.Length + 1);
            }

            if (name == "COUNT" && (firstArg == null || firstArg == "*"))
                return Count(q);

            var firstGetter = TypeProperties<T>.Instance.GetPublicGetter(firstArg);
            if (firstGetter == null)
                return null;

            var data = ApplyConditions(GetDataSource(q), query.Conditions);
            if (name == "FIRST" || name == "LAST")
                data = ApplySorting(data, q.OrderBy);

            var source = data.ToArray();

            switch (name)
            {
                case "COUNT":
                    if (modifier == "DISTINCT")
                    {
                        var results = new HashSet<object>();
                        foreach (var item in source)
                        {
                            results.Add(firstGetter(item));
                        }
                        return results.Count;
                    }

                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Add(acc, firstGetter(next)), 0);

                case "MIN":
                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Min(acc, firstGetter(next)));

                case "MAX":
                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Max(acc, firstGetter(next)));

                case "SUM":
                    return CompareTypeUtils.Sum(source.Map(x => firstGetter(x)));

                case "AVG":
                    object sum = CompareTypeUtils.Sum(source.Map(x => firstGetter(x)));
                    var sumDouble = (double)Convert.ChangeType(sum, typeof(double));
                    return sumDouble / source.Length;

                case "FIRST":
                    return source.Length > 0 ? firstGetter(source[0]) : null;

                case "LAST":
                    return source.Length > 0 ? firstGetter(source[source.Length - 1]) : null;
            }

            return null;
        }

        public virtual void Dispose() { }
    }



}
