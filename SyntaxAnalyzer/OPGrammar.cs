using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 算法优先文法
    /// </summary>
    public class OPGrammar : Grammar
    {
        private OPGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirst
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFollow
            ) : base(allProduction, startNonTerminal)
        {
            throw new NotImplementedException();
        }


        public static bool TryCreateSPGrammar(Grammar grammar, out OPGrammar oPGrammar, out string errorMsg)
        {
            throw new NotImplementedException();
        }

    }
}
