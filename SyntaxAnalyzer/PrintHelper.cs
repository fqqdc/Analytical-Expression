using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    public static class PrintHelper
    {
        public static string ToString<K, V>(this Dictionary<K, HashSet<V>> dict, string? rowHead = null) where K : notnull
        {
            StringBuilder builder = new StringBuilder();
            if (rowHead != null)
                builder.AppendLine(rowHead);
            foreach (var kp in dict)
            {
                builder.Append($"{kp.Key} => ");
                foreach (var t in kp.Value)
                {
                    builder.Append($"{t}, ");
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }
    }
}
