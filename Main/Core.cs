using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Documents;
using System.Windows.Threading;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Sockets;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Interfaces.Clients.SpotApi;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Futures;
using Kucoin.Net.Objects.Models.Spot;
using Kucoin.Net.Objects.Models.Spot.Socket;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Controls;
using VicTool.Main.Kucoin;
using VicTool.Main.Misc;

namespace VicTool.Main
{

    public enum CoreEvents
    {
        AddRemoveAccounts
    }

    public class Core
    {
        public static class ClipBoard
        {
            public static Dictionary<Type, object> _items = new();
            public static void Copy<T>(T item) where T : ICloneable
            {
                _items[typeof(T)] = item.Clone();
            }

            public static T Get<T>() where T : ICloneable
            {
                if (_items.ContainsKey(typeof(T)))
                    return (T)_items[typeof(T)];
                return default(T);
            }
        }
        
        private const long _defaultUiTickInterval = 100;
        private static Core _instance;
        private Web3Connection _web3Connection;
        private KucoinConnection _kucoinConnection;
        private DispatcherTimer _uiLoopTimer;
        private TBot _telegramBot;

        public static TBot TBot => _instance._telegramBot;
        public static Web3Connection Web3 => _instance._web3Connection;
        public static KucoinConnection Kucoin => _instance._kucoinConnection;

        public static EventHandler OnUiTick;
        private Emitter<CoreEvents> _emitter = new Emitter<CoreEvents>();
        public static Emitter<CoreEvents> Emitter => _instance._emitter;

        private static int _arbIndex;

        public static EventHandler<ArbData> SoldFromPancake;

        public Core(DispatcherTimer uiTimer)
        {
            _instance = this;
            _telegramBot = new TBot();
            _web3Connection = new Web3Connection();
            _kucoinConnection = new KucoinConnection(Global.GetKucoinCredentials());
            _uiLoopTimer = uiTimer;
            uiTimer.Interval = TimeSpan.FromMilliseconds(_defaultUiTickInterval);
            uiTimer.Tick += (s,e)=>
            {
                OnUiTick?.Invoke(s,e);
            };
            uiTimer.Start();

            

        }

        public static void StartArbitrageFromPancake(string ticker, decimal bnbQty)
        {
            Com.WriteLine("Starting arbitrage");
            var data = new ArbData()
            {
                Index = _arbIndex++,
                TokenTicker = ticker,
                BnbQuantity = bnbQty
            };
            SoldFromPancake?.Invoke(null,data);
        }

    }

    public class KucoinConnection
    {
        public KucoinClient RestClient => _restClient;
        private KucoinClient _restClient;

        public KucoinSocketClient SocketClient => _socketClient;
        private KucoinSocketClient _socketClient;

        private ApiCredentials _apiCredentials;

        private KucoinClientOptions _restOptions;
        private KucoinSocketClientOptions _socketOptions;

        private Dictionary<string, decimal> _kucoinPrices = new Dictionary<string, decimal>();

        public AggregatedOrderBook MainOrderBook { get; set; }


        public KucoinConnection(KucoinApiCredentials credentials = null)
        {
            
            _restOptions = new KucoinClientOptions()
            {
                ApiCredentials = credentials
            };
            _socketOptions = new KucoinSocketClientOptions()
            {
                ApiCredentials = credentials
            };
            KucoinClient.SetDefaultOptions(_restOptions);
            _restClient = new KucoinClient(_restOptions);
            KucoinSocketClient.SetDefaultOptions(_socketOptions);
            _socketClient = new KucoinSocketClient(_socketOptions);

        }

        private void SetApiCredentials(KucoinApiCredentials credentials)
        {
            _apiCredentials = credentials;
            _socketOptions.ApiCredentials = credentials;
            _restOptions.ApiCredentials = credentials;
        }


        private int subId;
        

        private Dictionary<string, string> _symbols = new Dictionary<string, string>();
        public decimal GetPriceSymbol(string symbol, int decimals = -1)
        {
            if (!_kucoinPrices.ContainsKey(symbol))
            {
                _kucoinPrices.Add(symbol, 1234);

                Task.Run(async () =>
                {
                    await _socketClient.UnsubscribeAsync(subId);
                    var sub = await _socketClient.SpotStreams.SubscribeToTickerUpdatesAsync(_kucoinPrices.Keys,
                        UpdatePrices);
                    subId = sub.Data.Id;
                });

            }
            return decimals == -1 ? _kucoinPrices[symbol] : _kucoinPrices[symbol].Round(decimals);
        }

        public decimal GetPrice(string asset, int decimals = -1)
        {
            return GetPriceSymbol(asset + "-USDT", decimals);
        }

        private void UpdatePrices(DataEvent<KucoinStreamTick> tick)
        {
            _kucoinPrices[tick.Data.Symbol] = tick.Data.LastPrice ?? 1234;
        }
    }

    public enum Web3Events
    {
        BalanceUpdate,
        AccountConnected,
        NetworkConnected,
        NetworksAltered
    }

    public class Web3Connection
    {
        public Web3 Client => _web3;
        private Web3 _web3;

        public string AccountName => _accountName ?? "(none)";
        private string _accountName;
        public string PublicAddress => _account?.Address ?? "(none)";

        public decimal CurrentBalance { get; private set; }

        
        public  Emitter<Web3Events> Emitter { get; private set; } = new Emitter<Web3Events>();
        

        public Account Account
        {
            get => _account;
            private set
            {
                _account = value;
                RefreshConnection();
            }
        }
        private Account _account;

        public Web3Network Network
        {
            get => _network;
            set
            {
                if (_network == value)
                    return;
                _network = value;
                RefreshConnection();
            }
        }

        private Web3Network _network;

        private void RefreshConnection()
        {
            if (_account != null)
                _web3 = new Web3(_account, _network.NetworkUrl);
            else
                _web3 = new Web3(_network.NetworkUrl);

            _web3.TransactionManager.UseLegacyAsDefault = true;
            Emitter.Emit(Web3Events.NetworkConnected);
        }

        public bool IsAccountConnected => _account != null;
        public bool IsNetworkConnected => _network != null;
        public bool IsFullyConnected => IsAccountConnected && IsNetworkConnected;

        public Web3Connection()
        {
            Task.Run(FetchBalance);
        }

        public void ConnectAccount(string privateKey, string accountName)
        {
            _accountName = accountName;
            Account = new Account(privateKey);
            Emitter.Emit(Web3Events.AccountConnected);
        }

        public void DisconnectAccount()
        {
            _accountName = null;
            Account = null;
        }

        private async Task FetchBalance()
        {
            while (true)
            {
                if (IsAccountConnected)
                {
                    var balanceInWei = await _web3.Eth.GetBalance.SendRequestAsync(_account.Address);
                    CurrentBalance = Web3.Convert.FromWei(balanceInWei);
                }
                else
                {
                    CurrentBalance = 0;
                }
                Emitter.Emit(Web3Events.BalanceUpdate);
                await Task.Delay(500);
            }
            
        }
    }

    public class ArbData
    {
        public int Index { get; set; }
        public string TokenTicker { get; set; }
        public decimal BnbQuantity { get; set; }

    }

    public abstract class Arbitrage
    {
        protected Dispatcher _dispatcher;
        public bool IsKucoinActive { get; protected set; }
        public bool IsDexActive { get; protected set; }
        public bool Failed { get; protected set; }

        protected Arbitrage()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public abstract void OnKucoinBalanceUpdate(DataEvent<KucoinBalanceUpdate> update);
        public abstract void OnKucoinOrderUpdate(DataEvent<KucoinStreamOrderBaseUpdate> baseUpdate);
        public abstract void OnKucoinMatchUpdate(DataEvent<KucoinStreamOrderMatchUpdate> matchUpdate);

    }

    public class SellDexBuyCex : Arbitrage
    {
        public string TokenTicker { get; set; }
        public decimal TokenQtySold { get; set; }
        public decimal EthQtySentToCex { get; set; }
        

        public SellDexBuyCex()
        {
            IsKucoinActive = true;
        }

        private string _sellBnbOrderId;
        private string _buySoulOrderId;


        public override void OnKucoinBalanceUpdate(DataEvent<KucoinBalanceUpdate> update)
        {
            var data = update.Data;

            if (data.RelationEvent == "main.deposit")
            {
                var qtyMatch = DecimalExt.IsEqual(EthQtySentToCex, data.AvailableChange, 7);
                if (qtyMatch)
                {
                    Com.WriteLine(data.AvailableChange.Round(8) + " " + data.Asset + " arrived in Main");
                    _dispatcher.InvokeAsync(async () =>
                    {
                        var innerTransfer = await Core.Kucoin.RestClient.SpotApi.Account.InnerTransferAsync(
                            data.Asset,
                            AccountType.Main,
                            AccountType.Trade, decimal.Round(data.AvailableChange, 7));
                        Com.WriteLine("Inner Transfer placed successful: " + innerTransfer.Success);
                        Failed = !innerTransfer.Success;
                    });
                }
                
            }

            if (data.RelationEvent == "trade.transfer")
            {
                
                var qtyMatch = DecimalExt.IsEqual(EthQtySentToCex, data.AvailableChange, 7);
                if (qtyMatch)
                {
                    Com.WriteLine(data.AvailableChange.Round(7) + " " + data.Asset + " transferred to Trading");
                    _dispatcher.InvokeAsync(async () =>
                    {
                        var guid = "vic" + Guid.NewGuid().ToString();
                        _sellBnbOrderId = guid;
                        var order = await Core.Kucoin.RestClient.SpotApi.Trading.PlaceOrderAsync("BNB-USDT", OrderSide.Sell, NewOrderType.Market,
                            data.AvailableChange.ToNegativeInfinity(4),clientOrderId: guid);
                        
                        Com.WriteLine("BNB Sell Order placed successful: " + order.Success);
                        Failed = !order.Success;
                    });
                }
            }

        }

        private decimal _usdtGain;
        private decimal _tokenGain;

        public override void OnKucoinMatchUpdate(DataEvent<KucoinStreamOrderMatchUpdate> matchUpdate)
        {
            if (matchUpdate.Data.ClientOrderid == _sellBnbOrderId)
            {
                _usdtGain += (matchUpdate.Data.MatchPrice * matchUpdate.Data.MatchQuantity);
            }
            if (matchUpdate.Data.ClientOrderid == _buySoulOrderId)
            {
                _tokenGain += (matchUpdate.Data.MatchPrice * matchUpdate.Data.Quantity);
            }
        }


        public override void OnKucoinOrderUpdate(DataEvent<KucoinStreamOrderBaseUpdate> baseUpdate)
        {
            Com.WriteLine("Update Type: " + baseUpdate.Data.UpdateType.ToString());
            Com.WriteLine("Order Id: " + baseUpdate.Data.OrderId);
            Com.WriteLine("Client Order Id: " + baseUpdate.Data.ClientOrderid);
            Com.WriteLine("Price: " + baseUpdate.Data.Price);
            Com.WriteLine("Quantity Filled: " + baseUpdate.Data.QuantityFilled);
            if (baseUpdate.Data.ClientOrderid == _sellBnbOrderId)
            {
                if (baseUpdate.Data.Status == ExtendedOrderStatus.Done)
                {
                    Com.WriteLine("Sold BNB for USDT");
                    _dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(1000);
                        var guid = "vic" + Guid.NewGuid().ToString();
                        _buySoulOrderId = guid;

                        if (_usdtGain == 0)
                        {
                            //_usdtGain = baseUpdate.Data.Price * baseUpdate.Data.QuantityFilled;
                        }
                        var buy = await Core.Kucoin.RestClient.SpotApi.Trading.PlaceOrderAsync("SOUL-USDT",
                            OrderSide.Buy, NewOrderType.Market,
                            quoteQuantity: (_usdtGain * 0.985m).ToNegativeInfinity(3),clientOrderId: guid);
                        Com.WriteLine("SOUL Buy Order placed successful: " + buy.Success);
                        
                        Failed = !buy.Success;
                    });
                }
            }

            if (baseUpdate.Data.ClientOrderid == _buySoulOrderId)
            {
                if (baseUpdate.Data.Status == ExtendedOrderStatus.Done)
                {
                    _dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(1000);
                        Com.WriteLine("Bought SOUL with USDT");
                        Com.WriteLine("Recieved " + _tokenGain + TokenTicker);
                    });

                }
            }
        }
    }

}
