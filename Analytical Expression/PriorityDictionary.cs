using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    internal static class PriorityDictionary
    {
        private readonly static Dictionary<string, Dictionary<string, int>> dctPriority = new();
        static PriorityDictionary()
        {
            SetPriority("#");
            SetPriority("+", ("+", -1), ("-", -1), ("*", +1), ("/", +1), ("(", +1), (")", -1), ("<", -1), (">", -1), ("#", -1));
            SetPriority("-", ("+", -1), ("-", -1), ("*", +1), ("/", +1), ("(", +1), (")", -1), ("<", -1), (">", -1), ("#", -1));
            SetPriority("*", ("+", -1), ("-", -1), ("*", -1), ("/", -1), ("(", +1), (")", -1), ("<", -1), (">", -1), ("#", -1));
            SetPriority("/", ("+", -1), ("-", -1), ("*", -1), ("/", -1), ("(", +1), (")", -1), ("<", -1), (">", -1), ("#", -1));
            SetPriority("(", ("+", +1), ("-", +1), ("*", +1), ("/", +1), ("(", +1), (")", +1), ("<", +1), (">", +1), ("#", -1));
            SetPriority(")", ("+", -1), ("-", -1), ("*", -1), ("/", -1), ("(", -1), (")", -1), ("<", -1), (">", -1), ("#", -1));
            SetPriority("<", ("+", +1), ("-", +1), ("*", +1), ("/", +1), ("(", +1), (")", -1), ("<", +1), (">", -1), ("#", -1));
            SetPriority(">", ("+", +1), ("-", +1), ("*", +1), ("/", +1), ("(", +1), (")", -1), ("<", -1), (">", +1), ("#", -1));
        }

        static void SetPriority(string fst, params (string lst, int prior)[] arrParam)
        {
            dctPriority[fst] = new();
            foreach (var item in arrParam)
            {
                dctPriority[fst][item.lst] = item.prior;
            }
        }

        public static Dictionary<string, Dictionary<string, int>> Data
        {
            get
            {
                return dctPriority;
            }
        }
    }
}
