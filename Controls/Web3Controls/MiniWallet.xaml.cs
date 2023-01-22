using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
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
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.Swap;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for MiniWallet.xaml
    /// </summary>
    public partial class MiniWallet : UserControl
    {
        private SwapControl _swapControl;
        private SwapManager _swapManager;

        private int _updateFrequency = 1000;
        private Dictionary<string, string> _contracts = new Dictionary<string, string>();
        private Token _asset;
        private string _targetAsset;
        private decimal _assetBalance;
        private string _toAddress;
        private decimal _gasPrice = 5;
        private decimal _gasLimit = 21000;
        private decimal _qty;
        public bool noise;

        public MiniWallet()
        {
            InitializeComponent();
            Initialize();

        }

        private void Initialize()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            this.Loaded += delegate
            {
                _swapControl = (Window.GetWindow(this) as MainWindow).SwapControl;
            };

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync();

            Core.OnUiTick += UiTick;

            _contracts.Add("SOUL", Global.Contracts.MainNet.SOUL);
            _contracts.Add("WBNB", Global.Contracts.MainNet.WBNB);
            _contracts.Add("BUSD", Global.Contracts.MainNet.BUSD);
            _contracts.Add("SWINGBY", "0x71de20e0c4616e7fcbfdd3f875d568492cbe4739");
            comboBoxAsset.ItemsSource = _contracts;
            comboBoxAsset.DisplayMemberPath = "Key";
            comboBoxAsset.SelectionChanged += ComboBoxAsset_SelectionChanged;
            textBoxTo.TextChanged += TextBoxTo_TextChanged;
            comboBoxTo.DisplayMemberPath = "Key";
            comboBoxTo.SelectionChanged += ComboBoxTo_SelectionChanged;
            decimalUpDownGasPrice.ValueChanged += DecimalUpDownGasPrice_ValueChanged;
            decimalUpDownQty.ValueChanged += DecimalUpDownQty_ValueChanged;
            decimalUpDownGasLimit.ValueChanged += DecimalUpDownGasLimit_ValueChanged;
            Core.OnUiTick += UiLoopTimer_Tick;
        }
        private void DecimalUpDownGasLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _gasLimit = (decimal)decimalUpDownGasLimit.Value;
        }

        private void UiLoopTimer_Tick(object sender, EventArgs e)
        {
            var price = Web3.Convert.ToWei(_gasPrice, UnitConversion.EthUnit.Gwei);
            var limit = Web3.Convert.ToWei(_gasLimit, UnitConversion.EthUnit.Wei);
            var maxFee = price * limit;


            var feeInEth = Web3.Convert.FromWei(maxFee, UnitConversion.EthUnit.Ether);
            var feeInUsd = decimal.Round(feeInEth * Core.Kucoin.GetPrice("BNB"),4);

            textBlockMaxFee.Text = "Max Fee: " + decimal.Round(feeInEth, 6) + " ($" + feeInUsd + ")";
        }

        private void DecimalUpDownQty_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _qty = (decimal)decimalUpDownQty.Value;
        }

        private void DecimalUpDownGasPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _gasPrice = (decimal)decimalUpDownGasPrice.Value;
        }

        private void ComboBoxTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var pair = (KeyValuePair<string, string>)e.AddedItems[0];
                noise = true;
                textBoxTo.Text = Web3.GetAddressFromPrivateKey(pair.Value);
                noise = false;
            }
        }

        private void TextBoxTo_TextChanged(object sender, TextChangedEventArgs e)
        {
            _toAddress = textBoxTo.Text;
            buttonSend.IsEnabled = (Core.Web3.Account.Address != _toAddress) && Web3.IsChecksumAddress(_toAddress);
            
            if (noise)
                return;
            bool foundMatch = false;
            foreach (var item in comboBoxTo.Items)
            {
                var pair = (KeyValuePair<string,string>) item;
                if (Web3.GetAddressFromPrivateKey(pair.Value) == _toAddress)
                {
                    comboBoxTo.SelectedItem = pair;
                    foundMatch = true;
                }
            }

            if (!foundMatch)
            {
                comboBoxTo.Text = "New";
            }
        }

        private void ComboBoxAsset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pair = (KeyValuePair<string, string>)e.AddedItems[0];
            _targetAsset = pair.Value;

        }

        private void UiTick(object sender, EventArgs e)
        {
            textBlockAssetBalance.Text = _assetBalance.ToString();
            decimalUpDownQty.Maximum = (double)_assetBalance;
        }

        private BigInteger _tempWeiBalance;
        private async void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (Core.Web3.Client != null)
                {
                    if (_asset == null)
                    {
                        if (!string.IsNullOrEmpty(_targetAsset))
                            _asset = await Core.Web3.Client.GenerateToken(_targetAsset);
                    }
                    else
                    {
                        if (_asset.Contract != _targetAsset)
                            _asset = await Core.Web3.Client.GenerateToken(_targetAsset);
                    }

                    if (_asset != null)
                    {
                        await _asset.Update(Core.Web3.Client);

                        if (Core.Web3.IsAccountConnected)
                        {
                            _tempWeiBalance = 0;

                            if (_asset.IsRaw)
                            {
                                _tempWeiBalance = await Core.Web3.Client.Eth.GetBalance.SendRequestAsync(Core.Web3.Account.Address);
                                _assetBalance = Web3.Convert.FromWei(_tempWeiBalance);
                            }
                            else
                            {
                                _tempWeiBalance = await Core.Web3.Client.Eth.ERC20.GetContractService(_asset.Contract)
                                    .BalanceOfQueryAsync(Core.Web3.Account.Address);
                                _assetBalance = Web3.Convert.FromWei(_tempWeiBalance,_asset.Decimals);
                            }

                            

                        }
                    }
                }

                await Task.Delay(_updateFrequency);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(Send);
        }



        private async void Send()
        {

            if (_asset.IsRaw)
            {
                Com.WriteLine("Submitting transaction: Transfer " + _qty + " " + _asset.Ticker + " from " + Core.Web3.PublicAddress + " to " +
                              _toAddress);
                Com.WriteLine("Awaiting results...");
                var transactionReceipt = await Core.Web3.Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(_toAddress, _qty, _gasPrice);

                var success = transactionReceipt.Succeeded();
                Com.WriteLine("Transaction " + (success ? "Successful" : "Failed"));
            }
            else
            {
                Com.WriteLine("Submitting transaction: Transfer " + _qty + " " + _asset.Ticker + " from " + Core.Web3.PublicAddress + " to " +
                              _toAddress);
                Com.WriteLine("Awaiting results...");


                
                var transactionMessage = new TransferFunction
                {

                    To = _toAddress,
                    Value = Web3.Convert.ToWei(_qty, _asset.Decimals)
                    //FromAddress = _account.Address

                };
                var transactionHandler = Core.Web3.Client.Eth.GetContractTransactionHandler<TransferFunction>();

                var gasPrice = Web3.Convert.ToWei(_gasPrice, UnitConversion.EthUnit.Gwei);
                
                var nonce = await Core.Web3.Account.NonceService.GetNextNonceAsync();

                transactionMessage.Nonce = nonce;
                transactionMessage.GasPrice = gasPrice;
                var estimate = await transactionHandler.EstimateGasAsync(_asset.Contract, transactionMessage);
                transactionMessage.Gas = estimate.Value;
               
                var transferReceipt = await transactionHandler.SendRequestAndWaitForReceiptAsync(_asset.Contract,transactionMessage);
                
                var success = transferReceipt.Succeeded();

                Com.WriteLine("Transaction " + (success ? "Successful" : "Failed"));

                if (success)
                {
                    var transaction =
                        await Core.Web3.Client.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transferReceipt.TransactionHash);

                    var transferDecoded = transaction.DecodeTransactionToFunctionMessage<TransferFunction>();
                    Com.WriteLine(transferDecoded.ToString());
                }

            }

        }
    }
}
