using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 简单优先文法
    /// </summary>
    public class SPGrammar : Grammar
    {
        private SPGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirst
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFollow
            ) : base(allProduction, startNonTerminal)
        {
            throw new NotImplementedException();
        }


        public static bool TryCreateSPGrammar(Grammar grammar, out SPGrammar sPGrammar, out string errorMsg)
        {
            throw new NotImplementedException();
        }


        #region 公共方法

        public static Symbol[] GetSymbolOrderArray(Grammar grammar)
        {
            return grammar.Vn.Cast<Symbol>().Union(grammar.Vt.Cast<Symbol>()).Distinct()
                .OrderBy(s => s != grammar.S)
                .ThenBy(s=>s is not NonTerminal)
                .ToArray() ;
        }

        #endregion
    }
}
