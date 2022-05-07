﻿using LexicalAnalyzer;
using RegularExpression;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;

namespace ArithmeticExpression
{
    class Program
    {
        static RegularLRSyntaxAnalyzer RegularAnalyzer = RegularLRSyntaxAnalyzer.LoadFromFile();
        static NFA CreateNFA(string regularExp)
        {
            RegularAnalyzer.Analyzer(regularExp);

            if (RegularAnalyzer.RegularNFA == null)
                throw new NotSupportedException();

            var dfa = DFA.CreateFrom(RegularAnalyzer.RegularNFA);

            return dfa.Minimize().ToNFA();
        }

        const string DefaultFileName = "Arithmetic";

        static void Main1(string[] args)
        {
            List<(NFA, Terminal)> nfaList = new();


            var nfaInteger = CreateNFA(@"\d+");
            nfaList.Add((nfaInteger, new("integer")));

            var nfaDecimal = CreateNFA(@"\d+(\.\d*)?|\.\d+");
            nfaList.Add((nfaDecimal, new("decimal")));            

            var nfaID = CreateNFA(@"\w(\d|\w)*");
            nfaList.Add((nfaID, new("id")));

            string[] opts = { "(", ")", ".", "[", "]", "^", "*", "/", "%", "+", "-", ">", ">=", "<", "<=", "==", "!=", "&&", };
            foreach (var strOpt in opts)
                nfaList.Add((NFA.CreateFromString(strOpt), new Terminal(strOpt)));
            nfaList.Add((NFA.CreateFromString("||"), new Terminal("or")));

            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));
            nfaList.Add((nfaSkip, new("skip")));

            List<Terminal> skipTerminals = new() { new("skip") };

            LexicalAnalyzer.LexicalAnalyzer lexicalAnalyzer = new(nfaList, skipTerminals);
            using var fs = File.OpenWrite($"{DefaultFileName}.lexical");
            using var bw = new BinaryWriter(fs);
            lexicalAnalyzer.Save(bw);
        }

        static void Main(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("ExpAtom", "integer|decimal|( Exp )|id"));
            listProduction.AddRange(Production.Create("ExpObject", "id . id|ExpObject . id"));
            listProduction.AddRange(Production.Create("ExpObject", "id [ ExpAtom ]|ExpObject [ ExpAtom ]"));
            listProduction.AddRange(Production.Create("ExpValue", "ExpAtom|ExpObject"));
            listProduction.AddRange(Production.Create("ExpSquare", "ExpValue|ExpValue ^ ExpSquare"));
            listProduction.AddRange(Production.Create("ExpSign", "ExpSquare|- ExpSign"));
            listProduction.AddRange(Production.Create("ExpMulti", "ExpSign|ExpMulti * ExpSign|ExpMulti / ExpSign|ExpMulti % ExpSign"));
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti|ExpAdd - ExpMulti"));
            listProduction.AddRange(Production.Create("ExpCompare", "ExpAdd|ExpAdd > ExpAdd|ExpAdd >= ExpAdd|ExpAdd < ExpAdd|ExpAdd <= ExpAdd"));
            listProduction.AddRange(Production.Create("ExpLogicEqual", "ExpCompare|ExpLogicEqual == ExpCompare|ExpLogicEqual != ExpCompare"));
            listProduction.AddRange(Production.Create("ExpLogicOr", "ExpLogicEqual|ExpLogicOr or ExpLogicEqual"));
            listProduction.AddRange(Production.Create("ExpLogicAnd", "ExpLogicOr|ExpLogicAnd && ExpLogicOr"));
            listProduction.AddRange(Production.Create("Exp", "ExpLogicAnd"));

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            //Console.WriteLine(grammar);

            SLRGrammar.CanPrintItems = false;
            if (!SLRGrammar.TryCreate(grammar, out var slrGrammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"SLRGrammar Error:\n{slrMsg}");
                return;
            }
            else Console.WriteLine(slrGrammar);

            using var fs = File.OpenRead($"{DefaultFileName}.lexical");
            using var br = new BinaryReader(fs);
            LexicalAnalyzer.LexicalAnalyzer lexicalAnalyzer = new(br);

            string text = "--b.b[(2^2)]";
            using var reader = new StringReader(text);

            ArithmeticSyntaxAnalyzer syntaxAnalyzer = new(slrGrammar.GetAction(), slrGrammar.GetGoto());
            syntaxAnalyzer.Analyzer(lexicalAnalyzer.GetEnumerator(reader));

            if (syntaxAnalyzer.Target == null || syntaxAnalyzer.Parameter == null)
                throw new NullReferenceException();

            var func = Expression.Lambda<Func<IDictionary<string, object?>, object>>(syntaxAnalyzer.Target, syntaxAnalyzer.Parameter).Compile();

            dynamic obj = new ExpandoObject();
            obj.a = new { b = new { c = 666 } };
            obj.b = new ExpandoObject();
            obj.b.b = new int[] { 1, 2, 3, 4, 5 };
            obj.b.c = 1234;
            obj.b.d = new ExpandoObject();
            obj.b.d.e = "777";
            obj.b.d.f = "abc";

            object? value = func(obj);

            Console.WriteLine();
            Console.WriteLine($"value:{value ?? "null"}");
            Console.WriteLine(value?.GetType());

        }

        class TestClass 
        {
            public int IntValue { get; set; }
        }


        static void Main3(string[] args)
        {
            object d = 123.2m;
            Console.WriteLine(int.Parse(d.ToString()));
        }
    }
}