using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class CharacterSupplier
    {
        public char END_OF_CHAR = '#';

        StringBuilder sbExpression;
        int index = 0;
        public CharacterSupplier(string expression)
        {
            sbExpression = new StringBuilder(expression).Append(END_OF_CHAR);
        }

        public bool TryGetChar(out char c)
        {
            if (index == sbExpression.Length)
            {
                c = END_OF_CHAR;
                return false;
            }
            else
            {
                c = sbExpression[index++];
                return true;
            }
        }

        public void Return()
        {
            if (index > 0)
                index--;
        }
    }
}
