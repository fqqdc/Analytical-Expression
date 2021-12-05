using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class LexicalAnalyzer
    {
        public LexicalAnalyzer(string text)
        {
            byte[] array = Encoding.UTF8.GetBytes(text);
            MemoryStream stream = new MemoryStream(array);
            reader = new StreamReader(stream);
        }

        private StreamReader reader;
        private HashSet<(int id, Func<string, (int type, string token)> func)> RegisterTable = new();
        private TreeFA treeFA = new();

        private DFA workFA;
        private int workState;
        private Queue<char> qBeforeMatch = new(), qFallback = new();
        private StringBuilder builder = new();

        public void Register(NFA nfa, Func<string, (int type, string token)> returnAction)
        {
            var ids = treeFA.Union(DFA.CreateFrom(nfa).Minimize());
            foreach (var id in ids)
            {
                RegisterTable.Add((id, returnAction));
            }

            workFA = DFA.CreateFrom(treeFA.ToNFA());
            workState = 0;
        }

        public void PrintTable()
        {
            Console.WriteLine(workFA); 
        }

        public (int type, string token)? NextToken()
        {
            while (qFallback.Count > 0 || !reader.EndOfStream)
            {
                char c;
                if (qFallback.Count > 0)
                    c = qFallback.Dequeue();
                else
                    c = (char)reader.Read();

                if (workState == 0)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                }

                var items = workFA.MappingTable.Where(i => i.s1 == workState && i.t.Name == c.ToString());

                if (items.Count() == 0)
                {
                    try
                    {
                        if (builder.Length == 0)
                            throw new NotImplementedException("匹配错误");

                        while (qBeforeMatch.Count > 0)
                            qFallback.Enqueue(qBeforeMatch.Dequeue());
                        qFallback.Enqueue(c);

                        var token = builder.ToString();                        

                        var tfaState = workFA.ZTable.Where(i => i.dfa == workState).OrderBy(i => i.nfa).First().nfa;
                        var func = RegisterTable.First(i => i.id == tfaState).func;
                        return func(token);
                    }
                    finally
                    {
                        workState = 0;
                        builder.Clear();
                    }
                }

                workState = items.Single().s2;
                builder.Append(c);

                if (workFA.Z.Contains(workState))
                {
                    qBeforeMatch.Clear();
                }
                else
                {
                    qBeforeMatch.Enqueue(c);
                }
            }

            if (builder.Length != 0)
            {
                try
                {
                    if (workFA.Z.Contains(workState))
                    {
                        var token = builder.ToString();

                        var tfaState = workFA.ZTable.Where(i => i.dfa == workState).OrderBy(i => i.nfa).First().nfa;
                        var func = RegisterTable.First(i => i.id == tfaState).func;
                        return func(token);
                    }
                    else throw new NotImplementedException("匹配错误");
                }
                finally
                {
                    workState = 0;
                    builder.Clear();
                }
            }

            return null;
        }
    }
}
