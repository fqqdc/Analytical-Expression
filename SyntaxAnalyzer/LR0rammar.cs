using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LR(0)文法
    /// </summary>
    public class LR0rammar : Grammar
    {
        private LR0rammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<(Symbol, Symbol), char> predictiveTable
            ) : base(allProduction, startNonTerminal)
        {

        }

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LR0rammar oPGrammar, [MaybeNullWhen(true)] out string errorMsg)
        {
            oPGrammar = null;
            errorMsg = null;
            StringBuilder sbErrorMsg = new();

            var (S, P) = (grammar.S, grammar.P);

            throw new NotImplementedException();
        }
    }
}
