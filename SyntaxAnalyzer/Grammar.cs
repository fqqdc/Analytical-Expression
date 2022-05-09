using LexicalAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class Grammar
    {
        public static bool CanPrintItems = false;
        public static bool CanPrintConflictTable = true;

        public Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal)
        {
            var symbols = allProduction.SelectMany(p => p.Right.Append(p.Left))
                .Except(Production.Epsilon).ToHashSet();
            _Vn = symbols.Where(s => s is NonTerminal).Cast<NonTerminal>().ToHashSet();
            _Vt = symbols.Where(s => s is Terminal).Cast<Terminal>().ToHashSet();
            _P = allProduction.ToHashSet();

            var leftVn = allProduction.Select(p => p.Left).ToHashSet();
            if (!leftVn.Contains(startNonTerminal))
                throw new NotSupportedException($"无效的起始符:{startNonTerminal}");

            S = startNonTerminal;
        }

        private readonly HashSet<Terminal> _Vt;
        private readonly HashSet<NonTerminal> _Vn;
        private readonly HashSet<Production> _P;

        public IEnumerable<Terminal> Vt { get => _Vt.AsEnumerable(); }
        public IEnumerable<NonTerminal> Vn { get => _Vn.AsEnumerable(); }
        public IEnumerable<Production> P { get => _P.AsEnumerable(); }
        public NonTerminal S { get; private set; }

        #region ToString()
        protected const string PRE = "    ";

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(GetType().Name).AppendLine();
            VtToString(builder);
            VnToString(builder);
            PToString(builder);
            SToString(builder);
            return builder.ToString();
        }

        public string ToFullString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ToString());
            return builder.ToString();
        }

        private void VtToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Terminals : {");
            foreach (var t in Vt)
            {
                builder.Append($" {t},");
            }
            if (Vt.Any())
                builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void VnToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("NonTerminal : {");
            foreach (var n in Vn.OrderBy(n => n != S))
            {
                builder.Append($" {n},");
            }
            if (Vn.Any())
                builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void PToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Productions :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var p in P)
            {
                builder.Append(PRE).Append(PRE).Append(p).AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }

        private void SToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"START : {S}").AppendLine();
        }



        #endregion

        /// <summary>
        /// 计算FIRST集
        /// </summary>
        protected static Dictionary<NonTerminal, HashSet<Terminal>> CalcFirsts(IEnumerable<Production> P)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapFirst = new();
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;

                foreach (var production in P)
                {
                    if (!mapFirst.TryGetValue(production.Left, out var first))
                    {
                        first = new();
                        mapFirst[production.Left] = first;
                        hasChanged = true;
                    }

                    var oldFirst = first.ToHashSet();

                    bool endWithEpsilon = true;
                    foreach (var symbol in production.Right)
                    {
                        if (symbol is Terminal terminal)
                        {
                            /// 若X∈Vt,则FIRST(X)={X}
                            /// 若X∈Vn,且有产生式X->a...，则把a加入到FIRST(X)中；
                            /// 若X->ε也是一条产生式，则把ε也加到FIRST(X)中。
                            first.Add(terminal);
                            endWithEpsilon = false;
                            break;
                        }

                        ///若X->Y1Y2...Yi-1Yi...Yk是一个产生式，Y1,...Yi-1都是非终结符                        
                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFirst.TryGetValue(nonTerminal, out var firstN))
                            {
                                firstN = new();
                                mapFirst[nonTerminal] = firstN;
                                hasChanged = true;
                            }

                            ///对于任何j，1<=j<=i-1，FIRST(Yj)都含有ε(即Y1...Yi=>ε)，则把FIRST(Yi)中的所有非ε元素都加到FIRST(X)中
                            var firstN_Copy = firstN.ToHashSet();
                            firstN_Copy.Remove(Terminal.Epsilon);
                            first.UnionWith(firstN_Copy);

                            if (!firstN.Contains(Terminal.Epsilon))
                            {
                                endWithEpsilon = false;
                                break;
                            }
                        }
                    }
                    if (endWithEpsilon)
                    {
                        ///若所有的FIRST(Yj)均含有ε，j=1,2,3,...,k，则把ε加到FIRST(X)中
                        first.Add(Terminal.Epsilon);
                    }

                    hasChanged = hasChanged || !first.SetEquals(oldFirst);
                }
            }

            //Console.WriteLine(mapFirst.ToString("First Sets:"));
            return mapFirst;
        }

        protected static HashSet<Terminal> CalcFirst(IEnumerable<Symbol> alpha, Dictionary<NonTerminal, HashSet<Terminal>> mapFirst)
        {
            HashSet<Terminal> first = new();
            foreach (var symbol in alpha)
            {

                if (symbol is Terminal terminal)
                {
                    first.Add(terminal);
                    break;
                }

                if (symbol is NonTerminal nonTerminal)
                {
                    if (!mapFirst.TryGetValue(nonTerminal, out var firstNonTerminal))
                    {
                        firstNonTerminal = new();
                    }

                    first.UnionWith(firstNonTerminal);
                    first.Remove(Terminal.Epsilon);

                    if (!firstNonTerminal.Contains(Terminal.Epsilon))
                        break;
                }
                first.Add(Terminal.Epsilon);
            }

            return first;
        }

        /// <summary>
        /// 计算FOLLOW集
        /// </summary>
        protected static Dictionary<NonTerminal, HashSet<Terminal>> CalcFollows(IEnumerable<Production> P, Dictionary<NonTerminal, HashSet<Terminal>> mapFirst, NonTerminal S)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();
            mapFollow[S] = new();
            mapFollow[S].Add(Terminal.EndTerminal); //对于文法开始符号S，要将#置于FOLLOW(S)中

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;

                foreach (var production in P)
                {

                    Stack<Symbol> beta = new();

                    foreach (var symbol in production.Right.Reverse())
                    {
                        if (symbol is Terminal terminal)
                        {
                            beta.Push(terminal);
                            continue;
                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFollow.TryGetValue(nonTerminal, out var follow))
                            {
                                follow = new();
                                mapFollow[nonTerminal] = follow;
                                hasChanged = true;
                            }

                            var oldFollow = follow.ToHashSet();
                            if (beta.Any())
                            {
                                var first = CalcFirst(beta, mapFirst);
                                //若A->αBβ是一个产生式，则把FIRST(β)\{ε}加入FOLLOW(B)中
                                bool containEpsilon = first.Remove(Terminal.Epsilon);
                                follow.UnionWith(first);

                                if (containEpsilon && mapFollow.TryGetValue(production.Left, out var followLeft))
                                {
                                    follow.UnionWith(followLeft); //若A->αBβ是一个产生式，而β=>ε(既ε∈FIRST(β))，则将FIRST(A)加入FIRST(B)
                                }
                            }
                            else
                            {
                                if (mapFollow.TryGetValue(production.Left, out var followLeft))
                                {
                                    follow.UnionWith(followLeft); //若A->αB是一个产生式，则将FIRST(A)加入FIRST(B)
                                }
                            }

                            hasChanged = hasChanged || !oldFollow.SetEquals(follow);
                            beta.Push(nonTerminal);
                        }
                    }
                }
            }

            //Console.WriteLine(mapFollow.ToString("Follow Sets:"));
            return mapFollow;
        }
    }
}
