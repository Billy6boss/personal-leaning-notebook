namespace Stage1.Exercises.Ex05_Collections;

/// <summary>
/// Stage 1 主題：Collections（集合）。
/// </summary>
public static class TextAnalyzer
{
    /// <summary>
    /// 計算 <paramref name="text"/> 中每個單字出現的次數，
    /// 不分大小寫（case-insensitively），並忽略標點符號
    /// （只有字母與數字才算是單字的一部分）。請用
    /// Dictionary&lt;string, int&gt; 來累計次數。
    /// 範例："Cat, cat! Dog." -> { "cat": 2, "dog": 1 }
    /// </summary>
    public static IDictionary<string, int> CountWordFrequency(string text)
    {
        var wordList = text.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '(', ')', '[', ']', '{', '}', '"' }, StringSplitOptions.RemoveEmptyEntries ).Select(w => w.ToLower());
        var result = new Dictionary<string, int>();

        foreach (var word in wordList)
        {
            if(result.ContainsKey(word))
            {
                result[word]++;
            }
            else
            {
                result[word] = 1;
            }
        }

        return result;
    }

    /// <summary>
    /// 如果 <paramref name="expression"/> 裡的每個括號都有正確配對且
    /// 巢狀（nested）關係正確 — 包含 ()、[]、{} 這三種括號 —
    /// 就回傳 true。請用 Stack&lt;char&gt; 來實作。
    /// 範例："([{}])" -> true，"([)]" -> false，"(()" -> false。
    /// </summary>
    public static bool IsBalanced(string expression)
    {
        var stack = new Stack<char>();
        var expressionCharList = expression.ToCharArray().ToList();

        foreach (var ch in expressionCharList)
        {
            if (ch == '(' || ch == '[' || ch == '{')
            {
                stack.Push(ch);
            }

            switch (ch)
            {
                case ')':
                    if (stack.Count == 0 || stack.Pop() != '(') return false;
                    break;
                case ']':
                    if (stack.Count == 0 || stack.Pop() != '[') return false;
                    break;
                case '}':
                    if (stack.Count == 0 || stack.Pop() != '{') return false;
                    break;
            }

        }
        return stack.Count == 0;

    }

    /// <summary>
    /// 回傳 <paramref name="items"/> 中出現超過一次的項目，
    /// 結果中每個項目只會出現一次，並依「第一次被看到」的順序排列。
    /// 請用 HashSet&lt;T&gt;（或類似的 collection）來追蹤哪些項目已經
    /// 看過。
    /// 範例：[1, 2, 3, 2, 1, 4] -> [1, 2]
    /// </summary>
    public static IReadOnlyList<T> FindDuplicates<T>(IEnumerable<T> items) where T : notnull
    {
        var temp = new HashSet<T>();
        var resultList = new List<T>();
        foreach (var item in items)
        {
            if (temp.TryGetValue(item, out var result))
            { 
                resultList.Add(result);
            }
            else
            {
                temp.Add(item);
            }
        }
        
        return resultList;
    }
}
