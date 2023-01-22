using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Controls;

namespace VicTool.Main.Eth
{
    public static class Web3Extensions
    {
        public static async Task<decimal> GetBalance(this Web3 web3, Token token)
        {
            if (token.IsRaw)
            {
                
            }
            else
            {

            }

            return 1234;
        }

        private static Dictionary<string, Token> _tokens;
        public static async Task<Token> GenerateToken(this Web3 web3, string contract)
        {
            if (_tokens == null)
                _tokens = new Dictionary<string, Token>();

            if (_tokens.ContainsKey(contract))
                return _tokens[contract];

            var tokenData = await GetTokenData(web3, contract);
            var token = new Token(tokenData);

            _tokens.Add(contract, token);
            return token;
        }

        public static async Task<TokenData> GetTokenData(this Web3 web3, string contract)
        {

            var decimals = await web3.Eth.ERC20.GetContractService(contract).DecimalsQueryAsync();

            var name = await web3.Eth.ERC20.GetContractService(contract).NameQueryAsync();
            var ticker = await web3.Eth.ERC20.GetContractService(contract).SymbolQueryAsync();
            var ethPair = await GetPairContract(web3, contract, Global.Contracts.MainNet.WBNB);

            return new TokenData() { Name = name, Ticker = ticker, Decimals = decimals, Contract = contract, EthPairContract = ethPair };
        }

        public static async Task<string> GetPairContract(this Web3 web3, string tokenAContract,
            string tokenBContract)
        {
            var getPair = new GetPairFunction()
            {
                TokenA = tokenAContract,
                TokenB = tokenBContract
            };
            var getPairHandler = web3.Eth.GetContractQueryHandler<GetPairFunction>();

            var result = await getPairHandler.QueryAsync<string>(Global.Addresses.PanckeFactoryMainnet, getPair);
            return result;
        }

        public static async Task<List<decimal>> GetAmountsOut(this Web3 web3, decimal amountIn, List<Token> path)
        {
            var decimalsIn = path.First().Decimals;

            var message = new GetAmountsOutFunction()
            {
                AmountIn = Web3.Convert.ToWei(amountIn, decimalsIn),
                Path = path.Select(token => token.Contract).ToList()
            };

            var handler = web3.Eth.GetContractQueryHandler<GetAmountsOutFunction>();
            var amounts = await handler.QueryAsync<List<BigInteger>>(Global.Addresses.PancakeRouterMainnet, message);

            var decimalList = new List<decimal>();
            for (int i = 0; i < amounts.Count; i++)
            {
                decimalList.Add(Web3.Convert.FromWei(amounts[i], path[i].Decimals));
            }

            return decimalList;
        }

        public static async Task<List<decimal>> GetAmountsIn(this Web3 web3, decimal amountOut, List<Token> path)
        {
            var decimalsOut = path.Last().Decimals;

            var message = new GetAmountsInFunction()
            {
                AmountOut = Web3.Convert.ToWei(amountOut, decimalsOut),
                Path = path.Select(token => token.Contract).ToList()
            };

            var handler = web3.Eth.GetContractQueryHandler<GetAmountsInFunction>();
            var amounts = await handler.QueryAsync<List<BigInteger>>(Global.Addresses.PancakeRouterMainnet, message);

            var decimalList = new List<decimal>();
            for (int i = 0; i < amounts.Count; i++)
            {
                decimalList.Add(Web3.Convert.FromWei(amounts[i], path[i].Decimals));
            }

            return decimalList;
        }

        public static async Task<bool> SendAsync(this Web3 web3, Account account, string toAddress, Token token, decimal qty, int gasPrice)
        {
            if (token.IsRaw)
            {
                Com.WriteLine("Submitting transaction: Transfer " + qty + " " + token.Ticker + " to " +
                              toAddress);
                Com.WriteLine("Awaiting results...");
                var transactionReceipt = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(toAddress, qty, gasPrice);

                var success = transactionReceipt.Succeeded();
                Com.WriteLine("Transaction " + (success ? "Successful" : "Failed"));
                return success;
            }
            else
            {
                Com.WriteLine("Submitting transaction: Transfer " + qty + " " + token.Ticker + " from " + account.Address + " to " +
                              toAddress);
                Com.WriteLine("Awaiting results...");



                var transactionMessage = new TransferFunction
                {

                    To = toAddress,
                    Value = Web3.Convert.ToWei(qty, token.Decimals)

                };
                var transactionHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();

                var gasPriceWei = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);

                var nonce = await account.NonceService.GetNextNonceAsync();

                transactionMessage.Nonce = nonce;
                transactionMessage.GasPrice = gasPriceWei;
                var estimate = await transactionHandler.EstimateGasAsync(token.Contract, transactionMessage);
                transactionMessage.Gas = estimate.Value;

                var transferReceipt = await transactionHandler.SendRequestAndWaitForReceiptAsync(token.Contract, transactionMessage);

                var success = transferReceipt.Succeeded();

                Com.WriteLine("Transaction " + (success ? "Successful" : "Failed"));
                return success;
            }

        }

    }
}
