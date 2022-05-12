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
        static void Main1(string[] args)
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

        static void Main(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("Exp", "ExpArith|ExpLogic|ExpObject")); // 返回double|bool|object

            listProduction.AddRange(Production.Create("ExpLogic", "ExpAnd")); // 布尔表达式

            listProduction.AddRange(Production.Create("ExpAnd", "ExpOr|ExpAnd && ExpOr")); // 与
            listProduction.AddRange(Production.Create("ExpOr", "ExpEqual|ExpOr or ExpEqual")); // 或            
            listProduction.AddRange(Production.Create("ExpEqual", "ExpBool|ExpEqual == ExpBool|ExpEqual != ExpBool")); // 等于

            listProduction.AddRange(Production.Create("ExpBool", "ExpArith == ExpArith|ExpArith != ExpArith")); // 比较数值
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith > ExpArith|ExpArith >= ExpArith|ExpArith < ExpArith|ExpArith <= ExpArith")); // 比较数值
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject == ExpObject|ExpObject != ExpObject")); // 比较对象
            listProduction.AddRange(Production.Create("ExpBool", "( ExpLogic )")); // 布尔
            listProduction.AddRange(Production.Create("ExpBool", "bool")); // 布尔

            listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式

            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti|ExpAdd - ExpMulti")); // 加法
            listProduction.AddRange(Production.Create("ExpMulti", "ExpSign|ExpMulti * ExpSign|ExpMulti / ExpSign|ExpMulti % ExpSign")); // 乘法
            listProduction.AddRange(Production.Create("ExpSign", "ExpSquare|- ExpSign")); // 取反
            listProduction.AddRange(Production.Create("ExpSquare", "ExpNumber|ExpNumber ^ ExpSquare")); // 开方

            listProduction.AddRange(Production.Create("ExpNumber", "( ExpObject )")); // 将对象转换为数值，可能报错
            listProduction.AddRange(Production.Create("ExpNumber", "( ExpArith )")); //
            listProduction.AddRange(Production.Create("ExpNumber", "number")); // 数值

            listProduction.AddRange(Production.Create("ExpObject", "ExpObject [ ExpArith ]")); // 读取数组
            listProduction.AddRange(Production.Create("ExpObject", "ExpObject . id")); // 读取对象、字典(<string,object>)

            listProduction.AddRange(Production.Create("ExpObject", "id|null")); // 对象

            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject != ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith != ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject == ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith == ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject <= ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith <= ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject <= ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject < ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith < ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject < ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject >= ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith >= ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject >= ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject > ExpArith")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpArith > ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpBool", "ExpObject > ExpObject")); // 比较数值
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpObject - ExpMulti")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpAdd - ExpObject")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpObject - ExpObject")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpObject + ExpMulti")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpAdd + ExpObject")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpObject + ExpObject")); // 加法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject % ExpSign")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti % ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject % ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject / ExpSign")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti / ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject / ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject * ExpSign")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti * ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpObject * ExpObject")); // 乘法
            //listProduction.AddRange(Production.Create("ExpSign", "- ExpObject")); // 取反
            listProduction.AddRange(Production.Create("ExpSquare", "ExpObject ^ ExpSquare")); // 开方
            listProduction.AddRange(Production.Create("ExpSquare", "ExpNumber ^ ExpObject")); // 开方
            listProduction.AddRange(Production.Create("ExpSquare", "ExpObject ^ ExpObject")); // 开方
            listProduction.AddRange(Production.Create("ExpObject", "ExpObject [ ExpObject ]")); // 读取数组

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            Console.WriteLine(grammar);

            Grammar.PrintTable = true;

            //if (!LR0Grammar.TryCreate(grammar, out var rl0Grammar, out var rl0Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR0Grammar Error:\n{rl0Msg}");
            //}
            //else Console.WriteLine(rl0Grammar);

            if (!SLRGrammar.TryCreate(grammar, out var slrGrammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"SLRGrammar Error:\n{slrMsg}");
            }
            else Console.WriteLine(slrGrammar);

            return;

            //if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
            //    return;
            //}
            //else Console.WriteLine(lr1Grammar);

            //if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            //}
            //else Console.WriteLine(lalrGrammar);
        }
    }
}