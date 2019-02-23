using System;
using System.Collections;
using ServiceStack;
using ServiceStack.Text;

namespace RevStackCore.DynamoDb.Client
{
    public static class CompareTypeUtils
    {
        public static int CompareTo(object a, object b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null
                    ? 0
                    : a == null //NULL is lowest in RDBMS
                        ? 1
                        : -1;
            }

            if (a.GetType() == b.GetType())
            {
                if (a is IComparable ac)
                    return ac.CompareTo(b);
            }

            var aLong = CoerceLong(a);
            if (aLong != null)
            {
                var bLong = CoerceLong(b);
                if (bLong != null)
                    return aLong.Value.CompareTo(bLong.Value);
            }

            var aDouble = CoerceDouble(a);
            if (aDouble != null)
            {
                var bDouble = CoerceDouble(b);
                if (bDouble != null)
                    return aDouble.Value.CompareTo(bDouble.Value);
            }

            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return string.Compare(aString, bString, StringComparison.Ordinal);
        }

        public static long? CoerceLong(object o)
        {
            return (long?)(o.GetType().IsIntegerType()
                ? Convert.ChangeType(o, typeof(long))
                : null);
        }

        public static double? CoerceDouble(object o)
        {
            return (long?)(o.GetType().IsRealNumberType()
                ? Convert.ChangeType(o, typeof(double))
                : null);
        }

        public static string CoerceString(object o)
        {
            return TypeSerializer.SerializeToString(o);
        }

        public static object Add(object a, object b)
        {
            var aLong = CoerceLong(a);
            if (aLong != null)
            {
                var bLong = CoerceLong(b);
                return aLong + bLong ?? aLong;
            }

            var aDouble = CoerceDouble(a);
            if (aDouble != null)
            {
                var bDouble = CoerceDouble(b);
                return aDouble + bDouble ?? aDouble;
            }

            var aString = CoerceString(a);
            var bString = CoerceString(b);
            return aString + bString;
        }

        public static object Min(object a, object b)
        {
            if (a == null)
                return b;

            return CompareTo(a, b) > 0 ? b : a;
        }

        public static object Max(object a, object b)
        {
            if (a == null)
                return b;

            return CompareTo(a, b) < 0 ? b : a;
        }

        public static object Sum(IEnumerable values)
        {
            object sum = null;
            foreach (var value in values)
            {
                sum = sum == null
                    ? value
                    : Add(sum, value);
            }
            return sum;
        }

        public static object Aggregate(IEnumerable source, Func<object, object, object> fn, object seed = null)
        {
            var acc = seed;
            foreach (var item in source)
            {
                acc = fn(acc, item);
            }
            return acc;
        }
    }
}
