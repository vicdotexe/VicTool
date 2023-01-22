using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VicTool.Main.Eth;

namespace VicTool.Main.Swap
{
    public static class SwapMath
    {
        private static List<decimal> _tempAmtsOut = new List<decimal>();

        public static List<decimal> GetAmountsOut(decimal amountIn, List<Token> path, List<decimal> injectionList)
        {
            injectionList.Clear();
            var amt = amountIn;
            for (int i = 0; i < path.Count - 1; i++)
            {
                amt = Token.GetAmountOut(amt, path[i], path[i + 1]);
                injectionList.Add(amt);
            }

            return injectionList;
        }

        public static List<decimal> GetAmountsIn(decimal amountOut, List<Token> path, List<decimal> injectionList)
        {
            injectionList.Clear();
            var amt = amountOut;

            for (int i = path.Count - 1; i > 0; i--)
            {
                amt = Token.GetAmountIn(amt, path[i - 1], path[i]);
                injectionList.Add(amt);
            }

            return injectionList;
        }
    }
}
