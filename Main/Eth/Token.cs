using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Web3;
using Org.BouncyCastle.Asn1.Sec;

namespace VicTool.Main.Eth
{

    public class Token
    {
        public string Contract { get; private set; }
        public string Ticker { get; private set; }
        public string Name { get; private set; }
        public int Decimals { get; private set; }

        public decimal GetValueInUsd()
        {
            if (IsSetUp || IsRaw)
                return IsRaw ? Core.Kucoin.GetPrice("BNB") : GetValueInEth() * Core.Kucoin.GetPrice("BNB");
            return 1234;
        }

        public decimal QtyAtUsdPriceOrBetter(decimal usdPrice, decimal currentEthUsdValue)
        {
            if (usdPrice <= 0)
                return 1234;
            var finalEthValue = usdPrice / currentEthUsdValue; 
            var delta = (EthReserves / finalEthValue) - TokenReserves;
            return delta;
        }

        
        public decimal GetValueInEth()
        {
            if (IsSetUp || IsRaw)
                return IsRaw ? 1 : (EthReserves / TokenReserves);
            return 1234;
        }

        public string EthPairContract { get; private set; }
        public decimal TokenReserves { get; private set; }
        public decimal EthReserves { get; private set; }
        public bool IsRaw { get; private set; }
        public bool IsWrappedRaw { get; private set; }
        public bool IsSetUp { get; private set; }

        public Token(TokenData data)
        {
            Contract = data.Contract;
            Ticker = data.Ticker;
            Name = data.Name;
            Decimals = data.Decimals;
            EthPairContract = data.EthPairContract;
            IsRaw = data.Contract == null;
        }

        
        public async Task Update(Web3 _web3)
        {
            if (IsRaw)
            {
                IsSetUp = true;
                return;
            }

            var balance = await _web3.Eth.ERC20.GetContractService(Contract)
                .BalanceOfQueryAsync(EthPairContract);
            TokenReserves = Web3.Convert.FromWei(balance, Decimals);
            balance = await _web3.Eth.ERC20.GetContractService(Global.Contracts.MainNet.WBNB)
                .BalanceOfQueryAsync(EthPairContract);
            EthReserves = Web3.Convert.FromWei(balance, 18);
            IsSetUp = true;
        }

        public decimal Quote(decimal amount, Token token = null)
        {
            if (!this.IsRaw)
            {
                if (token != null && !token.IsRaw)
                {
                    var ethOutA = Quote(amount);
                    var tokenOutB = Quote(ethOutA, token.EthReserves, token.TokenReserves);
                    return tokenOutB;
                }
                var resA = TokenReserves;
                var resB = EthReserves;
                return ((amount) * resB) / resA;
            }
            else
            {

                var amt = Quote(amount, token.EthReserves, token.TokenReserves);
                return amt;
            }


        }


        public decimal GetAmountOut(decimal amountIn, Token tokenOut = null)
        {

            if (!this.IsRaw)
            {
                if (tokenOut != null && !tokenOut.IsRaw)
                {
                    var ethOut = GetAmountOut(amountIn);
                    return Token.GetAmountOut(ethOut, tokenOut.EthReserves, tokenOut.TokenReserves);
                }
                return GetAmountOut(amountIn, TokenReserves, EthReserves);
            }

            return GetAmountOut(amountIn, tokenOut.EthReserves, tokenOut.TokenReserves);
        }


        public decimal PriceImpact(decimal amountIn, Token tokenOut = null)
        {
            if (!this.IsRaw)
            {
                if (tokenOut != null && !tokenOut.IsRaw)
                {
                    var imp = 1 - (GetAmountOut(amountIn, tokenOut) / Quote(amountIn, tokenOut));
                    return decimal.Round(imp * 100, 2);
                }

                var impact = 1 - (GetAmountOut(amountIn) / Quote(amountIn));
                return decimal.Round(impact * 100, 2);
            }

            var quote = Quote(amountIn, tokenOut);
            return tokenOut.PriceImpact(quote);
        }


        public static decimal GetAmountOut(decimal amountIn, decimal reserveIn, decimal reserveOut)
        {
            var amountInWithFee = amountIn * 0.9975m;
            var numerator = amountInWithFee * reserveOut;
            var denominator = reserveIn + amountInWithFee;
            var amountOut = numerator / denominator;
            return amountOut;
        }

        public static decimal GetAmountOut(decimal amountIn, Token tokenIn, Token tokenOut)
        {
            decimal outReserves = 0;
            decimal inReserves = 0;

            if (tokenIn.IsRaw)
            {
                outReserves = tokenOut.TokenReserves;
                inReserves = tokenOut.EthReserves;
            }
            else if (tokenOut.IsRaw)
            {
                outReserves = tokenIn.EthReserves;
                inReserves = tokenIn.TokenReserves;
            }
            else
            {
                throw new NotSupportedException(
                    "'GetAmountOut' only used for direct Eth swaps... Must used 'GetAmountsOut' for token to token swaps.");
            }

            return GetAmountOut(amountIn, inReserves, outReserves);



            if (tokenIn.IsRaw)
            {
                return GetAmountIn(amountIn, tokenOut.TokenReserves, tokenOut.EthReserves);
            }
            else if (tokenOut.IsRaw)
            {
                return GetAmountOut(amountIn, tokenIn.TokenReserves, tokenIn.EthReserves);
            }
            else
            {
                throw new NotSupportedException(
                    "'GetAmountOut' only used for direct Eth swaps... Must used 'GetAmountsOut' for token to token swaps.");
            }
        }

        public static decimal GetAmountIn(decimal amountOut, Token tokenIn, Token tokenOut)
        {
            decimal outReserves = 0;
            decimal inReserves = 0;

            if (tokenIn.IsRaw)
            {
                outReserves = tokenOut.TokenReserves;
                inReserves = tokenOut.EthReserves;
            }
            else if (tokenOut.IsRaw)
            {
                outReserves = tokenIn.EthReserves;
                inReserves = tokenIn.TokenReserves;
            }
            else
            {
                throw new NotSupportedException(
                    "'GetAmountOut' only used for direct Eth swaps... Must used 'GetAmountsOut' for token to token swaps.");
            }

            return GetAmountIn(amountOut, inReserves, outReserves);

            if (tokenIn.IsRaw)
            {
                return GetAmountIn(amountOut, tokenOut.EthReserves, tokenOut.TokenReserves);
            }
            else if (tokenOut.IsRaw)
            {
                return GetAmountIn(amountOut, tokenIn.TokenReserves, tokenIn.EthReserves);
            }
            else
            {
                throw new NotSupportedException(
                    "'GetAmountOut' only used for direct Eth swaps... Must used 'GetAmountsOut' for token to token swaps.");
            }
        }

        /*
        public static List<decimal> GetAmountsOut(decimal amountIn, List<Token> swapRoute)
        {
            
            List<decimal> _amountsOut = new List<decimal>();
            for (int i = 0; i < swapRoute.Count -1; i++)
            {
                var currentIn = _amountsOut.Count == 0 ? amountIn : _amountsOut[i-1];
                var currentToken = swapRoute[i];
                var nextToken = swapRoute[i + 1];
                var nextOut = GetAmountOut(currentIn, cu)
            }
            
        }
    */
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

        private static List<decimal> _tempQuoteList = new List<decimal>();
        public static List<decimal> QuoteChain(decimal amount, List<Token> tokens)
        {
            _tempQuoteList.Clear();
            var amt = amount;
            List<decimal> quotes = new List<decimal>();

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                amt = tokens[i].Quote(amt, tokens[i + 1]);
                _tempQuoteList.Add(amt);
            }

            return _tempQuoteList;
        }

        public static List<decimal> QuoteChainReverse(decimal amount, List<Token> tokens)
        {
            _tempQuoteList.Clear();
            var amt = amount;
            List<decimal> quotes = new List<decimal>();

            for (int i = tokens.Count - 1; i > 0; i--)
            {
                amt = tokens[i].Quote(amt, tokens[i - 1]);
                _tempQuoteList.Add(amt);
            }

            return _tempQuoteList;
        }

    }
}
