using LexicalAnalyzer;
using RegularExpression;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
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

            var nfaNumber = CreateNFA(@"\d+(\.\d*)?|\.\d+");
            nfaList.Add((nfaNumber, new("number")));

            Console.WriteLine(nfaNumber);

            var nfaID = CreateNFA(@"\w(\d|\w)*");
            nfaList.Add((nfaID, new("id")));

            Console.WriteLine(nfaID);

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
            listProduction.AddRange(Production.Create("ExpValue", "number|id|( Exp )|id . id|id [ Exp ]"));
            listProduction.AddRange(Production.Create("ExpSquare", "ExpValue|ExpValue ^ ExpSquare"));
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

            string text = "1+2^3^4";
            using var reader = new StringReader(text);

            LRSyntaxAnalyzer syntaxAnalyzer = new(slrGrammar.GetAction(), slrGrammar.GetGoto());
            syntaxAnalyzer.Analyzer(lexicalAnalyzer.GetEnumerator(reader));
        }
    }
}