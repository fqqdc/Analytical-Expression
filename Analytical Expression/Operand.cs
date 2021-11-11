using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Operand
    {
        int type = 0;
        string strOperand;

        Operand fstOperand;
        string opt;
        Operand lstOperand;

        public Operand(string str)
        {
            this.strOperand = str;

            type = 1;
        }

        public Operand(Operand fst, string opt, Operand lst)
        {
            this.fstOperand = fst;
            this.opt = opt;
            this.lstOperand = lst;

            type = 2;
        }

        public override string ToString()
        {
            switch (type)
            {
                case 1:
                    return strOperand;
                case 2:
                    return $"OP[{fstOperand}{opt}{lstOperand}]";
                default:
                    return String.Empty;
            }


        }
    }
}
