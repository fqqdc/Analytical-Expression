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

            var dict = instance as IDictionary<string, object?>;
            if (dict != null && dict.TryGetValue(memberName, out object? value))
            {
                return value;
            }

            var getProperty = instance.GetType().GetProperty(memberName);
            if (getProperty != null)
            {
                return getProperty.GetValue(instance);
            }

            var getField = instance.GetType().GetField(memberName);
            if (getField != null)
            {
                return getField.GetValue(instance);
            }

            return null;
        }

        public static object? GetIndex(object? instance, object? key)
        {
            if (instance == null || key == null) return null;

            string? keyString = key.ToString();
            if (keyString == null) return null;

            var dict = instance as IDictionary<string, object?>;
            if (dict != null && dict.TryGetValue(keyString, out object? value))
                return value;

            if (double.TryParse(keyString, out double doubleValue))
            {
                value = GetIndex(instance, (int)doubleValue);
                if (value != null) return value;
            }

            return null;
        }

        public static object? GetIndex(object? instance, int index)
        {
            if (instance == null) return null;
            var e = instance as IEnumerable;
            if (e != null) return e.Cast<object>().ElementAtOrDefault(index);

            return null;
        }

        public static object ParseToNumber(object? instance)
        {
            if (instance == null) return 0;

            if (double.TryParse(instance.ToString(), out var d))
                return d;

            return 0;

        }
    }
}
