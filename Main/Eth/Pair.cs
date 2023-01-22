using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using VicTool.Controls;

namespace VicTool.Main.Eth
{
    public enum TokenIndex
    {
        A,
        B
    }
    public class LiquidityPair
    {
        public static decimal DexFee { get; set; } = 0.0025m;

        private Web3DexInfo _dexInfo;
        public Web3Token TokenA { get; private set; }
        public Web3Token TokenB { get; private set; }

        public decimal TokenAReserves { get; private set; }
        public decimal TokenBReserves { get; private set; }

        public string PairContract { get; private set; }
        public bool HasLiquidity { get; private set; }
        public bool IsInitialized { get; private set; }

        public LiquidityPair(Web3Token tokenA, Web3Token tokenB, Web3DexInfo dexInfo)
        {
            TokenA = tokenA;
            TokenB = tokenB;
            _dexInfo = dexInfo;
        }

        public async Task Initialize(Web3 web3)
        {
            PairContract = await GetPairContract(web3);
            if (PairContract != Global.NullAddress)
                HasLiquidity = true;
            if (!HasLiquidity)
            {

            }
            await RefreshReserves(web3);
            IsInitialized = true;
        }

        public async Task RefreshReserves(Web3 web3)
        {
            var balance = await web3.Eth.ERC20.GetContractService(TokenA.Contract)
                .BalanceOfQueryAsync(PairContract);
            TokenAReserves = Web3.Convert.FromWei(balance, TokenA.Decimals);

            balance = await web3.Eth.ERC20.GetContractService(TokenB.Contract)
                .BalanceOfQueryAsync(PairContract);
            TokenBReserves = Web3.Convert.FromWei(balance, TokenB.Decimals);
        }

        private async Task<string> GetPairContract(Web3 web3)
        {
            var getPair = new GetPairFunction()
            {
                TokenA = TokenA.Contract,
                TokenB = TokenB.Contract
            };
            var getPairHandler = web3.Eth.GetContractQueryHandler<GetPairFunction>();

            var result = await getPairHandler.QueryAsync<string>(_dexInfo.Factory, getPair);
            return result;
        }

        public decimal GetAmountOut(decimal amountIn, Web3Token tokenIn)
        {
            if (tokenIn.Contract == TokenA.Contract)
                return GetAmountOut(amountIn, TokenAReserves, TokenBReserves);
            else if(tokenIn.Contract == TokenB.Contract)
            {
                return GetAmountOut(amountIn, TokenBReserves, TokenAReserves);
            }

            throw new NotImplementedException("Shouldn't arrive here.");
        }

        public decimal GetAmountIn(decimal amountOut, Web3Token tokenIn)
        {
            if (tokenIn.Contract == TokenA.Contract)
                return GetAmountIn(amountOut, TokenAReserves, TokenBReserves);
            else if (tokenIn.Contract == TokenB.Contract)
            {
                return GetAmountIn(amountOut, TokenBReserves, TokenAReserves);
            }

            throw new NotImplementedException("Shouldn't arrive here.");
        }

        public decimal Quote(decimal amount, Web3Token token)
        {
            if (token.Contract == TokenA.Contract)
            {
                return ((amount) * TokenBReserves)/TokenAReserves;
            }
            else if (token.Contract == TokenB.Contract)
            {
                return ((amount) * TokenAReserves) / TokenBReserves;
            }
            throw new NotImplementedException("Shouldn't arrive here.");
        }

        public static decimal GetAmountIn(decimal amountOut, decimal reserveIn, decimal reserveOut)
        {
            var numerator = reserveIn * amountOut;
            var denominator = (reserveOut - amountOut) * (1-DexFee);
            var amountIn = numerator / denominator;
            return amountIn;
        }

        public static decimal Quote(decimal amount, decimal reserveIn, decimal reserveOut)
        {
            return (amount * reserveOut) / reserveIn;
        }

        public static decimal GetAmountOut(decimal amountIn, decimal reserveIn, decimal reserveOut)
        {
            var amountInWithFee = amountIn * (1-DexFee);
            var numerator = amountInWithFee * reserveOut;
            var denominator = reserveIn + amountInWithFee;
            var amountOut = numerator / denominator;
            return amountOut;
        }
    }
}
