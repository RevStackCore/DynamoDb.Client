using System;
namespace RevStackCore.DynamoDb.Client
{

    public enum QueryTerm
    {
        Default = 0,
        And = 1,
        Or = 2,
    }

    public abstract class QueryCondition
    {
        public abstract string Alias { get; }

        public QueryTerm Term { get; set; }

        public abstract bool Match(object a, object b);

        public virtual int CompareTo(object a, object b)
        {
            return CompareTypeUtils.CompareTo(a, b);
        }
    }
}
