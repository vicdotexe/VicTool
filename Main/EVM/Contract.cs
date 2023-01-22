using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Contracts.MessageEncodingServices;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Annotations;
using VicTool.Controls;
using VicTool.Main.Eth;
using VicTool.Main.Eth.UniswapPair;
using FactoryFunction = VicTool.Main.Eth.FactoryFunction;

namespace VicTool.Main.EVM
{
    public abstract class Contract : IEquatable<Contract>
    {
        public string Address { get; set; }

        protected Contract(string address)
        {
            Address = address;
        }

        public bool Equals(Contract other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address == other.Address;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Contract)obj);
        }

        public override int GetHashCode()
        {
            return (Address != null ? Address.GetHashCode() : 0);
        }
    }


    public class EvmToken : Contract
    {
        public EvmToken(string name, string symbol, int decimals, string address) : base(address)
        {
            Name = name;
            Symbol = symbol;
            Decimals = decimals;

        }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
    }


    public class EVMDexFactory : Contract
    {
        
        public EVMDexFactory(string address, Dictionary<ValuePair<EvmToken>,EVMDexPair> preLoad = null) : base(address)
        {
            if (preLoad != null)
                _pairs = preLoad;
            else
                _pairs = new();
        }

        private Dictionary<ValuePair<EvmToken>, EVMDexPair > _pairs;
        public async Task<EVMDexPair> GetPairAsync(Web3 web3, EvmToken  tokenA, EvmToken tokenB)
        {
            var valuePair = new ValuePair<EvmToken>(tokenA, tokenB);
            foreach (var vPair in _pairs.Keys)
            {
                if (vPair == valuePair)
                {
                    return _pairs[vPair];
                }
            }
            var getPair = new GetPairFunction()
            {
                TokenA = tokenA.Address,
                TokenB = tokenB.Address
            };
            var getPairHandler = web3.Eth.GetContractQueryHandler<GetPairFunction>();

            var result = await getPairHandler.QueryAsync<string>(Address, getPair);
            var dexPair = new EVMDexPair(result, tokenA, tokenB);
            _pairs.Add(valuePair, dexPair);
            return dexPair;
        }
    }

    public class EVMDexPair : Contract
    {
        public EvmToken Token0 { get; }
        public EvmToken Token1 { get; }

        public decimal Reserve0 { get; private set; }
        public decimal Reserve1 { get; private set; }

        public EVMDexPair(string address, EvmToken tokenA, EvmToken tokenB):base(address)
        {
            Token0 = tokenA;
            Token1 = tokenB;
        }

        public async Task UpdateReserves(Web3 web3)
        {
            if (Address == Global.NullAddress)
                return;
            var getReservesFunction = new GetReservesFunction();
            var getReservesHandler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();
            var result = await getReservesHandler.QueryAsync<GetReservesOutputDTO>(Address, getReservesFunction);
            Reserve0 = Web3.Convert.FromWei(result.Reserve0, Token0.Decimals);
            Reserve1 = Web3.Convert.FromWei(result.Reserve1, Token1.Decimals);
        }

        public decimal GetReserves(EvmToken token)
        {
            if (token.Address == Token0.Address)
                return Reserve0;
            if (token.Address == Token1.Address)
                return Reserve1;
            throw new NotImplementedException("Shouldn't arrive here.");
        }


    }


    public class EVMDex : INotifyPropertyChanged
    {
        public EVMDexRouter Router { get; }
        public Web3 Web3 => _web3;

        private Web3 _web3;
        private decimal _fee;

        public EvmToken TokenIn
        {
            get => _tokenIn;
            set
            {
                _tokenIn = value;
            }
        }

        private EvmToken _tokenIn;

        public EvmToken TokenOut
        {
            get => _tokenOut;
            set
            {
                _tokenOut = value;
            }
        }

        private EvmToken _tokenOut;

        public decimal AmountIn
        {
            get => _amountIn;
            set
            {
                if (_amountIn == value)
                    return;
                _amountIn = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        private decimal _amountOut;

        public decimal MinimumAmount
        {
            get => _minimumAmount;
            private set
            {
                if (_minimumAmount == value)
                    return;
                _minimumAmount = value;
                OnPropertyChanged();
            }
        }

        private decimal _minimumAmount = 0.5m;

        public bool IsExactIn
        {
            get;
            set;
        } = true;

        public decimal Slippage
        {
            get;
            set;
        }

        private EVMDex(Account account, EvmNetwork network, EVMDexRouter router)
        {
            Router = router;
            TrackedRpcClient rpc = new TrackedRpcClient(new Uri(network.NetworkUrl));
            _web3 = new Web3(account, rpc);
        }

        private readonly object _lock = new object();

        private List<EvmToken> _tempPath;
        public async Task ProcessData()
        {
            var tokenIn = _tokenIn;
            var tokenOut = _tokenOut;
            var amountIn = _amountIn;
            var amountOut = _amountOut;
            var isExactIn = IsExactIn;

            if ((amountIn == 0 && isExactIn) || (amountOut == 0 && !isExactIn) || (tokenIn?.Address == tokenOut.Address))
            {
                return;
            }

            bool pathFound = false;
            if (_tempPath != null)
            {
                pathFound = (_tempPath.First().Equals(tokenIn) && _tempPath.Last().Equals(tokenOut));
            }
            
            if (!pathFound)
                _tempPath = await GetRouteAsync(tokenIn, tokenOut);

            if (isExactIn)
            {
                try
                {
                    var amounts = await Router.GetAmountsOutAsync(_web3, amountIn, _tempPath);
                    AmountOut = amounts.Last();
                }
                catch (Exception e)
                {

                }
                
            }
            else
            {

            }

        }

        private async Task<List<EvmToken>> GetRouteAsync(EvmToken tokenIn, EvmToken tokenOut)
        {
            var factory = await Router.GetFactoryAsync(_web3);
            var directPair = await factory.GetPairAsync(_web3, tokenIn, tokenOut);

            var path = new List<EvmToken>();
            path.Add(tokenIn);
            if (directPair.Address == Global.NullAddress)
            {
                var wrappedToken = await Router.GetWrappedTokenAsync(_web3);
                path.Add(wrappedToken);
            }

            path.Add(tokenOut);
            return path;

        }



        public static EVMDex Load(Account account, EvmNetwork network, string routerAddress, decimal fee = 0.0025m)
        {
            EVMDexRouter router = new EVMDexRouter(routerAddress,network);

            return new EVMDex(account, network,router);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EvmNetwork
    {
        public string NetworkName { get; set; }
        public string NetworkUrl { get; set; }
        public string ChainId { get; set; }
        public string CurrencySymbol { get; set; }
    }

    public class EVMDexRouter : Contract
    {
        private EVMDexFactory _factory;
        private EvmToken _wrappedToken;
        private EvmNetwork _network;

        public EVMDexRouter(string address, EvmNetwork network) : base(address)
        {
            _network = network;
        }

        public async Task<EvmToken> GetWrappedTokenAsync(Web3 web3)
        {
            if (_wrappedToken != null)
                return _wrappedToken;

            try
            {
                /*
                var getWeth = new WETHFunction();
                var getWethHandler = web3.Eth.GetContractQueryHandler<WETHFunction>();
                var result = await getWethHandler.QueryAsync<string>(Address, getWeth);
                */
                var abi =
                    @"[{'inputs':[],'name':'WETH','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'}]";
                var result = await web3.Eth.GetContract(abi, Address).GetFunction("WETH")
                    .CallAsync<string>();
                var tokenData = await web3.GetTokenData(result);
                var token = new EvmToken(tokenData.Name, tokenData.Ticker, tokenData.Decimals, result);
                _wrappedToken = token;
                return token;
            }
            catch
            {
                try
                {
                    var wrapped = "W" + _network.CurrencySymbol;
                    var abi =
                        @"[{'inputs':[],'name':'" + wrapped +
                        "','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'}]";
                    var result = await web3.Eth.GetContract(abi, Address).GetFunction(wrapped)
                        .CallAsync<string>();
                    var tokenData = await web3.GetTokenData(result);
                    var token = new EvmToken(tokenData.Name, tokenData.Ticker, tokenData.Decimals, result);
                    _wrappedToken = token;
                    return token;
                }
                catch
                {
                    Com.WriteLine("Can't find wrapped " + _network.CurrencySymbol + " token through router function.");
                    var abi =
                        @"[{'inputs':[],'name':'WETH9','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'}]";
                    var result = await web3.Eth.GetContract(abi, Address).GetFunction("WETH9")
                        .CallAsync<string>();
                    var tokenData = await web3.GetTokenData(result);
                    var token = new EvmToken(tokenData.Name, tokenData.Ticker, tokenData.Decimals, result);
                    _wrappedToken = token;
                    return token;
                    
                }

            }





        }

        public async Task<EVMDexFactory> GetFactoryAsync(Web3 web3)
        {
            if (_factory != null)
                return _factory;

            var getFactory = new FactoryFunction();
            var getWethHandler = web3.Eth.GetContractQueryHandler<FactoryFunction>();
            var result = await getWethHandler.QueryAsync<string>(Address, getFactory);

            _factory = new EVMDexFactory(result);
            return _factory;
        }

        public async Task<decimal> GetAmountOutAsync(Web3 web3, decimal amountIn, EvmToken tokenIn, EvmToken tokenOut)
        {
            var factory = await GetFactoryAsync(web3);
            var pair = await factory.GetPairAsync(web3, tokenIn, tokenOut);
            await pair.UpdateReserves(web3);

            var reserveIn = pair.GetReserves(tokenIn);
            var reserveOut = pair.GetReserves(tokenOut);

            var amountInWithFee = amountIn * (1 - 0.0025m);
            var numerator = amountInWithFee * reserveOut;
            var denominator = reserveIn + amountInWithFee;
            var amountOut = numerator / denominator;
            return amountOut;
        }

        public async Task<List<decimal>> GetAmountsOutAsync(Web3 web3, decimal amountIn, List<EvmToken> path)
        {
            List<decimal> amounts = new();
            amounts.Add(amountIn);

            for (int i = 0; i < path.Count-1; i++)
            {
                var amount = await GetAmountOutAsync(web3, amounts[i], path[i], path[i + 1]);
                amounts.Add(amount);
            }

            return amounts;
        }
    }
}
