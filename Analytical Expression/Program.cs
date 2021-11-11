using Analytical_Expression;
using System.Text;

var dctPrior = PriorityDictionary.Data;
string expression = @"aa+bbb*((c+d1)/ee22)";
CharacterSupplier cs = new(expression);

//TestCharacterSupplier_GetChar(cs);
//TestGetNextWord(cs);
Console.WriteLine(Analyze(expression));



string Analyze(string expression)
{
    Stack<Operand> stackOperand = new();
    Stack<string> stackOpt = new();

    Word word;

    while (TryGetWord(cs, out word))
    {
        // 操作数
        if (word.Type == WordType.Op)
        {
            stackOperand.Push(new(word.Content));
        }
        else
        {
            // 运算符
            string newOpt = word.Content;

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

                if (word.Content == ")")
                {
                    stackOpt.Pop(); // 去除多余的左括号
                }
                else if (word.Content == "#") // 结束操作符
                {
                    break;
                }
                else
                {
                    stackOpt.Push(newOpt); // 保存当前操作符
                }
            }
        }
    }

    return stackOperand.Pop().ToString();
}

bool TryGetWord(CharacterSupplier cs, out Word word)
{
    StringBuilder sbWord = new();

    bool isOp = false;
    char c;
    while (cs.TryGetChar(out c))
    {
        if (char.IsWhiteSpace(c)) continue;

        bool isDigitOrLetter = char.IsDigit(c) || char.IsLetter(c);

        if (sbWord.Length == 0 && isDigitOrLetter)
            isOp = true;

        if (isOp)
        {
            if (isDigitOrLetter)
                sbWord.Append(c);
            else
            {
                cs.Return();
                word = new(WordType.Op, sbWord.ToString());
                return true;
            }
        }
        else
        {
            if (!isDigitOrLetter)
            {
                sbWord.Append(c);
                if (!dctPrior.ContainsKey(sbWord.ToString()))
                {
                    cs.Return();
                    sbWord.Length += -1;
                    word = new(WordType.Opt, sbWord.ToString());
                    return true;
                }
            }
            else
            {
                cs.Return();
                word = new(WordType.Opt, sbWord.ToString());
                return true;
            }
        }
    }

    if (sbWord.Length > 0)
    {
        word = isOp ? new(WordType.Op, sbWord.ToString()) : new(WordType.Opt, sbWord.ToString());
        return true;
    }

    word = null;
    return false;
}

void TestCharacterSupplier_GetChar(CharacterSupplier cs)
{
    char c;
    while (cs.TryGetChar(out c))
    {
        Console.WriteLine(c);
    }
}

void TestGetNextWord(CharacterSupplier cs)
{
    Word word;
    while (TryGetWord(cs, out word))
    {
        Console.WriteLine($"{word.Type} {word.Content}");
    }
}