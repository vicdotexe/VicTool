namespace VicTool.Main.Trash
{
    /*
    public struct SwapData
    {
        public bool IsExactOut { get; private set; }
        public Token TokenIn { get; private set; }
        public Token TokenOut { get; private set; }
        public decimal Slippage { get; private set; }
        public decimal UserInputAmount { get; private set; }
        public decimal TargetOutput { get; private set; }
        public decimal MinimumOutput { get; private set; }

        public decimal PriceImpact { get; private set; }
        public decimal PriceImpactUsd { get; private set; }


        public SwapData(decimal userInputAmount, Token tokenIn, Token tokenOut,  decimal slippage, bool isExactOut = false)
        {
            IsExactOut = isExactOut;
            TokenIn = tokenIn;
            TokenOut = tokenOut;
            Slippage = slippage;
            UserInputAmount = userInputAmount;

            if (!isExactOut)
            {
                var amountIn = userInputAmount;
                var priceImpact = TokenIn.PriceImpact(amountIn, TokenOut);
                var priceImpactUsd = (amountIn * TokenIn.GetValueInUsd(Web3Manager.EthPriceUsd)) * (priceImpact / 100);
                var targetOutput = TokenIn.GetAmountOut(amountIn, tokenOut);
                var minimumOutput = targetOutput * (1 - slippage);

                PriceImpact = priceImpact;
                TargetOutput = targetOutput;
                MinimumOutput = minimumOutput;
                PriceImpactUsd = priceImpactUsd;
            }
            else
            {
                var amountIn = userInputAmount;
                var priceImpact = TokenIn.PriceImpact(amountIn, TokenOut);
                var priceImpactUsd = (amountIn * TokenIn.GetValueInUsd(Web3Manager.EthPriceUsd)) * (priceImpact / 100);
                var targetOutput = TokenIn.GetAmountOut(amountIn, tokenOut);
                var minimumOutput = targetOutput * (1 - slippage);

                PriceImpact = priceImpact;
                TargetOutput = targetOutput;
                MinimumOutput = minimumOutput;
                PriceImpactUsd = priceImpactUsd;
            }
            
        }

        public string GetMinimumValue()
        {
            return MinimumOutput.ToString().Truncate(9) + " " + TokenOut.Ticker;
        }

        public string GetPriceImpactValue()
        {
            return PriceImpact + "%";//"(Investment Value: -$" + decimal.Round(PriceImpactUsd, 2) + " / " + TokenIn.Ticker + "'s Value:" + (TokenIn.GetValueInUsd(Web3Manager.EthPriceUsd) * PriceImpact)+") " + PriceImpact + "%";
        }
        public string GetInPerOutLabel()
        {
            return TokenIn.Ticker + " per " + TokenOut.Ticker + ":";
        }

        public string GetInPerOutValue()
        {
            return (UserInputAmount / TargetOutput).ToString().Truncate(6);
        }

        public string GetOutPerInLabel()
        {
            return TokenOut.Ticker + " per " + TokenIn.Ticker + ":";
        }

        public string GetOutPerInValue()
        {
            return (TargetOutput / UserInputAmount).ToString().Truncate(6);
        }
    }
    */
}
