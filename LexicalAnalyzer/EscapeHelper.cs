using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public static class EscapeHelper
    {
        public static string Escape(this char c)
        {
            switch (c)
            {
                case '\0': return "\\0";
                case '\r': return "\\r";
                case '\n': return "\\n";
                case ' ': return "\\s";
                case '\t': return "\\t";
                default: return c.ToString();
            }
        }

        public static string Escape(this string str)
        {
            StringBuilder builder = new();
            foreach (char c in str)
            {
                builder.Append(c.Escape());
            }
            return builder.ToString();
        }
    }
}
