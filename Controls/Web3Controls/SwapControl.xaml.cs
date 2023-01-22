using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Telegram.Bot.Types;
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.Misc;
using VicTool.Main.Swap;
using VicTool.Windows;
using File = System.IO.File;

namespace VicTool.Controls
{

    /// <summary>
    /// Interaction logic for SwapControl.xaml
    /// </summary>
    public partial class SwapControl : UserControl
    {
        private SwapManager _swapManager;
        private bool _isExactIn => radioButtonIn.IsChecked.Value;
        private bool _noise;
        private int _gasPrice = 5;
        private decimal _slippage = 0.005m;
        private string _sendToAddress = "0x5559c885b89865CF30e1eF3e191665fFbc9Ffba0";
        private bool _sendOnCompletion;
        private BindingList<Web3Token> _installedTokens = new BindingList<Web3Token>();

        private void RefreshTokenOptions()
        {
            _installedTokens.Clear();
            foreach(var token in Global.Paths.GetTokensFromFileByNetwork(Core.Web3.Network))
                _installedTokens.Add(token);
        }

        public SwapControl()
        {
            InitializeComponent();
            Initialize();
            Core.Web3.Emitter.AddObserver(Web3Events.NetworkConnected, RefreshTokenOptions);
        }

        #region Control Events
        private void CheckBoxSendTo_Changed(object sender, RoutedEventArgs e)
        {
            _sendOnCompletion = checkBoxSendTo.IsChecked.Value;
        }

        private void IntegerUpDownGas_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _gasPrice = integerUpDownGas.Value ?? 5;
        }

        private void DecimalUpDownSlippage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _slippage = decimalUpDownSlippage.Value / 100m ?? 0.005m;
        }

        private void ComboBoxIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_noise)
                return;
            var contract = ((Web3Token)e.AddedItems[0]).Contract;
            _swapManager.SetTokenIn(contract);

        }
        private void ComboBoxOut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_noise)
                return;
            var contract = ((Web3Token)e.AddedItems[0]).Contract;
            _swapManager.SetTokenOut(contract);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            radioButtonIn.IsChecked = !radioButtonIn.IsChecked;
            radioButtonOut.IsChecked = !radioButtonIn.IsChecked;
            (decimalUpDownIn.Value, decimalUpDownOut.Value) = (decimalUpDownOut.Value, decimalUpDownIn.Value);
            _noise = true;
            (comboBoxIn.SelectedValue, comboBoxOut.SelectedValue) = (comboBoxOut.SelectedValue, comboBoxIn.SelectedValue);
            _noise = false;
            _swapManager.Flip();
        }

        private void buttonSwap_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(PerformSwap);
        }

        #endregion

        private void Initialize()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            DataContext = this;

            //swingby 0x71de20e0c4616e7fcbfdd3f875d568492cbe4739
            comboBoxIn.ItemsSource = _installedTokens;
            comboBoxIn.DisplayMemberPath = "Symbol";

            comboBoxOut.ItemsSource = _installedTokens;
            comboBoxOut.DisplayMemberPath = "Symbol";

            comboBoxIn.SelectionChanged += ComboBoxIn_SelectionChanged;
            comboBoxOut.SelectionChanged += ComboBoxOut_SelectionChanged;
            decimalUpDownSlippage.ValueChanged += DecimalUpDownSlippage_ValueChanged;
            decimalUpDownIn.KeyDown += delegate { radioButtonIn.IsChecked = true; };
            decimalUpDownOut.KeyDown += delegate { radioButtonOut.IsChecked = true; };
            integerUpDownGas.ValueChanged += IntegerUpDownGas_ValueChanged;
            textBoxSendTo.Text = _sendToAddress;
            checkBoxSendTo.Checked += CheckBoxSendTo_Changed;
            checkBoxSendTo.Unchecked += CheckBoxSendTo_Changed;

            _swapManager = new SwapManager();
            Core.OnUiTick += UiTick;

            Core.TBot.RecievedInput += (s, e) =>
            {
                Dispatcher.Invoke(
                    delegate
                    {
                        OnTelegramInput(e);
                    });
            };

            Core.TBot.OnCallBack += (s, e) =>
            {
                Dispatcher.Invoke(
                    delegate
                    {
                        OnTelegramCallBack(e);
                    });
            };
        }

        private void UiTick(object sender, EventArgs e)
        {

            labelRefreshTracker.Content = _swapManager.RefreshTime;

            if (_swapManager.TokenIn != null)
                labelInBalance.Content = "($" + decimal.Round(_swapManager.LastTokenInBalance * _swapManager.TokenIn.GetValueInUsd(), 2) + ") " + decimal.Round(_swapManager.LastTokenInBalance, 4);
            if (_swapManager.TokenOut != null)
                labelOutBalance.Content = "($" + decimal.Round(_swapManager.LastTokenOutBalance * _swapManager.TokenOut.GetValueInUsd(), 2) + ") " + decimal.Round(_swapManager.LastTokenOutBalance, 4);

            var slippage = decimalUpDownSlippage.Value / 100 ?? 0.0003m;

            if (_swapManager.IsPairValid && _swapManager.IsPairSetup)
            {
                decimal quote;
                decimal minimum;

                if (_isExactIn && decimalUpDownIn.Value > 0)
                {
                    _swapManager.SetAmountIn(decimalUpDownIn.Value ?? 0);
                    decimalUpDownOut.Value = _swapManager.AmountOut;
                    labelMinimum.Content = "Minimum Received:";
                    minimum = _swapManager.AmountOut * (1 - slippage);
                    labelMinimumValue.Content = minimum.ToString().Truncate(6) + " " + _swapManager.TokenOut?.Ticker;
                    if (_swapManager.TokenIn != null && _swapManager.LastSwapChain.Count > 0)
                    {
                        var quotes = Token.QuoteChain(_swapManager.AmountIn, _swapManager.LastSwapChain);
                        quote = quotes.Last();

                        var impact = 1 - (_swapManager.AmountOut / quote);
                        impact *= 100;
                        labelPriceImpactValue.Content = decimal.Round(impact, 2) + "%";
                    }
                }
                else if (!_isExactIn && decimalUpDownOut.Value > 0)
                {
                    _swapManager.SetAmountOut(decimalUpDownOut.Value ?? 0);
                    decimalUpDownIn.Value = _swapManager.AmountIn;
                    labelMinimum.Content = "Minimum Sold:";
                    minimum = _swapManager.AmountIn * (1 - slippage);
                    labelMinimumValue.Content = minimum.ToString().Truncate(6) + " " + _swapManager.TokenIn?.Ticker;
                    if (_swapManager.TokenOut != null && _swapManager.LastSwapChain.Count > 0)
                    {
                        //var newList = new List<Token>();
                        //newList.AddRange(_swapManager.LastSwapChain);
                        //newList.Reverse();
                        //var quotes = Token.QuoteChain(_swapManager.AmountOut, newList);
                        var quotes = Token.QuoteChainReverse(_swapManager.AmountOut, _swapManager.LastSwapChain);
                        quote = quotes.Last();

                        var impact = 1 - (quote / _swapManager.AmountIn);
                        impact *= 100;
                        labelPriceImpactValue.Content = decimal.Round(impact, 2) + "%";
                    }
                }
            }

            labelInvalidPair.Visibility = _swapManager.IsPairValid ? Visibility.Hidden : Visibility.Visible;
            labelInPrice.Content = _swapManager.TokenIn?.Ticker;
            labelInPriceValue.Content = "$"+_swapManager.TokenIn?.GetValueInUsd().ToString().Truncate(6);
            labelOutPrice.Content = _swapManager.TokenOut?.Ticker;
            labelOutPriceValue.Content = "$" + _swapManager.TokenOut?.GetValueInUsd().ToString().Truncate(6);



        }

        private async Task<TransactionReceipt> PerformSwap()
        {
            var routing = _sendOnCompletion ? _sendToAddress : null;
            return await _swapManager.PerformSwap(_gasPrice,_slippage, routing);
        }

        private void OnTelegramCallBack(CallbackQuery callBack)
        {
            if (callBack.Data == null)
                return;
        }

        private DateTime _swapEntered;
        private void OnTelegramInput(string message)
        {
            if (message == "/swapshot")
            {
                Core.TBot.SendSnapshot(this);
            }

            if (message == "/prices")
            {

                Core.TBot.Write(
                    "BNB: $" + Core.Kucoin.GetPrice("BNB", 2) + "\n" +
                    "Target(DEX): $" + ProfitSheet.TargetToken.GetValueInUsd().Round(4) + "\n" +
                    "Target(CEX): $" + ProfitSheet.KucoinPrice
                );
            }

            if (message == "/swap")
            {
                Core.TBot.Write("command -> /swap:(amount):($IN):($OUT)");
            }
            if (message.Contains("/swap:"))
            {
                var commands = message.Replace("/swap:", "").Split(':');
                var amount = decimal.Parse(commands[0]);
                var tokenIn = commands[1].ToUpper();
                var tokenOut = commands[2].ToUpper();

                decimalUpDownIn.Value = amount;
                int index = 0;
                foreach (var item in comboBoxIn.ItemsSource)
                {
                    var pair = (KeyValuePair<string, string>)item;
                    if (tokenIn == pair.Key)
                    {
                        comboBoxIn.SelectedItem = comboBoxIn.Items[index];
                    }
                    index++;
                }

                index = 0;
                foreach (var item in comboBoxIn.ItemsSource)
                {
                    var pair = (KeyValuePair<string, string>)item;
                    if (tokenOut == pair.Key)
                    {
                        comboBoxOut.SelectedItem = comboBoxIn.Items[index];
                    }
                    index++;
                }

                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(5000);
                    Core.TBot.SendSnapshot(this);
                    Core.TBot.Write("Type '/confirm' to attempt the swap.");
                });
                _swapEntered = DateTime.Now;
            }

            if (message == "/confirm")
            {
                if (DateTime.Now - _swapEntered <= TimeSpan.FromSeconds(20))
                {
                    Core.TBot.Write("Submitting Swap...");
                    Dispatcher.InvokeAsync(async() =>
                    {
                        var receipt = await PerformSwap();
                        if (receipt != null && receipt.Succeeded())
                        {
                            Core.TBot.Write("Transaction Succeeded.");
                        }
                        else
                        {
                            Core.TBot.Write("Transaction Failed.");
                        }
                        
                    });
                }
                else
                {
                    Core.TBot.Write("Last swap has expired.");
                }
            }
        }

    }
}
