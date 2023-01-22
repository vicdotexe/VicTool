using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;
using Kucoin.Net.SymbolOrderBooks;
using Nethereum.Web3;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.Kucoin;
using VicTool.Main.Misc;
using VicTool.Main.Swap;
using VicTool.Properties;
using Math = System.Math;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for ProfitSheet.xaml
    /// </summary>
    public partial class ProfitSheet : UserControl
    {


        private decimal _pPrice;
        private static decimal _kPrice;
        public static decimal KucoinPrice => _kPrice;
        private decimal _spread => (_kPrice + _pPrice) / 2;
        private Token _dexToken;
        private KucoinSpotSymbolOrderBook _book => Core.Kucoin.MainOrderBook.Book;
        private decimal _lastSign;
        public static Token TargetToken=>_instance._dexToken;
        private static ProfitSheet _instance;
        private List<Web3Token> _tokens;

        public ProfitSheet()
        {
            _instance = this;
            InitializeComponent();
            Initialize();
        }

        public void Initialize()
        {
            Core.Web3.Emitter.AddObserver(Web3Events.NetworkConnected, () =>
            {
                _dexToken = null;
                comboBoxTargetToken.ItemsSource = Global.Paths.GetTokensFromFileByNetwork(Core.Web3.Network);
                comboBoxTargetToken.DisplayMemberPath = "Symbol";
            });
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            Core.OnUiTick += UiTick;



            dataGridRisks.ItemsSource = _risks;
            comboBoxTargetToken.SelectionChanged += ComboBoxTargetToken_SelectionChanged;
            comboBoxTargetToken.MouseRightButtonDown +=
                delegate
                {
                    Task.Run(async () =>
                    {
                        var w3T = Core.ClipBoard.Get<Web3Token>();
                        _dexToken = await Core.Web3.Client.GenerateToken(w3T.Contract);

                    });

                };

            Task.Run(async () =>
            {
                while (true)
                {
                    if (_dexToken != null) 
                        await _dexToken.Update(Core.Web3.Client);

                    await Task.Delay(1000);
                }
            });

            Core.TBot.OnCallBack += (s, e) =>
            {
                Dispatcher.Invoke(
                    delegate
                    {
                        OnTelegramCallBack(e);
                    });
            };
            Core.TBot.RecievedInput += (s, e) =>
            {
                Dispatcher.Invoke(
                    delegate
                    {
                        OnTelegramInput(e);
                    });
            };
        }

        public void OnTelegramInput(string message)
        {
            if (message == "/profitsheet")
            {
                Core.TBot.SendSnapshot(this);
            }
        }
        private void OnTelegramCallBack(CallbackQuery callBack)
        {
            if (callBack.Data == null)
                return;
            if (callBack.Data == "profitsheet")
                Core.TBot.SendSnapshot(this);
        }

        private void ComboBoxTargetToken_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Core.Web3.IsNetworkConnected)
                return;
            var contract = ((Web3Token)e.AddedItems[0]).Contract;
            
            Task.Run(async ()=>
            {
                _dexToken = await Core.Web3.Client.GenerateToken(contract);
            });
        }

        private BindingList<Risk> _risks = new BindingList<Risk>() { new Risk(), new Risk(), new Risk(), new Risk(), new Risk(), new Risk(), new Risk(), new Risk(), new Risk() };

        private void UiTick(object sender, EventArgs e)
        {
            bool safeToCalculate = _dexToken != null && _book != null && _pPrice > 0 && _kPrice > 0 && _kPrice != 1234 && _pPrice != 1234;

            if (_dexToken != null)
            {
                _pPrice = _dexToken.GetValueInUsd();
            }

            if (_book != null)
            {
                _kPrice = Core.Kucoin.GetPrice(Core.Kucoin.MainOrderBook.Asset);
            }

            var sign = _lastSign;
            if (_pPrice != 1234 && _kPrice != 1234)
               sign = Math.Sign(_pPrice - _kPrice);

            if (sign != _lastSign && _pPrice != 0 && _pPrice !=0)
            {
                var higher = _pPrice > _kPrice ? "Pancake" : "Kucoin";
                Core.TBot.Write("Price flip occurred.\n"+higher +" is on top.\n"+ "Pancake: $" + _pPrice.Round(4)+"\nKucoin: $"+_kPrice.Round(4));
            }
            labelKucoinPrice.Content = _kPrice;
            labelPancakePrice.Content = decimal.Round(_pPrice,4);
            labelSpread.Content = decimal.Round(_spread,4);

            if (_kPrice < _pPrice)
            {
                labelBuySellKucoin.Content = "BUY";
                labelBuySellKucoin.Foreground = Brushes.LimeGreen;

                labelBuySellPancake.Content = "SELL";
                labelBuySellPancake.Foreground = Brushes.OrangeRed;
            }
            else
            {
                labelBuySellKucoin.Content = "SELL";
                labelBuySellKucoin.Foreground = Brushes.OrangeRed;

                labelBuySellPancake.Content = "BUY";
                labelBuySellPancake.Foreground = Brushes.LimeGreen;
            }
            
            labelDelta.Content = decimal.Round(Math.Max(_kPrice,_pPrice)-_spread,4);

            if (safeToCalculate)
            {
                var lastEthPrice = Core.Kucoin.GetPrice("BNB");
                var kucoinAverageTradePrice = decimalUpDownCustomKucoin.Value.HasValue
                    ? (decimalUpDownCustomKucoin.Value.Value > 0 ? decimalUpDownCustomKucoin.Value.Value : _kPrice)
                    : _kPrice;
                var breakEven = _dexToken.QtyAtUsdPriceOrBetter(kucoinAverageTradePrice, lastEthPrice);
                labelBreakEvenTradeValue.Content = decimal.Round(breakEven, 2);
                labelTokenPoolQty.Content = decimal.Round(_dexToken.TokenReserves);
                labelEthPoolQty.Content = decimal.Round(_dexToken.EthReserves, 2);

                if (breakEven == 0)
                    return;

                var maxTradeQty = breakEven / 2;

                _risks[0].Set(maxTradeQty,_dexToken,1,_book);
                _risks[1].Set(maxTradeQty, _dexToken, 0.875m, _book);
                _risks[2].Set(maxTradeQty, _dexToken, 0.75m, _book);
                _risks[3].Set(maxTradeQty, _dexToken, 0.625m, _book);
                _risks[4].Set(maxTradeQty, _dexToken, 0.5m, _book);
                _risks[5].Set(maxTradeQty, _dexToken, 0.375m, _book);
                _risks[6].Set(maxTradeQty, _dexToken, 0.25m, _book);
                _risks[7].Set(maxTradeQty, _dexToken, 0.125m, _book);
                _risks[8].Set(maxTradeQty, _dexToken, 0.0625m, _book);
            }

            _lastSign = sign;

            if (_spamTimer.Elapsed.TotalMinutes > (intUpDownSpamTimer.Value ?? 5) || !_spamTimer.IsRunning)
            {
                decimal profit = 0;
                foreach (var risk in _risks)
                {
                    if (risk.ProfitUsd > (intUpDownUsdAlert.Value ?? 20))
                    {
                        if (risk.Profit > profit)
                            profit = risk.Profit;

                    }
                }

                if (profit > 0)
                {
                    InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData("Show Profit Sheet", "profitsheet");
                    InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(button);
                    Core.TBot.Write("Opportunity Alert - $" + profit.Round(2), replyMarkup: keyboard);
                    _spamTimer.Restart();
                }
            }

        }

        private Stopwatch _spamTimer = new Stopwatch();
        public static decimal CalculatePriceAverageAtBaseVolume(decimal volumeBase, IEnumerable<ISymbolOrderBookEntry> list)
        {

            var totalCost = 0m;
            var totalAmount = 0m;
            var volumeLeft = volumeBase;


            var step = 0;
            while (volumeLeft > 0 && step < list.Count())
            {

                var element = list.ElementAt(step);
                var stepAmount = Math.Min(element.Quantity, volumeLeft);
                totalCost += stepAmount * element.Price;
                volumeLeft -= stepAmount * element.Price;
                totalAmount += stepAmount;
                step++;
            }

            
            return step == 0 ? 1234 : (Math.Round(totalCost/totalAmount, 8));
        }
        public static decimal CalculatePriceAverageAtTokenVolume(decimal volumeToken, IEnumerable<ISymbolOrderBookEntry> list)
        {

            var totalCost = 0m;
            var totalAmount = 0m;
            var volumeLeft = volumeToken;


            var step = 0;
            while (volumeLeft > 0 && step < list.Count())
            {

                var element = list.ElementAt(step);
                var stepAmount = Math.Min(element.Quantity, volumeLeft);
                totalCost += stepAmount * element.Price;
                volumeLeft -= element.Quantity;
                step++;
            }


            return step == 0 ? 1234 : (Math.Round(totalCost / volumeToken, 8));
        }
        public static decimal CalculateQtyAtBaseVolume(decimal volumeUsd, IEnumerable<ISymbolOrderBookEntry> list)
        {

            var totalCost = 0m;
            var totalAmount = 0m;
            var volumeLeft = volumeUsd;


            var step = 0;
            while (volumeLeft > 0 && step < list.Count())
            {

                var element = list.ElementAt(step);
                var stepAmount = Math.Min(element.Quantity, volumeLeft);

                totalAmount += stepAmount;
                volumeLeft -= stepAmount * element.Price;
                step++;
            }


            return totalAmount;
        }

        public static decimal CalculateBaseVolumeAtQty(decimal volumeToken, IEnumerable<ISymbolOrderBookEntry> list)
        {

            var totalCost = 0m;
            var totalAmount = 0m;
            var volumeLeft = volumeToken;


            var step = 0;
            while (volumeLeft > 0 && step < list.Count())
            {

                var element = list.ElementAt(step);
                var stepAmount = Math.Min(element.Quantity, volumeLeft);

                totalAmount += stepAmount * element.Price;
                volumeLeft -= stepAmount;
                step++;
            }


            return totalAmount;
        }
    }



    public class Risk : INotifyPropertyChanged
    {
        public string RiskLevel
        {
            get => _riskLevel;
            set
            {
                if (value != _riskLevel)
                {
                    _riskLevel = value;
                    OnPropertyChanged();
                }

            }
        }

        private string _riskLevel;
        public decimal SellQty
        {
            get => _sellQty;
            set
            {
                if (value.Round(2) != _sellQty)
                {
                    _sellQty = value.Round(2);
                    OnPropertyChanged();
                }

            }
        }
        private decimal _sellQty;
        public decimal Profit
        {
            get => _profit;
            set
            {
                if (value.Round(2) != _profit)
                {
                    _profit = value.Round(2);
                    OnPropertyChanged();
                }

            }
        }
        private decimal _profit;

        public decimal ProfitUsd
        {
            get => _profitUsd;
            set
            {
                if (value.Round(2) != _profitUsd)
                {
                    _profitUsd = value.Round(2);
                    OnPropertyChanged();
                }

            }
        }
        private decimal _profitUsd;

        

        public void Set(decimal maxQty, Token token, decimal multiplier,  KucoinSpotSymbolOrderBook book = null,string riskLevel = null)
        {
            if (maxQty > 0)
            {
                RiskLevel = riskLevel ?? decimal.Round(multiplier, 3).ToString();
                SellQty = maxQty * multiplier;
                var ethReceived = Token.GetAmountOut(SellQty, token.TokenReserves, token.EthReserves);
                var volumeUsd = ethReceived * Core.Kucoin.GetPrice("BNB");
                var targetKucoinQtyPurchase = ProfitSheet.CalculateQtyAtBaseVolume(volumeUsd, book.Asks);
                var cexPriceAverage = ProfitSheet.CalculatePriceAverageAtBaseVolume(targetKucoinQtyPurchase, book.Asks);
                Profit = targetKucoinQtyPurchase - SellQty;
                ProfitUsd = Profit * cexPriceAverage;
            }
            else
            {
                RiskLevel = riskLevel ?? decimal.Round(multiplier, 3).ToString();
                SellQty = Math.Abs(maxQty) * multiplier;
                var ethReceived = ProfitSheet.CalculateBaseVolumeAtQty(SellQty,book.Bids)/Core.Kucoin.GetPrice("BNB");
                var volumeUsd = ethReceived * Core.Kucoin.GetPrice("BNB");
                Profit = Token.GetAmountOut(ethReceived,token.EthReserves,token.TokenReserves) - SellQty;
                ProfitUsd = Profit * token.GetValueInUsd();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
