using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public class LexicalAnalyzer
    {
        private Dictionary<NFA, Terminal> registerTable = new();
        private Terminal[] skipTerminals;
        private DFA unionNFA;
        private Dictionary<int, Terminal> z2TerminalTable;

        private void Register(NFA nfa, Terminal terminal)
        {
            if (nfa.S_0.Count() == 0) return;
            var newNfa = DFA.CreateFrom(nfa).Minimize().ToNFA();
            registerTable[newNfa] = terminal;
        }

        private (DFA, Dictionary<int, Terminal>) GenerateUnionNFA()
        {
            //S
            var S = new HashSet<int>();
            //Sigma
            var Sigma = new HashSet<char>();
            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            //Z
            var Z = new HashSet<int>();


            int head = 0;
            S.Add(head);

            Dictionary<int, NFA> zTable = new();
            foreach (var (nfa, terminal) in registerTable)
            {
                int base_id = S.Count();
                S.UnionWith(nfa.S.Select(s => base_id + s));
                Sigma.UnionWith(nfa.Sigma);
                MappingTable.UnionWith(nfa.MappingTable.Select(i => (base_id + i.s1, i.c, base_id + i.s2)));

                var z = nfa.Z.Select(s => base_id + s).Single();
                zTable[z] = nfa;
                Z.Add(z);

                //UnionNFA
                MappingTable.UnionWith(nfa.S_0.Select(s => (head, FA.CHAR_Epsilon, base_id + s)));
            }

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            var unionNFA = new NFA(S, Sigma, MappingTable, S_0, Z);
            var dfa = DFA.CreateFrom(unionNFA);
            var unionDFA = dfa.MinimizeByNfaFinal();

            var nfaZTable = unionDFA.ZNfaNodes;
            Dictionary<int, Terminal> z2TerminalTable = new();
            foreach (var z in unionDFA.Z)
            {
                var nfaState = nfaZTable[z].First();
                var nfa = zTable[nfaState];
                var t = registerTable[nfa];
                z2TerminalTable[z] = t;
            }

            return (unionDFA, z2TerminalTable);
        }

        public IEnumerator<(Terminal terminal, string token)> GetEnumerator(TextReader reader)
        {
            return new LexicalAnalyzerEnumerator(reader, this.unionNFA, this.z2TerminalTable, this.skipTerminals);
        }

        public LexicalAnalyzer(IEnumerable<(NFA nfa, Terminal terminal)> nfaList,
            IEnumerable<Terminal> skipTerminals)
        {
            this.skipTerminals = skipTerminals.ToArray();

            foreach (var item in nfaList)
            {
                Register(item.nfa, item.terminal);
            }

            (this.unionNFA, this.z2TerminalTable) = GenerateUnionNFA();
        }

        public LexicalAnalyzer(BinaryReader br)
        {
            var nfa_size = br.ReadInt32();
            for (int i = 0; i < nfa_size; i++)
            {
                var nfa = NFA.Load(br);
                registerTable.Add(nfa, new Terminal(br.ReadString()));
            }
            var skip_size = br.ReadInt32();
            skipTerminals = new Terminal[skip_size];
            for (int i = 0; i < skip_size; i++)
            {
                skipTerminals[i] = new(br.ReadString());
            }

            (this.unionNFA, this.z2TerminalTable) = GenerateUnionNFA();
        }

        public void Save(BinaryWriter bw)
        {
            var nfa_size = registerTable.Count;
            bw.Write(nfa_size);
            for (int i = 0; i < nfa_size; i++)
            {
                var nfa = registerTable.ElementAt(i).Key;
                //Console.WriteLine(nfa);
                nfa.Save(bw);
                bw.Write(registerTable.ElementAt(i).Value.Name);
            }
            var skip_size = skipTerminals.Length;
            bw.Write(skip_size);
            for (int i = 0; i < skip_size; i++)
            {
                bw.Write(skipTerminals[i].Name);
            }

        }
    }

    class LexicalAnalyzerEnumerator : IEnumerator<(Terminal terminal, string token)>
    {
        private TextReader textReader;
        private (int s1, char c, int s2)[] mappingTable;
        private HashSet<int> finalStates;
        private Dictionary<int, Terminal> z2TerminalTable;
        private HashSet<Terminal> skipTerminals;
        private (Terminal terminal, string token)? nextValue = null;

        /// <summary>
        /// 当前状态
        /// </summary>
        private int currentState = 0;
        /// <summary>
        /// 上次被接受的状态
        /// </summary>
        private int matchState = -1;

        private Queue<char> qFallback = new();

        public LexicalAnalyzerEnumerator(TextReader reader,
            DFA dfa,
            Dictionary<int, Terminal> table,
            IEnumerable<Terminal> skipTerminals)
        {
            textReader = reader;
            mappingTable = dfa.MappingTable.ToArray();
            finalStates = dfa.Z.ToHashSet();
            z2TerminalTable = table;
            this.skipTerminals = skipTerminals.ToHashSet();
        }

        public (Terminal terminal, string token) Current
        {
            get
            {
                if (nextValue.HasValue)
                {
                    var returnValue = nextValue.Value;
                    nextValue = null;
                    return returnValue;
                }

                throw new Exception("无值");
            }
        }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        private void GetNextValue()
        {
            StringBuilder currentToken = new(); //待匹配字符串
            StringBuilder matchToken = new(); //已匹配字符串
            nextValue = null;

            while (qFallback.Count > 0 || textReader.Peek() != -1)
            {
                char c;
                if (qFallback.Count > 0)
                    c = qFallback.Dequeue();
                else
                    c = (char)textReader.Read();

                var items = mappingTable.Where(i => i.s1 == currentState && i.c == c); //查找下一个状态

                if (!items.Any())
                {
                    if (matchState == -1)
                        throw new LexicalAnalyzerException(currentToken.ToString() + c);

                    //将匹配失败的字符串放回待匹配的队列
                    foreach (var cFallback in currentToken.ToString())
                        qFallback.Enqueue(cFallback);
                    qFallback.Enqueue(c);

                    Terminal terminal = z2TerminalTable[matchState];
                    string tokenString = matchToken.ToString();

                    nextValue = (terminal, tokenString);
                    currentState = 0;
                    matchState = -1;
                    break;
                }
                else
                {
                    (int s1, char _, int s2) = items.First();
                    currentState = s2; //更新当前状态
                    currentToken.Append(c);

                    if (finalStates.Contains(currentState)) //当前状态属于接受状态
                    {
                        matchState = currentState;
                        matchToken.Append(currentToken); //更长的字符串被接受，更新已匹配字符串
                        currentToken.Clear();
                    }
                }
            }

            if (nextValue == null)
            {
                if (matchState == -1)
                {
                    if (currentToken.Length > 0)
                        throw new LexicalAnalyzerException(currentToken.ToString());
                }
                else
                {
                    //将匹配失败的字符串放回待匹配的队列
                    foreach (var cFallback in currentToken.ToString())
                        qFallback.Enqueue(cFallback);

                    Terminal terminal = z2TerminalTable[matchState];
                    string tokenString = matchToken.ToString();

                    nextValue = (terminal, tokenString);
                    currentState = 0;
                    matchState = -1;
                }
            }


        }

        public bool MoveNext()
        {
            do
            {
                GetNextValue();
            }
            while (nextValue.HasValue && skipTerminals.Contains(nextValue.Value.terminal));

            return nextValue.HasValue;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }

    public class LexicalAnalyzerException : Exception
    {
        public LexicalAnalyzerException(string seq)
        : base($"匹配错误，当前序列为：{seq}。") { }
    }
}
