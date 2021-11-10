using Analytical_Expression;
using System.Text;

var dctPrior = PriorityDictionary.Data;


string expression = @"num1<34";
Console.WriteLine(Analyze(expression));

string Analyze(string expression)
{
    var sb = new StringBuilder().Append(expression).Append("#");
    var sbOp = new StringBuilder();
    var sbOpt = new StringBuilder();

    Stack<Operand> stackOperand = new();
    Stack<string> stackOpt = new();

    while (sb.Length != 0)
    {
        char c = sb[0];
        sb.Remove(0, 1);

        if (char.IsWhiteSpace(c))
            continue;

        if (char.IsNumber(c) || char.IsLetter(c)) // 是否是字母和数字
        { // 是
            // 判断运算符是否非法
            if (sbOpt.Length != 0)
            {
                throw new NotSupportedException($"\"{sbOpt.ToString()}\"是非法运算符");
            }

            sbOp.Append(c); // 添加字符到操作数
            continue;
        }
        else
        { // 否
            // 生成操作数
            if (sbOp.Length != 0) // 连续的操作符会导致操作数为空：1 *( 2+3)
            {
                stackOperand.Push(new(sbOp.ToString()));
                sbOp = new();
            }

            // 是否构成合法的运算符
            sbOpt.Append(c); // 添加字符到运算符
            if (!dctPrior.ContainsKey(sbOpt.ToString()))
            { // 否
                continue;
            }
        }

        // 生成运算符
        string newOpt = sbOpt.ToString();
        sbOpt = new();

        if (stackOpt.Count == 0
            || dctPrior[stackOpt.Peek()][newOpt] > 0 // 高优先级跳过
            )
        {
            stackOpt.Push(newOpt); // 保存当前操作符
            continue;
        }

        // 低优先级：计算之前的表达式
        var lstOp = stackOperand.Pop(); // 获取第二操作数
        do
        {
            var opt = stackOpt.Pop(); // 操作符
            var fstOp = stackOperand.Pop(); // 获取第一操作数            
            lstOp = new Operand(fstOp, opt, lstOp); // 构造新操作数

        } while (stackOpt.Count > 0 && dctPrior[stackOpt.Peek()][newOpt] < 0);

        stackOperand.Push(lstOp); // 保存新操作数

        if (c == ')')
        {
            stackOpt.Pop(); // 去除多余的左括号
        }
        else if (c == '#') // 表达式技术操作符
        {
            break;
        }
        else
        {
            stackOpt.Push(newOpt); // 保存当前操作符
        }
    }

    return stackOperand.Pop().ToString();
}

class Operand
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

