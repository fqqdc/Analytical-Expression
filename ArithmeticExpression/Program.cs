using LexicalAnalyzer;
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


            //var nfaInteger = CreateNFA(@"\d+");
            //nfaList.Add((nfaInteger, new("integer")));

            var nfaDecimal = CreateNFA(@"\d+(\.\d+)?|\.\d+");
            nfaList.Add((nfaDecimal, new("number")));

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
            listProduction.AddRange(Production.Create("ExpAtom", "number|( Exp )|id"));
            listProduction.AddRange(Production.Create("ExpObject", "id . id|ExpObject . id"));
            listProduction.AddRange(Production.Create("ExpObject", "id [ Exp ]|ExpObject [ Exp ]"));
            listProduction.AddRange(Production.Create("ExpValue", "ExpAtom|ExpObject"));
            listProduction.AddRange(Production.Create("ExpSquare", "ExpValue|ExpValue ^ ExpSquare"));
            listProduction.AddRange(Production.Create("ExpSign", "ExpSquare|- ExpSign"));
            listProduction.AddRange(Production.Create("ExpMulti", "ExpSign|ExpMulti * ExpSign|ExpMulti / ExpSign|ExpMulti % ExpSign"));
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti|ExpAdd - ExpMulti"));
            listProduction.AddRange(Production.Create("ExpCompare", "ExpAdd|ExpAdd > ExpAdd|ExpAdd >= ExpAdd|ExpAdd < ExpAdd|ExpAdd <= ExpAdd"));
            listProduction.AddRange(Production.Create("ExpLogicEqual", "ExpCompare|ExpCompare == ExpCompare|ExpCompare != ExpCompare"));
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

            //string text = "--b.b[(2^2)]^2";
            string text = "v2 <= v3 || 0";

            ArithmeticSyntaxAnalyzer syntaxAnalyzer = new(slrGrammar.GetAction(), slrGrammar.GetGoto());
            Func<IDictionary<string, object?>, object> GetDelegate(string text)
            {
                using var reader = new StringReader(text);
                
                syntaxAnalyzer.Analyzer(lexicalAnalyzer.GetEnumerator(reader));

                if (syntaxAnalyzer.Target == null || syntaxAnalyzer.Parameter == null)
                    throw new NullReferenceException();

                var func = Expression.Lambda<Func<IDictionary<string, object?>, object>>(syntaxAnalyzer.Target, syntaxAnalyzer.Parameter).Compile();

                return func;
            }

            dynamic obj = new ExpandoObject();
            obj.a = new { b = new { c = 666 } };
            obj.b = new ExpandoObject();
            obj.b.c = new int[] { 100, 101, 102, 103, 104, 105 };
            obj.b.d = 1234;
            obj.b.e = new ExpandoObject();
            obj.b.e.f = "100";
            obj.b.e.g = "200";
            obj.b.e.h = "abc";
            obj.v2 = 4;
            obj.v3 = 3;

            //text = "a.b.c";
            //Console.WriteLine($"{text} = {GetDelegate(text)(obj) ?? "null"}");
            //text = "b[c][3]";
            //Console.WriteLine($"{text} = {GetDelegate(text)(obj) ?? "null"}");
            //text = "b.e.f + b.e.g";
            //Console.WriteLine($"{text} = {GetDelegate(text)(obj) ?? "null"}");
            //text = "b.e.h"; 
            //Console.WriteLine($"{text} = {GetDelegate(text)(obj) ?? "null"}");
            text = "v2";
            Console.WriteLine($"{text} = {GetDelegate(text)(obj) ?? "null"}");

        }

        class TestClass
        {
            public int IntValue { get; set; }
        }


        static void Main3(string[] args)
        {
            int[] arr = new int[] { 1, 2, 3 };
            Console.WriteLine(arr[2 + 1]);
        }
    }
}