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

        public static object? GetIndex(object? instance, object key)
        {
            if (instance == null || key == null) return null;

            string? keyString = key.ToString();
            if (keyString == null) return null;

            var dict = instance as IDictionary<string, object?>;
            if (dict != null && dict.TryGetValue(keyString, out object? value))
                return value;

            if (int.TryParse(keyString, out int intValue))
            {
                value = GetIndex(instance, intValue);
                if (value != null) return value;
            }

            return null;
        }

        public static object? GetIndex(object? instance, int index)
        {
            if (instance == null) return null;
            var e = instance as IEnumerable;
            if (e != null) return e.Cast<object>().ElementAt(index);

            return null;
        }

        public static object ParseToNumber(object? instance)
        {
            if (instance == null) return 0;

            if (double.TryParse(instance.ToString(), out var d))
            {
                if (d - (int)d == 0) return (int)d;
                else return d;
            }

            if (string.IsNullOrEmpty(instance.ToString()))
                return 0;
            else return 1;

        }

        public static object? Negate(object? obj)
        {
            if (obj == null) return null;
            if (obj is int) return -(int)obj;
            if (obj is double) return -(double)obj;
            return new NotSupportedException();
        }

        public static object? Calculate(string? op, object? obj1, object? obj2)
        {
            if (obj1 == null)
                obj1 = 0;
            if (obj2 == null)
                obj2 = 0;

            string str1 = obj1.ToString() ?? "0";
            string str2 = obj2.ToString() ?? "0"; ;

            switch (op)
            {
                case "+":
                    {
                        try
                        {
                            var v1 = int.Parse(str1);
                            var v2 = int.Parse(str2);
                            return v1 + v2;
                        }
                        catch { }

                        try
                        {
                            var v1 = decimal.Parse(str1);
                            var v2 = decimal.Parse(str2);
                            return v1 + v2;
                        }
                        catch { }
                    }
                    break;
                case "-":
                    {
                        try
                        {
                            var v1 = int.Parse(str1);
                            var v2 = int.Parse(str2);
                            return v1 - v2;
                        }
                        catch { }

                        try
                        {
                            var v1 = decimal.Parse(str1);
                            var v2 = decimal.Parse(str2);
                            return v1 - v2;
                        }
                        catch { }
                    }
                    break;
                case "*":
                    {
                        try
                        {
                            var v1 = int.Parse(str1);
                            var v2 = int.Parse(str2);
                            return v1 * v2;
                        }
                        catch { }

                        try
                        {
                            var v1 = decimal.Parse(str1);
                            var v2 = decimal.Parse(str2);
                            return v1 * v2;
                        }
                        catch { }
                    }
                    break;
                case "/":
                    {
                        try
                        {
                            var v1 = int.Parse(str1);
                            var v2 = int.Parse(str2);
                            return v1 / v2;
                        }
                        catch { }

                        try
                        {
                            var v1 = decimal.Parse(str1);
                            var v2 = decimal.Parse(str2);
                            return v1 / v2;
                        }
                        catch { }
                    }
                    break;
                case "^":
                    {
                        try
                        {
                            var v1 = int.Parse(str1);
                            var v2 = int.Parse(str2);
                            return (int)Math.Pow(v1, v2);
                        }
                        catch { }

                        try
                        {
                            var v1 = double.Parse(str1);
                            var v2 = double.Parse(str2);
                            return (decimal)Math.Pow(v1, v2);
                        }
                        catch { }
                    }
                    break;
            }
            throw new NotSupportedException($"不支持的运算:{str1} {op} {str2}");
        }
    }
}
