using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Common.Logging;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.OpenPgp;
using VicTool.Annotations;
using VicTool.Controls;
using VicTool.Main.EVM;

namespace VicTool.Main.Eth
{

    public class Web3DexInfo
    {
        public string Name { get; set; }
        public string Router { get; set; }
        public string Factory { get; set; }
        public string Fee { get; set; }
    }

    public class Web3DexRouter : INotifyPropertyChanged
    {
        public void SetDex(Web3DexInfo dex)
        {
            _info = dex;
            if (_info != null)
                LiquidityPair.DexFee = decimal.Parse(_info.Fee);
        }

        private Web3DexInfo _info;
        public Web3DexInfo DexInfo => _info;
        private Web3Network _network;

        public Web3 Web3 => _web3;
        private Web3 _web3;

        public bool AccountConnected { get; private set; }

        public bool IsExactIn { get; set; } = true;

        private LiquidityPair _tokenInEthPair;
        private LiquidityPair _tokenOutEthPair;
        private LiquidityPair _directPair;
        private Web3Token _tempMainToken;

        public decimal Slippage
        {
            get;
            set;
        } = 0.5m;

        public decimal MinimumAmount
        {
            get => _minimumAmount;
            set
            {
                if (_minimumAmount == value)
                    return;
                _minimumAmount = value;
                OnPropertyChanged();
            }
        }
        private decimal _minimumAmount;
        public decimal PriceImpact
        {
            get => _priceImpact;
            private set
            {
                if (_priceImpact == value)
                    return;
                _priceImpact = value;
                OnPropertyChanged();
            }
        }
        private decimal _priceImpact;
        public decimal InPerOut
        {
            get => _inForOut;
            private set
            {
                if (_inForOut == value)
                    return;
                _inForOut = value;
                OnPropertyChanged();
            }
        }
        private decimal _inForOut;
        public decimal OutPerIn
        {
            get => _outForIn;
            private set
            {
                if (_outForIn == value)
                    return;
                _outForIn = value;
                OnPropertyChanged();
            }
        }
        private decimal _outForIn;

        public Web3Token TokenIn
        {
            get => _tokenIn;
            set
            {
                if (value != null)
                    _tokenInEthPair = new LiquidityPair(value, _tempMainToken, _info);

                if (_tokenOut != null)
                    _directPair = new LiquidityPair(value, _tokenOut, _info);
                _tokenIn = value;
            }
        }
        private Web3Token _tokenIn;

        public Web3Token TokenOut
        {
            get => _tokenOut;
            set
            {
                if (value != null)
                    _tokenOutEthPair = new LiquidityPair(value, _tempMainToken, _info);

                if (_tokenIn != null)
                    _directPair = new LiquidityPair(_tokenIn, value, _info);
                _tokenOut = value;
            }
        }
        private Web3Token _tokenOut;

        private Web3Token _wrappedMainToken;

        public decimal AmountIn
        {
            get => _amountIn;
            set
            {
                if (_amountIn == value)
                    return;
                _amountIn = value;
                if (!IsExactIn)
                    OnPropertyChanged("AmountIn");
            }
        }
        private decimal _amountIn;

        public decimal AmountOut
        {
            get => _amountOut;
            set
            {
                if (_amountOut == value)
                    return;
                _amountOut = value;
                if (IsExactIn)
                    OnPropertyChanged("AmountOut");
            }
        }
        private decimal _amountOut;

        public bool PairValid
        {
            get => _pairValid;
            set
            {
                if (value == _pairValid)
                    return;
                _pairValid = value;
                OnPropertyChanged();
            }
        }
        private bool _pairValid;
        private RpcClient client;
        public Web3DexRouter(Web3Network network)
        {
            _network = network;

            var uri = new Uri(network.NetworkUrl);
            client = new TrackedRpcClient(uri);

            _web3 = new Web3(client);
            _tempMainToken = network.GetWrappedMainToken();
        }

        public async Task ProcessData()
        {
            if (_info == null)
                return;
            try
            {
                //Initialize/Update Pairs
                if (_tokenInEthPair != null)
                {
                    if (!_tokenInEthPair.IsInitialized)
                        await _tokenInEthPair.Initialize(_web3);
                    if (_directPair?.HasLiquidity == false)
                        await _tokenInEthPair.RefreshReserves(_web3);
                }

                if (_tokenOutEthPair != null)
                {
                    if (!_tokenOutEthPair.IsInitialized)
                        await _tokenOutEthPair.Initialize(_web3);
                    if (_directPair?.HasLiquidity == false)
                        await _tokenOutEthPair.RefreshReserves(_web3);
                }

                if (_directPair != null)
                {
                    if (!_directPair.IsInitialized)
                        await _directPair.Initialize(_web3);
                    if (_directPair.HasLiquidity)
                        await _directPair.RefreshReserves(_web3);
                }

            }
            catch (Exception e)
            {
                Com.WriteLine(e.Message);
                Com.WriteLine(e.InnerException?.Message);
            }

        }

        private List<decimal> _amounts = new();

        private List<Web3Token> _path = new List<Web3Token>();
        private async Task CalculateAsync()
        {
            var needsRouting = _directPair?.HasLiquidity == false;
            _path.Clear();
            _amounts.Clear();

            _path.Add(_tokenIn);
            if (needsRouting)
                _path.Add(_tempMainToken);
            _path.Add(_tokenOut);

            if (IsExactIn)
            {
                await GetAmountsOut(_amountIn, _path, _amounts);
                AmountOut = _amounts.Last();
            }
            else
            {
                await GetAmountsIn(_amountOut, _path, _amounts);
                AmountIn = _amounts.First();
            }
        }
        public void CalculateAmounts()
        {
            if (TokenIn?.Contract != _tokenInEthPair?.TokenA.Contract)
            {
                PairValid = false;
                return;
            }

            if (TokenOut?.Contract != _tokenOutEthPair?.TokenA.Contract)
            {
                PairValid = false;
                return;
            }

            if (_tokenInEthPair?.IsInitialized == false || _tokenOutEthPair?.IsInitialized == false ||
                _directPair?.IsInitialized == false)
            {
                PairValid = false;
                return;
            }

            var needsRouting = _directPair?.HasLiquidity == false;



            PairValid = true;
            _amounts.Clear();
            _path.Clear();

            decimal quote = 0;
            if (IsExactIn)
            {
                if (AmountIn != 0)
                {
                    if (!needsRouting)
                    {
                        var amountOut = _directPair.GetAmountOut(_amountIn, TokenIn);
                        _amounts.Add(amountOut);
                        quote = _directPair.Quote(AmountIn, TokenIn);
                        OutPerIn = _directPair.Quote(1, TokenIn);
                        InPerOut = _directPair.Quote(1, TokenOut);
                    }
                    else
                    {
                        if (_tokenIn.Contract == _tokenOut.Contract)
                        {
                            _amounts.Add(AmountIn);
                        }
                        else
                        {
                            //get amounts out
                            var amountOut = _tokenInEthPair.GetAmountOut(_amountIn, TokenIn);
                            _amounts.Add(amountOut);
                            amountOut = _tokenOutEthPair.GetAmountOut(amountOut, _tempMainToken);
                            _amounts.Add(amountOut);

                            //get quote
                            quote = _tokenInEthPair.Quote(AmountIn, TokenIn);
                            quote = _tokenOutEthPair.Quote(quote, _tempMainToken);

                            //out per in
                            var outPerIn = _tokenInEthPair.Quote(1, TokenIn);
                            outPerIn = _tokenOutEthPair.Quote(outPerIn, _tempMainToken);

                            //InPerOut
                            var inPerOut = _tokenOutEthPair.Quote(1, TokenOut);
                            inPerOut = _tokenInEthPair.Quote(inPerOut, _tempMainToken);

                            OutPerIn = outPerIn;
                            InPerOut = inPerOut;

                        }

                    }

                    AmountOut = _amounts.Last();
                    PriceImpact = (1-(AmountOut / quote))*100;
                    MinimumAmount = AmountOut * (1 - (Slippage / 100));
                }
                else
                {
                    AmountOut = 0;
                    MinimumAmount = 0;
                    PriceImpact = 0;
                    OutPerIn = 0;
                    InPerOut = 0;
                }

            } 
            else
            {
                if (AmountOut != 0)
                {
                    if (!needsRouting)
                    {
                        var amountIn = _directPair.GetAmountIn(_amountOut, TokenIn);
                        _amounts.Add(amountIn);
                    }
                    else
                    {
                        if (TokenOut.Contract == TokenIn.Contract)
                        {
                            _amounts.Add(AmountOut);
                        }
                        else
                        {
                            var amountIn = _tokenOutEthPair.GetAmountIn(_amountOut, _tempMainToken);
                            _amounts.Add(amountIn);
                            amountIn = _tokenInEthPair.GetAmountIn(amountIn, TokenIn);
                            _amounts.Add(amountIn);

                        }

                    }

                    AmountIn = _amounts.Last();
                }
                else
                {
                    AmountIn = 0;
                }
            }

        }


        #region Contract Functions
        
        public async Task<string> GetPairContract(Web3Token tokenA, Web3Token tokenB)
        {
            var getPair = new GetPairFunction()
            {
                TokenA = tokenA.Contract,
                TokenB = tokenB.Contract
            };
            var getPairHandler = _web3.Eth.GetContractQueryHandler<GetPairFunction>();

            var result = await getPairHandler.QueryAsync<string>(_info.Factory, getPair);
            return result;
        }

        public async Task<Web3Token> GetWrappedMainToken()
        {
            var getWeth = new WETHFunction();
            var getWethHandler = _web3.Eth.GetContractQueryHandler<WETHFunction>();
            var result = await getWethHandler.QueryAsync<string>(_info.Factory, getWeth);

            var decimals = await _web3.Eth.ERC20.GetContractService(result).DecimalsQueryAsync();
            var symbol = await _web3.Eth.ERC20.GetContractService(result).SymbolQueryAsync();
            var name = await _web3.Eth.ERC20.GetContractService(result).NameQueryAsync();
            var token = new Web3Token()
            {
                Contract = result,
                Decimals = decimals,
                IsMain = true,
                IsRaw = false,
                Name = name,
                Symbol = symbol
            };
            return token;
        }

        
        public async Task GetAmountsOut(decimal amountIn, List<Web3Token> path, List<decimal> amounts)
        {
            var contracts = path.Select(token => token.Contract).ToList();

            var amountsOut = new GetAmountsOutFunction()
            {
                AmountIn = Web3.Convert.ToWei(amountIn, path.First().Decimals),
                Path = contracts
            };
            var amountsOutHandler = _web3.Eth.GetContractQueryHandler<GetAmountsOutFunction>();
            var result = await amountsOutHandler.QueryAsync<List<BigInteger>>(_info.Router, amountsOut);
            
            
            for (int i = 0; i < result.Count; i++)
            {
                amounts.Add(Web3.Convert.FromWei(result[i],path[i].Decimals));
            }
            
        }
        public async Task GetAmountsIn(decimal amountOut, List<Web3Token> path, List<decimal> amounts)
        {
            var contracts = path.Select(token => token.Contract).ToList();

            var amountsIn = new GetAmountsInFunction()
            {
                AmountOut = Web3.Convert.ToWei(amountOut,path.Last().Decimals),
                Path = contracts
            };
            var amountsOutHandler = _web3.Eth.GetContractQueryHandler<GetAmountsInFunction>();
            var result = await amountsOutHandler.QueryAsync<List<BigInteger>>(_info.Router, amountsIn);


            for (int i = 0; i < result.Count; i++)
            {
                amounts.Add(Web3.Convert.FromWei(result[i], path[i].Decimals));
            }

        }

        public async Task<TransactionReceipt> SwapExactTokensForEth(decimal amountInTokens, decimal amountEthOutMin, List<Web3Token> path, decimal slippage = 0.005m, int gasPrice = 5, string toAddress = null, double deadlineMinutes = 5)
        {

            var swapHandler = _web3.Eth.GetContractTransactionHandler<SwapExactTokensForETHFunction>();
            var swap = new SwapExactTokensForETHFunction()
            {
                AmountIn = Web3.Convert.ToWei(amountInTokens, path.First().Decimals),
                AmountOutMin = Web3.Convert.ToWei(amountEthOutMin),
                Path = new List<string>() { path.First().Contract, _network.WrappedMainContract },
                To = Core.Web3.Account.Address,
                Deadline = DateTimeOffset.Now.AddMinutes(deadlineMinutes).ToUnixTimeSeconds()
            };

            var gasprice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);

            var nonce = await _web3.TransactionManager.Account.NonceService.GetNextNonceAsync();

            swap.Nonce = nonce;
            swap.GasPrice = gasprice;
            var estimate = await swapHandler.EstimateGasAsync(_info.Router, swap);
            swap.Gas = estimate.Value;

            Com.WriteLine("Signing Transaction - Nonce:" + swap.Nonce);
            await swapHandler.SignTransactionAsync(_info.Router, swap);

            Com.WriteLine("Sending Transaction - Nonce:" + swap.Nonce);
            var receipt =
                await swapHandler.SendRequestAndWaitForReceiptAsync(_info.Router, swap);

            return receipt;
        }

        async public Task<TransactionReceipt> SwapExactEthForTokens(decimal amountInEth, decimal amountTokensOutMin, List<Web3Token> path, decimal slippage = 0.005m, int gasPrice = 5, double deadlineMinutes = 5)
        {
            var swapHandler = Core.Web3.Client.Eth.GetContractTransactionHandler<SwapExactETHForTokensFunction>();
            var swap = new SwapExactETHForTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(amountInEth),
                AmountOutMin = Web3.Convert.ToWei(amountTokensOutMin * (1 - slippage), path.Last().Decimals),
                Path = new List<string>() { path.First().Contract, path.Last().Contract },
                To = Core.Web3.Account.Address,
                Deadline = DateTimeOffset.Now.AddMinutes(deadlineMinutes).ToUnixTimeSeconds(),

            };
            var gasprice = Web3.Convert.ToWei(gasPrice, UnitConversion.EthUnit.Gwei);
            var estimate = await swapHandler.EstimateGasAsync(_info.Router, swap);
            var nonce = await Core.Web3.Account.NonceService.GetNextNonceAsync();

            swap.Nonce = nonce;
            swap.GasPrice = gasprice;
            swap.Gas = estimate.Value;

            Com.WriteLine("Signing Transaction - Nonce:" + swap.Nonce);
            await swapHandler.SignTransactionAsync(_info.Router, swap);

            Com.WriteLine("Sending Transaction - Nonce:" + swap.Nonce);
            var receipt =
                await swapHandler.SendRequestAndWaitForReceiptAsync(_info.Router, swap);

            return receipt;
        }
        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
