using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    public abstract class SyntaxAnalyzer
    {
        public delegate void AdvanceProcedure(out Terminal Sym);
    }
}
