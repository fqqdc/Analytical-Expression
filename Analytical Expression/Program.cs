using Analytical_Expression;
using System.Text;

var dctPrior = PriorityDictionary.Data;


string expression = @"aa+bbb*((c+d1)/ee22)";
Console.WriteLine(Analyze(expression));
//string? word;
//int index = 0;
//var wordType = GetNextWord(new(expression), ref index, out word);
//while (wordType != Type.EOS)
//{
//    Console.WriteLine($"{word} {wordType}");
//    wordType = GetNextWord(new(expression), ref index, out word);
//}


string Analyze(string expression)
{
    var sb = new StringBuilder(expression).Append("#");
    var sbOp = new StringBuilder();
    var sbOpt = new StringBuilder();

    Stack<Operand> stackOperand = new();
    Stack<string> stackOpt = new();

    string? word;
    int index = 0;
    var wordType = GetNextWord(sb, ref index, out word);
    while (wordType != Type.EOS)
    {
        // 操作数
        if (wordType == Type.Op)
        {
            stackOperand.Push(new(word));
        }
        else
        {
            // 运算符
            string newOpt = word;
            sbOpt = new();

            if (stackOpt.Count == 0
                || dctPrior[stackOpt.Peek()][newOpt] > 0 // 高优先级跳过
                )
            {
                stackOpt.Push(newOpt); // 保存当前操作符
            }
            else
            {
                // 低优先级：计算之前的表达式
                var lstOp = stackOperand.Pop(); // 获取第二操作数
                do
                {
                    var opt = stackOpt.Pop(); // 操作符
                    var fstOp = stackOperand.Pop(); // 获取第一操作数            
                    lstOp = new Operand(fstOp, opt, lstOp); // 构造新操作数

                } while (stackOpt.Count > 0 && dctPrior[stackOpt.Peek()][newOpt] < 0);

                stackOperand.Push(lstOp); // 保存新操作数

                if (word == ")")
                {
                    stackOpt.Pop(); // 去除多余的左括号
                }
                else if (word == "#") // 结束操作符
                {
                    break;
                }
                else
                {
                    stackOpt.Push(newOpt); // 保存当前操作符
                }
            }
        }

        wordType = GetNextWord(sb, ref index, out word);
    }

    return stackOperand.Pop().ToString();
}

Type GetNextWord(StringBuilder expression, ref int index, out string? word)
{
    StringBuilder sb = new();

    bool isOp = false;
    while (index < expression.Length)
    {
        char c = expression[index];
        if (char.IsWhiteSpace(c)) continue;

        bool isDigitOrLetter = char.IsDigit(c) || char.IsLetter(c);

        if (sb.Length == 0 && isDigitOrLetter)
            isOp = true;

        if (isOp)
        {
            if (isDigitOrLetter)
                sb.Append(c);
            else
            {
                word = sb.ToString();
                return Type.Op;
            }
        }
        else
        {
            if (!isDigitOrLetter)
            {
                sb.Append(c);
                if (!dctPrior.ContainsKey(sb.ToString()))
                {
                    sb.Length += -1;
                    word = sb.ToString();
                    return Type.Opt;
                }
            }
            else
            {
                word = sb.ToString();
                return Type.Opt;
            }

            
        }

        index++;
    }

    if (sb.Length > 0)
    {
        word = sb.ToString();
        return isOp ? Type.Op : Type.Opt;
    }

    word = null;
    return Type.EOS;
}

enum Type
{
    Op,
    Opt,
    EOS,
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

