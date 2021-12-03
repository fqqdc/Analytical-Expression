using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class LexicalAnalyzer_Old
    {
        HashSet<StateMachine> states;

        public LexicalAnalyzer_Old(IEnumerable<StateMachine> machines)
        {
            states = new(machines);
        }

        class MachineComparer : IComparer<StateMachine>
        {
            int IComparer<StateMachine>.Compare(StateMachine? x, StateMachine? y)
            {
                return x.Count - y.Count;
            }
        }

        public void Analyze(string txt)
        {
            Console.WriteLine(txt);

            int basePos = 0;
            int curPos = 0;
            HashSet<StateMachine> workStates = new(); 

            while (curPos < txt.Length)
            {
                char c = txt[curPos];

                if (workStates.Count == 0)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        basePos++;
                        curPos++;
                        continue;
                    }
                    else
                    {
                        states.ToList().ForEach(m => m.Reset());
                        workStates = new(states);
                    }
                }

                foreach (var sm in workStates)
                {
                    sm.Jump(c);
                    if (!sm.IsWork)
                        workStates.Remove(sm);
                }
                curPos += 1;


                if (workStates.Count > 0) continue;


                var machine = states.Max(new MachineComparer());
                if (machine != null && machine.Count != 0)
                {
                    Console.WriteLine($"{machine.Name,-10}:{txt.Substring(basePos, machine.Count)}");
                    basePos += machine.Count;
                    curPos = basePos;
                }
                else
                {
                    throw new Exception("非法字符");
                }
            }
        }
    }
}
