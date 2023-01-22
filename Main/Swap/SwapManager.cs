using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Threading;
using Kucoin.Net.Clients;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Controls;
using VicTool.Main.Eth;
using VicTool.Main.Kucoin;

namespace VicTool.Main.Swap
{
    public class SwapManager
    {
        public void SetTokenIn(string contract)
        {
            if (_nextTokenIn == contract)
                return;
            _nextTokenIn = contract;
            _tokenInChanged = true;
        }
        public void SetTokenOut(string contract)
        {
            if (_nextTokenOut == contract)
                return;
            _nextTokenOut = contract;
            _tokenOutChanged = true;
        }

        public void Flip()
        {
            (_tokenIn, _tokenOut) = (_tokenOut, _tokenIn);
            (_nextTokenIn, _nextTokenOut) = (_nextTokenOut, _nextTokenIn);
        }
        
        public Token TokenIn => _tokenIn;
        public Token TokenOut => _tokenOut;
        private Token _tokenIn;
        private Token _tokenOut;
        private Token _wEthToken;

        private string _nextTokenIn;
        private string _nextTokenOut;
        
        public decimal LastTokenInBalance => _lastTokenInBalance;
        public decimal LastTokenOutBalance => _lastTokenOutBalance;

        public bool IsPairValid => GetPairValidity();
        public bool IsPairSetup => PairIsSetup();
        public bool CanCalculate => IsPairValid && IsPairSetup;
        public double RefreshTime { get; private set; }

        private decimal _lastTokenInBalance;
        private decimal _lastTokenOutBalance;

        private double _updateInterval = 1000;
        private bool _tokenInChanged;
        private bool _tokenOutChanged;

        public void SetAmountIn(decimal value)
        {
            _amountIn = value;
            _sendExactTokens = true;
            _receiveExactTokens = false;
        }

        public void SetAmountOut(decimal value)
        {
            _amountOut = value;
            _receiveExactTokens = true;
            _sendExactTokens = false;
        }

        public decimal AmountIn => _amountIn;
        public decimal AmountOut => _amountOut;
        private decimal _amountIn;
        private decimal _amountOut;
        private bool _sendExactTokens = true;
        private bool _receiveExactTokens = false;
        
        public List<Token> LastSwapChain => _tempPath;
        private List<Token> _tempPath = new List<Token>();

        public SwapManager()
        {
            Core.Web3.Emitter.AddObserver(Web3Events.NetworkConnected,GetWrappedNetworkToken);
            Task.Factory.StartNew(Web3Loop);
            Task.Factory.StartNew(CalculateLoop);
        }

        private async Task CalculateLoop()
        {
            while (true)
            {
                CalculateSummary();
                await Task.Delay(100);
            }
        }

        private void GetWrappedNetworkToken()
        {
            Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
            {
                _wEthToken = await Core.Web3.Client.GenerateToken(Global.Contracts.MainNet.WBNB);
            });

        }

        public async Task<TransactionReceipt> PerformSwap(int gasPrice = 5, decimal slippage = 0.005m, string toAddress = null)
        {

            var addressValid = Core.Web3.Account.Address != toAddress && Web3.IsChecksumAddress(toAddress) && toAddress != null;

            if (_tokenIn.IsRaw && _sendExactTokens)
            {
                var receipt = await SwapExactEthForTokens(_amountIn, _amountOut, LastSwapChain, slippage, gasPrice);
                Com.WriteLine("Swap " + (receipt.Succeeded() ? "Succeeded" : "Failed"));
                return receipt;
            }

            if (_tokenOut.IsRaw && _sendExactTokens)
            {
                var ensureAmt = _amountOut;
                var ensureAmtIn = _amountIn;
                var receipt = await SwapExactTokensForEth(_amountIn, _amountOut, LastSwapChain, slippage, gasPrice, toAddress);
                Com.WriteLine("Swap " + (receipt.Succeeded() ? "Succeeded" : "Failed"));
                
                if (receipt.Succeeded() && addressValid)
                {
                    var gasPriceWei = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);
                    var gasFeeInWei = gasPriceWei * Web3.Convert.ToWei(receipt.GasUsed, UnitConversion.EthUnit.Wei);
                    var gasFeeInEth = Web3.Convert.FromWei(gasFeeInWei, UnitConversion.EthUnit.Ether);
                    Com.WriteLine("Gas used " + gasFeeInEth + " BNB");
                    Com.WriteLine("Routing minimum output to: " + toAddress);

                    var amountToSend = (ensureAmt * (1 - slippage)) - gasFeeInEth;
                    var success = await Core.Web3.Client.SendAsync(Core.Web3.Account, toAddress,LastSwapChain.Last(), amountToSend, 5);
                    if (success)
                    {
                        var arb = new SellDexBuyCex()
                        {
                            EthQtySentToCex = amountToSend,
                            TokenQtySold = ensureAmtIn,
                            TokenTicker = LastSwapChain.First().Ticker
                        };
                        KucoinManager.HandleArbitrage(arb);
                    }
                }

                return receipt;
            }

            return null;
        }

        #region Swap Functions
        async public Task<TransactionReceipt> SwapExactEthForTokens(decimal amountInEth, decimal amountTokensOutMin, List<Token> path, decimal slippage = 0.005m, int gasPrice = 5, double deadlineMinutes = 5)
        {
            var swapHandler = Core.Web3.Client.Eth.GetContractTransactionHandler<SwapExactETHForTokensFunction>();
            var swap = new SwapExactETHForTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(amountInEth),
                AmountOutMin = Web3.Convert.ToWei(amountTokensOutMin*(1-slippage), path.Last().Decimals),
                Path = new List<string>(){ Global.Contracts.MainNet.WBNB, path.Last().Contract},
                To = Core.Web3.Account.Address,
                Deadline = DateTimeOffset.Now.AddMinutes(deadlineMinutes).ToUnixTimeSeconds(),

            };
            var gasprice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);
            var estimate = await swapHandler.EstimateGasAsync(Global.Addresses.PancakeRouterMainnet, swap);
            var nonce = await Core.Web3.Account.NonceService.GetNextNonceAsync();

            swap.Nonce = nonce;
            swap.GasPrice = gasprice;
            swap.Gas = estimate.Value;

            Com.WriteLine("Signing Transaction - Nonce:" + swap.Nonce);
            await swapHandler.SignTransactionAsync(Global.Addresses.PancakeRouterMainnet, swap);

            Com.WriteLine("Sending Transaction - Nonce:" + swap.Nonce);
            var receipt =
                await swapHandler.SendRequestAndWaitForReceiptAsync(Global.Addresses.PancakeRouterMainnet, swap);

            return receipt;
        }

        async public Task<TransactionReceipt> SwapExactTokensForEth(decimal amountInTokens, decimal amountEthOutMin, List<Token> path, decimal slippage = 0.005m, int gasPrice = 5, string toAddress = null, double deadlineMinutes = 5)
        {

            var swapHandler = Core.Web3.Client.Eth.GetContractTransactionHandler<SwapExactTokensForETHFunction>();
            var swap = new SwapExactTokensForETHFunction()
            {
                AmountIn = Web3.Convert.ToWei(amountInTokens,path.First().Decimals),
                AmountOutMin = Web3.Convert.ToWei(amountEthOutMin),
                Path = new List<string>() {path.First().Contract , Global.Contracts.MainNet.WBNB },
                To = Core.Web3.Account.Address,
                Deadline = DateTimeOffset.Now.AddMinutes(deadlineMinutes).ToUnixTimeSeconds()
            };
            
            var gasprice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);
            
            var nonce = await Core.Web3.Account.NonceService.GetNextNonceAsync();

            swap.Nonce = nonce;
            swap.GasPrice = gasprice;
            var estimate = await swapHandler.EstimateGasAsync(Global.Addresses.PancakeRouterMainnet, swap);
            swap.Gas = estimate.Value;

            Com.WriteLine("Signing Transaction - Nonce:" + swap.Nonce);
            await swapHandler.SignTransactionAsync(Global.Addresses.PancakeRouterMainnet, swap);

            Com.WriteLine("Sending Transaction - Nonce:" + swap.Nonce);
            var receipt =
                await swapHandler.SendRequestAndWaitForReceiptAsync(Global.Addresses.PancakeRouterMainnet, swap);

            return receipt;
        }
        #endregion


        
        private async Task Web3Loop()
        {
            Stopwatch watch = new Stopwatch();

            while (true)
            {

                watch.Restart();

                if (Core.Web3.IsNetworkConnected)
                {
                    await ApplyTokenChanges();
                    await UpdateTokenPools();
                }

                if (Core.Web3.IsAccountConnected)
                {
                    await UpdateBalances();
                }

                RefreshTime = watch.ElapsedMilliseconds;

                var timeleft = _updateInterval - watch.ElapsedMilliseconds;

                if (timeleft >= 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
                
            }

        }

        private List<decimal> _tempList = new List<decimal>();
        public void CalculateSummary()
        {
            if (!CanCalculate)
                return;

            var needsRouting = CheckPairNeedsRouting();
            var getAmountIn = _receiveExactTokens;
            var getAmountOut = _sendExactTokens;
            _tempPath.Clear();

            if (getAmountOut)
            {
                if (_amountIn == 0)
                {
                    _amountOut = 0;
                    return;
                }
                _tempPath.Add(_tokenIn);
                if (needsRouting)
                    _tempPath.Add(_wEthToken);
                _tempPath.Add(_tokenOut);

                _amountOut = SwapMath.GetAmountsOut(_amountIn, _tempPath, _tempList)[_tempList.Count - 1];
            }

            if (getAmountIn)
            {
                if (_amountOut == 0)
                {
                    _amountIn = 0;
                    return;
                }
                _tempPath.Add(_tokenIn);
                if (needsRouting)
                    _tempPath.Add(_wEthToken);
                _tempPath.Add(_tokenOut);

                _amountIn = SwapMath.GetAmountsIn(_amountOut, _tempPath, _tempList)[_tempList.Count - 1];
            }


        }




        private async Task ApplyTokenChanges()
        {
            if (_tokenInChanged)
                _tokenIn = await Core.Web3.Client.GenerateToken(_nextTokenIn);

            if (_tokenOutChanged)
                _tokenOut = await Core.Web3.Client.GenerateToken(_nextTokenOut);

            _tokenInChanged = false;
            _tokenOutChanged = false;
        }

        private bool GetPairValidity()
        {
            if (_tokenIn == null || _tokenOut == null)
                return false;
            if (_tokenIn.Contract == _tokenOut.Contract)
                return false;
            return true;
        }

        private bool PairIsSetup()
        {
            if (_tokenIn == null || _tokenOut == null)
                return false;
            return (_tokenIn.IsSetUp && _tokenOut.IsSetUp);
        }

        private bool CheckPairNeedsRouting()
        {
            return (!_tokenIn.IsRaw && !_tokenOut.IsRaw);
        }



        private async Task UpdateTokenPools()
        {
            if (_tokenIn != null)
            {
                await _tokenIn.Update(Core.Web3.Client);
            }

            if (_tokenOut != null)
            {
                await _tokenOut.Update(Core.Web3.Client);
            }

        }

        private async Task UpdateBalances()
        {
            if (_tokenIn != null)
            {
                if (Core.Web3.IsAccountConnected)
                {
                    if (_tokenIn.IsRaw)
                        _lastTokenInBalance = Core.Web3.CurrentBalance;
                    else
                    {
                        var tokenInWei = await Core.Web3.Client.Eth.ERC20.GetContractService(_tokenIn.Contract)
                            .BalanceOfQueryAsync(Core.Web3.Account.Address);
                        _lastTokenInBalance = Web3.Convert.FromWei(tokenInWei, _tokenIn.Decimals);
                    }

                }

            }

            if (_tokenOut != null)
            {
                if (Core.Web3.IsAccountConnected)
                {
                    if (_tokenOut.IsRaw)
                        _lastTokenOutBalance = Core.Web3.CurrentBalance;
                    else
                    {
                        var tokenInWei = await Core.Web3.Client.Eth.ERC20.GetContractService(_tokenOut.Contract)
                            .BalanceOfQueryAsync(Core.Web3.Account.Address);
                        _lastTokenOutBalance = Web3.Convert.FromWei(tokenInWei, _tokenOut.Decimals);
                    }
                }
            }
        }

        async public Task<List<BigInteger>> GetAmountsOutTransaction(decimal amountIn,List<Token> path)
        {
            var handler = Core.Web3.Client.Eth.GetContractQueryHandler<GetAmountsOutFunction>();
            var amountsOut = new GetAmountsOutFunction
            {
                AmountIn = Web3.Convert.ToWei(amountIn,path.First().Decimals),
                //Path = new Pair(path.First().Contract, path.Last().Contract)
            };
            var result = await handler.QueryAsync<List<BigInteger>>(Global.Addresses.PancakeRouterMainnet, amountsOut);
            return result;
        }
    }
}
