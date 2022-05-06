using LexicalAnalyzer;
using RegularExpression;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

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

            var nfaDecimal = CreateNFA(@"\d+(\.\d*)?|\.\d+");
            nfaList.Add((nfaDecimal, new("decimal")));

            var nfaInteger = CreateNFA(@"\d+");
            nfaList.Add((nfaInteger, new("integer")));

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

        static void Main2(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("ExpAtom", "integer|decimal|id|( Exp )"));
            listProduction.AddRange(Production.Create("ExpValue", "id . id|ExpValue . id|id [ Exp ]|ExpValue [ Exp ]"));
            listProduction.AddRange(Production.Create("ExpSquare", "ExpAtom|ExpValue|ExpValue ^ ExpSquare"));
            listProduction.AddRange(Production.Create("ExpSign", "ExpSquare|- ExpSquare"));
            listProduction.AddRange(Production.Create("ExpMulti", "ExpSign|ExpMulti * ExpSign|ExpMulti / ExpSign|ExpMulti % ExpSign"));
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti|ExpAdd - ExpMulti"));
            listProduction.AddRange(Production.Create("ExpCompare", "ExpAdd|ExpAdd > ExpAdd|ExpAdd >= ExpAdd|ExpAdd < ExpAdd|ExpAdd <= ExpAdd"));
            listProduction.AddRange(Production.Create("ExpLogicEqual", "ExpCompare|ExpLogicEqual == ExpCompare|ExpLogicEqual != ExpCompare"));
            listProduction.AddRange(Production.Create("ExpLogicOr", "ExpLogicEqual|ExpLogicOr or ExpLogicEqual"));
            listProduction.AddRange(Production.Create("ExpLogicAnd", "ExpLogicOr|ExpLogicAnd && ExpLogicOr"));
            listProduction.AddRange(Production.Create("Exp", "ExpLogicAnd"));

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            Console.WriteLine(grammar);

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

            string text = "a[b + c]";
            using var reader = new StringReader(text);

            LRSyntaxAnalyzer syntaxAnalyzer = new(slrGrammar.GetAction(), slrGrammar.GetGoto());
            syntaxAnalyzer.Analyzer(lexicalAnalyzer.GetEnumerator(reader));
        }

        static void Main(string[] args)
        {
            dynamic obj = new ExpandoObject();
            obj.aa = 123;
            Console.WriteLine(obj.aa);
        }

        class DClass1 : DynamicObject
        {
            Dictionary<string, object?> dictionary = new();

            // This property returns the number of elements
            // in the inner dictionary.
            public int Count
            {
                get
                {
                    return dictionary.Count;
                }
            }

            // If you try to get a value of a property
            // not defined in the class, this method is called.
            public override bool TryGetMember(
                GetMemberBinder binder, out object? result)
            {
                // Converting the property name to lowercase
                // so that property names become case-insensitive.
                string name = binder.Name.ToLower();

                // If the property name is found in a dictionary,
                // set the result parameter to the property value and return true.
                // Otherwise, return false.
                return dictionary.TryGetValue(name, out result);
            }

            // If you try to set a value of a property that is
            // not defined in the class, this method is called.
            public override bool TrySetMember(
                SetMemberBinder binder, object? value)
            {
                // Converting the property name to lowercase
                // so that property names become case-insensitive.
                dictionary[binder.Name.ToLower()] = value;

                // You can always add a value to a dictionary,
                // so this method always returns true.
                return true;
            }
        }
    }
}