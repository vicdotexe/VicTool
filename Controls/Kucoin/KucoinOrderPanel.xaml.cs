using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using CryptoExchange.Net.Objects;
using Kucoin.Net.Enums;
using VicTool.Annotations;
using VicTool.Main;
using VicTool.Main.Kucoin;
using VicTool.Main.Misc;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for KucoinOrderPanel.xaml
    /// </summary>
    public partial class KucoinOrderPanel : UserControl, INotifyPropertyChanged
    {
        private List<string> _symbols = new List<string>();
        private string _currentSymbol;
        private AggregatedOrderBook _orderBook;

        private enum TargetVolume
        {
            Base,
            Token
        }

        private TargetVolume _buyTarget = TargetVolume.Base;
        private TargetVolume _sellTarget = TargetVolume.Token;

        public string Token
        {
            get => _token;
            set
            {
                if (_token == value)
                    return;

                _token = value;
                OnPropertyChanged();
            }
        }
        private string _token = "SOUL";

        public string Base
        {
            get => _base;
            set
            {
                if (_base == value)
                    return;
                _base = value;
                OnPropertyChanged();
            }
        }
        private string _base = "USDT";

        public decimal TokenBalance
        {
            get => _tokenBalance;
            set
            {
                if (_tokenBalance == value)
                    return;
                _tokenBalance = value;
                OnPropertyChanged();
            }
        }
        private decimal _tokenBalance;

        public decimal BaseBalance
        {
            get => _baseBalance;
            set
            {
                if (_baseBalance == value)
                    return;
                _baseBalance = value;
                OnPropertyChanged();
            }
        }
        private decimal _baseBalance;

        public decimal BuyPrice
        {
            get => _buyPrice;
            set
            {
                if (_buyPrice == value)
                    return;
                _buyPrice = value;
                OnPropertyChanged();
            }
        }
        private decimal _buyPrice;

        public decimal BuyVolumeBase
        {
            get => _buyVolumeBase;
            set
            {
                if (_buyVolumeBase == value)
                    return;
                _buyVolumeBase = value;
                OnPropertyChanged();
            }
        }
        private decimal _buyVolumeBase;

        public decimal BuyVolumeToken
        {
            get => _buyVolumeToken;
            set
            {
                if (_buyVolumeToken == value)
                    return;
                _buyVolumeToken = value;
                OnPropertyChanged();
            }
        }
        private decimal _buyVolumeToken;

        public decimal SellPrice
        {
            get => _sellPrice;
            set
            {
                if (_sellPrice == value)
                    return;
                _sellPrice = value;
                OnPropertyChanged();
            }
        }
        private decimal _sellPrice;

        public decimal SellVolumeBase
        {
            get => _sellVolumeBase;
            set
            {
                if (_sellVolumeBase == value)
                    return;
                _sellVolumeBase = value;
                OnPropertyChanged();
            }
        }
        private decimal _sellVolumeBase;

        public decimal SellVolumeToken
        {
            get => _sellVolumeToken;
            set
            {
                if (_sellVolumeToken == value)
                    return;
                _sellVolumeToken = value;
                OnPropertyChanged();
            }
        }
        private decimal _sellVolumeToken;

        public KucoinOrderPanel()
        {
            InitializeComponent();
            Initialize();
            Task.Factory.StartNew(Calculate);
        }

        private async Task Calculate()
        {
            while (true)
            {
                //Buy
                if (_buyTarget == TargetVolume.Base)
                {
                    BuyVolumeToken = ProfitSheet.CalculateQtyAtBaseVolume(BuyVolumeBase, Core.Kucoin.MainOrderBook.Book.Asks).Round(2);
                }
                else
                {
                    BuyVolumeBase =
                        ProfitSheet.CalculateBaseVolumeAtQty(BuyVolumeToken, Core.Kucoin.MainOrderBook.Book.Asks).Round(2);
                }

                if (BuyVolumeBase != 0)
                {
                    var result = Core.Kucoin.MainOrderBook.Book.CalculateAverageFillPrice(BuyVolumeToken,
                        OrderBookEntryType.Ask);
                    if (result.Success)
                    {
                        BuyPrice = result.Data.Round(4);
                    }
                }

                //Sell
                if (_sellTarget == TargetVolume.Base)
                {
                    SellVolumeToken = ProfitSheet.CalculateQtyAtBaseVolume(SellVolumeBase, Core.Kucoin.MainOrderBook.Book.Bids).Round(2);
                }
                else
                {
                    SellVolumeBase =
                        ProfitSheet.CalculateBaseVolumeAtQty(SellVolumeToken, Core.Kucoin.MainOrderBook.Book.Bids).Round(2);
                }

                if (SellVolumeBase != 0)
                {
                    var result = Core.Kucoin.MainOrderBook.Book.CalculateAverageFillPrice(SellVolumeToken,
                        OrderBookEntryType.Bid);
                    if (result.Success)
                    {
                        SellPrice = result.Data.Round(4);
                    }
                }

                await Task.Delay(500);
            }
        }
        private void Initialize()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            _symbols.Add("SOUL-USDT");
            _symbols.Add("BNB-USDT");
            _symbols.Add("SWINGBY-USDT");

            this.DataContext = this;
            comboBoxSymbol.DataContext = this;
            comboBoxSymbol.ItemsSource = _symbols;
            comboBoxSymbol.SelectionChanged += ComboBoxSymbol_SelectionChanged;
            comboBoxSymbol.SelectedItem = comboBoxSymbol.Items[0];

            decimalUpDownBuyAmount.KeyDown += delegate { _buyTarget = TargetVolume.Base; };
            decimalUpDownBuyVolumeToken.KeyDown += delegate { _buyTarget = TargetVolume.Token; };

            decimalUpDownSellAmount.KeyDown += delegate { _sellTarget = TargetVolume.Token; };
            decimalUpDownSellVolumeToken.KeyDown += delegate { _sellTarget = TargetVolume.Base; };
        }


        private void ComboBoxSymbol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentSymbol = (string)comboBoxSymbol.SelectedItem;
            Dispatcher.InvokeAsync(async () =>
            {
                await SelectSymbol(_currentSymbol);
            });
        }

        private async Task SelectSymbol(string symbol)
        {
            var split = _currentSymbol.Split('-');
            Token = split[0];
            Base = split[1];

            var tokenAccount = await Core.Kucoin.RestClient.SpotApi.Account.GetAccountsAsync(_token, AccountType.Trade);
            var baseAccount = await Core.Kucoin.RestClient.SpotApi.Account.GetAccountsAsync(_base, AccountType.Trade);

            if (tokenAccount.Success)
                TokenBalance = tokenAccount.Data.First().Available.Round(4);

            if (baseAccount.Success)
                BaseBalance = baseAccount.Data.First().Available.Round(4);
            Core.Kucoin.MainOrderBook.SetPair(symbol);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
