namespace VicTool.Main.Trash
{
    /*
    public enum Reserves
    {
        A,
        B
    }

    public class LiquidityPool
    {
        public bool PoolValid => PairContract != "0x0000000000000000000000000000000000000000";
        public string PairContract { get; private set; }
        public TokenData TokenA { get; private set; }
        public TokenData TokenB { get; private set; }

        private decimal _reserveA;
        private decimal _reserveB;

        public LiquidityPool()
        {
            PairContract = "0x0000000000000000000000000000000000000000";
        }

        public LiquidityPool(TokenData tokenA, TokenData tokenB, string pairContract)
        {
            PairContract = pairContract;
            TokenA = tokenA;
            TokenB = tokenB;
            Update();
        }

        public void Update()
        {
            if (TokenA != null)
                _reserveA = WebManager.BSCQueryPool(PairContract, TokenA.Contract) / (decimal)Math.Pow(10, TokenA.Decimals);
            
            if (TokenB != null)
                _reserveB = WebManager.BSCQueryPool(PairContract, TokenB.Contract) / (decimal)Math.Pow(10, TokenB.Decimals);
        }

        public void UpdatePrices()
        {
           
        }

        public decimal GetReserve(Reserves reserve)
        {
            return reserve == Reserves.A ? _reserveA : _reserveB;
        }

        public decimal GetOpposingReserve(Reserves reserve)
        {
            return reserve == Reserves.A ? _reserveB : _reserveA;
        }

        public decimal Quote(decimal amount, Reserves reserve)
        {
            var resA = GetReserve(reserve);
            var resB = GetOpposingReserve(reserve);
            return ((amount) * resB) / resA;
        }

        public decimal GetAmountOut(decimal amountIn, Reserves reserveIn)
        {
            var resIn = GetReserve(reserveIn);
            var resOut = GetOpposingReserve(reserveIn);
            return LiquidityPool.GetAmountOut(amountIn, resIn, resOut);
        }

        public decimal PriceImpact(decimal amountIn, Reserves reserveIn)
        {
            var impact =  1 - (GetAmountOut(amountIn, reserveIn) / Quote(amountIn, reserveIn));
            return decimal.Round(impact * 100, 2);
        }

        public static decimal GetAmountOut(decimal amountIn, decimal reserveIn, decimal reserveOut)
        {
            var amountInWithFee = amountIn * 0.9975m;
            var numerator = amountInWithFee * reserveOut;
            var denominator = reserveIn + amountInWithFee;
            var amountOut = numerator / denominator;
            return amountOut;
        }

        public static decimal GetAmountIn(decimal amountOut, decimal reserveIn, decimal reserveOut)
        {
            var numerator = reserveIn * amountOut;
            var denominator = (reserveOut - amountOut) * 0.9975m;
            var amountIn = numerator / denominator;
            return amountIn;
        }

        public static decimal Quote(decimal amount, decimal reserveIn, decimal reserveOut)
        {
            return (amount * reserveOut) / reserveIn;
        }

    }
    */
}
