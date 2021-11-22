using System.Collections.Generic;
using System.Linq;

namespace Analytical_Expression
{
    public class StateMachine
    {
        private bool IsAcceptable
        {
            get
            {
                return current.IsAcceptable;
            }
        }

        DfaDigraphNode head, current;
        public int Count { get; private set; } = 0;
        private int jumpCount = 0;

        public bool IsWork { get; private set; } = true;

        public string StateName
        {
            get
            {
                return current.ToString();
            }
        }

        public string Name { get; set; }

        public StateMachine(DfaDigraphNode dfa)
        {
            this.head = dfa;
            current = head;
        }

        public bool Jump(int c)
        {
            if (!IsWork)
                return false;

            if (current.Edges.TryGetValue(c, out var shiftNode))
            {
                current = shiftNode;
                jumpCount += 1;

                if (IsAcceptable)
                {
                    Count += jumpCount;
                    jumpCount = 0;
                }



                return true;
            }
            else
            {
                IsWork = false;
                jumpCount = 0;
                return false;
            }
        }



        public void Reset()
        {
            IsWork = true;
            current = head;
            Count = 0;
            jumpCount = 0;
        }
    }

}
