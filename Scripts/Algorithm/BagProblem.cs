using System;
using System.Collections.Generic;

/*
有 N
 件物品和一个容量是 V
 的背包。每件物品只能使用一次。

第 i
 件物品的体积是 vi
，价值是 wi
。

求解将哪些物品装入背包，可使这些物品的总体积不超过背包容量，且总价值最大。
输出最大价值。

输入格式
第一行两个整数，N，V
，用空格隔开，分别表示物品数量和背包容积。

接下来有 N
 行，每行两个整数 vi,wi
，用空格隔开，分别表示第 i
 件物品的体积和价值。

输出格式
输出一个整数，表示最大价值。 后面空格跟上选择的物品index

数据范围
0<N,V≤1000

0<vi,wi≤1000
*/

class Program
{
    static void Main()
    {
        string[] firstLine = Console.ReadLine().Split(' ');
        int N = int.Parse(firstLine[0]);
        int V = int.Parse(firstLine[1]);

        int[] volumes = new int[N];
        int[] values = new int[N];
        for (int i = 0; i < N; i++)
        {
            string[] line = Console.ReadLine().Split(' ');
            volumes[i] = int.Parse(line[0]);
            values[i] = int.Parse(line[1]);
        }

        // DP array with item tracking
        (int value, List<int> items)[] dp = new (int, List<int>)[V + 1];
        for (int j = 0; j <= V; j++)
            dp[j] = (0, new List<int>());

        // Fill DP
        for (int i = 0; i < N; i++)
        {
            for (int j = V; j >= volumes[i]; j--)
            {
                int valueWithout = dp[j].value;
                int valueWith = dp[j - volumes[i]].value + values[i];
                if (valueWith > valueWithout)
                {
                    var newItems = new List<int>(dp[j - volumes[i]].items) { i };
                    dp[j] = (valueWith, newItems);
                }
            }
        }

        // Output
        Console.Write(dp[V].value);
        if (dp[V].items.Count > 0)
            Console.Write(" " + string.Join(" ", dp[V].items));
    }
}