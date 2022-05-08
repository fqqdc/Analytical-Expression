using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticExpression
{
    public class ArithmeticHelper
    {
        public static object? GetMember(object? instance, string memberName)
        {
            if (instance == null) return null;            

            var getProperty = instance.GetType().GetProperty(memberName);
            if (getProperty != null && getProperty.CanRead)
            {
                return getProperty.GetValue(instance);
            }

            var getField = instance.GetType().GetField(memberName);
            if (getField != null)
            {
                return getField.GetValue(instance);
            }

            var dict = instance as IDictionary<string, object?>;
            if (dict != null && dict.TryGetValue(memberName, out object? value))
            {
                return value;
            }

            return null;
        }

        public static object? GetIndex(object? instance, double index)
        {
            if (instance == null) return null;

            return GetIndex(instance, (int)index);
        }

        public static object? GetIndex(object? instance, int index)
        {
            if (instance == null) return null;
            var e = instance as IEnumerable;
            if (e != null) return e.Cast<object>().ElementAtOrDefault(index);

            return null;
        }

        public static double ParseToNumber(object? instance)
        {
            if (instance == null) return 0;

            if (double.TryParse(instance.ToString(), out var d))
                return d;

            return 0;

        }
    }
}
