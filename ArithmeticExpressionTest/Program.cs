using ArithmeticExpression;
using LexicalAnalyzer;
using RegularExpression;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;

namespace ArithmeticExpressionTest
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

            obj.b.e = new ExpandoObject();
            obj.b.e.f = "100";
            obj.b.e.g = "200";

            obj.v2 = 2;
            obj.v3 = 3;

            var text = " 2 == 3 == 4 == 5";
            object? value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = " b.c[5-(v2)] ";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = "(v2) + (v3) + (v4)";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = "b.c[(v3)]";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = "2 ^ (3 * (4 - 1) % 5) + 6 / 7";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");

            text = "v4 == v2 && 2 - 3 > -10";
            value = analyzer.Analyzer(text)(obj);
            Console.WriteLine($"{text} => {value ?? "null" } {value?.GetType().Name}");
        }
    }
}