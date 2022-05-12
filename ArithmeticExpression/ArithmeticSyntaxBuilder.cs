using LexicalAnalyzer;
using RegularExpression;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticExpression
{
    internal class ArithmeticSyntaxBuilder
    {
        public const string DefaultFileName = "Arithmetic";

        private static RegularLRSyntaxAnalyzer RegularAnalyzer = RegularLRSyntaxAnalyzer.LoadFromFile();
        private static NFA CreateNFA(string regularExp)
        {
            RegularAnalyzer.Analyzer(regularExp);

            if (RegularAnalyzer.RegularNFA == null)
                throw new NotSupportedException();

            var dfa = DFA.CreateFrom(RegularAnalyzer.RegularNFA);

            return dfa.Minimize().ToNFA();
        }

        public static void CreateRegularFiles()
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

            listProduction.AddRange(Production.Create("ExpBool", "ExpObject != ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith != ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject == ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith == ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject <= ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith <= ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject <= ExpObject")); // 比较-对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject < ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith < ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject < ExpObject")); // 比较-对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject >= ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith >= ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject >= ExpObject")); // 比较-对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject > ExpArith")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpArith > ExpObject")); // 比较-数值与对象
            listProduction.AddRange(Production.Create("ExpBool", "ExpObject > ExpObject")); // 比较-对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpObject - ExpMulti")); // 加法-数值与对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpAdd - ExpObject")); // 加法-数值与对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpObject - ExpObject")); // 加法-对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpObject + ExpMulti")); // 加法-数值与对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpAdd + ExpObject")); // 加法-对象
            listProduction.AddRange(Production.Create("ExpAdd", "ExpObject + ExpObject")); // 加法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject % ExpSign")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti % ExpObject")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject % ExpObject")); // 乘法-对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject / ExpSign")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti / ExpObject")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject / ExpObject")); // 乘法-对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject * ExpSign")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpMulti * ExpObject")); // 乘法-数值与对象
            listProduction.AddRange(Production.Create("ExpMulti", "ExpObject * ExpObject")); // 乘法-对象
            listProduction.AddRange(Production.Create("ExpSign", "- ExpObject")); // 取反-对象
            listProduction.AddRange(Production.Create("ExpSquare", "ExpObject ^ ExpSquare")); // 开方-数值与对象
            listProduction.AddRange(Production.Create("ExpSquare", "ExpNumber ^ ExpObject")); // 开方-数值与对象
            listProduction.AddRange(Production.Create("ExpSquare", "ExpObject ^ ExpObject")); // 开方-对象
            listProduction.AddRange(Production.Create("ExpObject", "ExpObject [ ExpObject ]")); // 读取数组-对象索引

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            Console.WriteLine(grammar);


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
                return;
            }
            else Console.WriteLine(slrGrammar);

            //var clr1Grammar = CLR1Grammar.Create(grammar);

            //Console.WriteLine();
            //Console.WriteLine($"CLR1Grammar Conflict:\n{clr1Grammar.ConflictMessage}");

            //Console.WriteLine(clr1Grammar);

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

            Dictionary<(int state, Terminal t), List<ActionItem>>? actionTable = null;
            Dictionary<(int state, NonTerminal t), int>? gotoTable = null;
            FileInfo syntaxFile = new($"{DefaultFileName}.syntax");
            FileInfo lexicalFile = new FileInfo($"{DefaultFileName}.lexical");
            LexicalAnalyzer.LexicalAnalyzer? lexicalAnalyzer = null;

            actionTable = slrGrammar.GetAction();
            gotoTable = slrGrammar.GetGoto();

            using (var fs = syntaxFile.Open(FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                actionTable.Save(bw);
                gotoTable.Save(bw);
            }

            List<(NFA, Terminal)> nfaList = new();

            var nfaDecimal = CreateNFA(@"\d+(\.\d+)?|\.\d+");
            nfaList.Add((nfaDecimal, new("number")));

            var nfaBool = CreateNFA(@"true|false");
            nfaList.Add((nfaBool, new("bool")));

            var nfaID = CreateNFA(@"\w(\d|\w)*");
            nfaList.Add((nfaID, new("id")));

            string[] opts = { "(", ")", ".", "[", "]", "^", "*", "/", "%", "+", "-", ">", ">=", "<", "<=", "==", "!=", "&&", "null" };
            foreach (var strOpt in opts)
                nfaList.Add((NFA.CreateFromString(strOpt), new Terminal(strOpt)));
            nfaList.Add((NFA.CreateFromString("||"), new Terminal("or")));

            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));
            nfaList.Add((nfaSkip, new("skip")));

            List<Terminal> skipTerminals = new() { new("skip") };

            lexicalAnalyzer = new(nfaList, skipTerminals);
            using (var fs = lexicalFile.Open(FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                lexicalAnalyzer.Save(bw);
            }
        }
    }
}
