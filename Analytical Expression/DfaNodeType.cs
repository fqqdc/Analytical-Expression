using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public enum DfaNodeType : int
    {
        Unacceptable = 0b000,
        Start = 0b001,
        Acceptable = 0b010,

    }
}
