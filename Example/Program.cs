using ArithmeticExpression;
using System;
using System.Dynamic;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var analyzer = ArithmeticSyntaxAnalyzer.LoadFromFile();

            dynamic obj = new ExpandoObject();
            obj.a = new { b = new { c = 666 } };

            obj.b = new ExpandoObject();
            obj.b.c = new int[] { 100, 101, 102, 103, 104, 105 };
            obj.b.v1 = 1;

            obj.b.e = new ExpandoObject();
            obj.b.e.f = "100";
            obj.b.e.g = "200";

            obj.v2 = 2;
            obj.v3 = 3;
            obj.v4 = 4;

            obj.s1 = "aaa";
            obj.s2 = "aaa";

            string text;
            object? value;

            text = " v2 == 3 == v4 == 5";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " b.c[2*v2] ";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " v2 * v3";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " b.e.f + b.e.g";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " b.c[b.v1]";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " 2 ^ (3 * (4 - 1) % 5) + 6 / 7";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " s1 == s2  && 2 - 3 > -10";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");
        }
    }
}