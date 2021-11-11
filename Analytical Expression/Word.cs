using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Word
    {
        public WordType Type { get; init; }
        public string Content { get; init; }

        public Word(WordType type, string content)
        {
            this.Type = type;
            this.Content = content;
        }
    }
}
