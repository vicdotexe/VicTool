using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.EVM;
using VicTool.Main.Misc;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;

namespace VicTool.Controls.Web3Controls
{
    /// <summary>
    /// Interaction logic for Web3DexSwap.xaml
    /// </summary>


    public partial class Web3DexSwap : UserControl
    {
        public Web3DexRouter _dexRouter { get; set; }
        public decimal Slippage { get; set; }
        public decimal MinimumAmount { get; set; }
        public decimal PriceImpact { get; set; }

        private Stopwatch stopwatch = new Stopwatch();
        public Web3DexSwap()
        {
            InitializeComponent();
            comboBoxNetwork.ItemsSource = Global.Paths.GetNetworksFromFile();
            comboBoxNetwork.SelectionChanged += ComboBoxNetwork_SelectionChanged;
            comboBoxNetwork.DisplayMemberPath = "NetworkName";
            comboBoxDex.SelectionChanged += ComboBoxDex_SelectionChanged;
            comboBoxIn.SelectionChanged += ComboBoxIn_SelectionChanged;
            comboBoxOut.SelectionChanged += ComboBoxOut_SelectionChanged;
            decimalUpDownIn.GotFocus += DecimalUpDownIn_GotFocus;
            decimalUpDownOut.GotFocus += DecimalUpDownOut_GotFocus;

            DecimalUpDown dec;
            Core.OnUiTick += OnUiTick;
            
            Dispatcher.InvokeAsync(async () =>
            {
                stopwatch.Start();
                while (true)
                {
                    stopwatch.Restart();
                    if (_dexRouter != null)
                        await _dexRouter.ProcessData();
                    if (stopwatch.ElapsedMilliseconds < 2000)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(2000-stopwatch.ElapsedMilliseconds));
                    }
                    
                }
            });
        }

        private void DecimalUpDownOut_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_dexRouter != null)
                _dexRouter.IsExactIn = false;
        }

        private void DecimalUpDownIn_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_dexRouter != null)
                _dexRouter.IsExactIn = true;
        }

        private void OnUiTick(object sender, EventArgs e)
        {
            _dexRouter?.CalculateAmounts();
            if (_dexRouter != null)
                labelInvalidPair.Visibility = _dexRouter.PairValid ? Visibility.Hidden : Visibility.Visible;
            else
                labelInvalidPair.Visibility = Visibility.Visible;

            var seconds = TrackedRpcClient.TotalTime / 1000;
            var count = TrackedRpcClient.CountTotal;


            labelRefreshTracker.Content = TrackedRpcClient.CountTotal + " (" + count/seconds + "/sec)";
        }

        private void CalculateSummary()
        {
            if (_dexRouter == null)
                return;
            var amountIn = _dexRouter.AmountIn;
            var amountOut = _dexRouter.AmountOut;

            if (amountIn == 0 || amountOut == 0)
                return;

            if (_dexRouter.IsExactIn)
                MinimumAmount = amountOut * (1-(Slippage / 100));
            else
                MinimumAmount = amountIn * (1+(Slippage / 100));
        }

        private void ComboBoxOut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
                _dexRouter.TokenOut = e.AddedItems[0] as Web3Token;
        }

        private void ComboBoxIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
                _dexRouter.TokenIn = e.AddedItems[0] as Web3Token;
        }

        private void ComboBoxDex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                _dexRouter.SetDex(null);
                return;
            }
            var dex = (e.AddedItems[0] as Web3DexInfo);
            _dexRouter.SetDex(dex);
        }

        private void ComboBoxNetwork_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var network = (e.AddedItems[0] as Web3Network);
            _dexRouter = new Web3DexRouter(network);
            decimalUpDownIn.DataContext = _dexRouter;
            decimalUpDownOut.DataContext = _dexRouter;
            labelMinimumValue.DataContext = _dexRouter;
            labelPriceImpactValue.DataContext = _dexRouter;
            labelInPerOutValue.DataContext = _dexRouter;
            labelOutPerInValue.DataContext = _dexRouter;
            decimalUpDownSlippage.DataContext = _dexRouter;

            comboBoxDex.ItemsSource = Global.Paths.GetDexsFromFileByNetwork(network);
            comboBoxDex.DisplayMemberPath = "Name";
            var tokens = Global.Paths.GetTokensFromFileByNetwork(network);
            tokens.Insert(0,network.GetWrappedMainToken());
            tokens.Insert(0,network.GetMainToken());

            comboBoxIn.ItemsSource = tokens;
            comboBoxOut.ItemsSource = tokens;
            comboBoxIn.DisplayMemberPath = "Symbol";
            comboBoxOut.DisplayMemberPath = "Symbol";

        }

        private void buttonFlip_Click(object sender, RoutedEventArgs e)
        {
            var exactIn = _dexRouter.IsExactIn;
            (decimalUpDownIn.Value, decimalUpDownOut.Value) = (decimalUpDownOut.Value, decimalUpDownIn.Value);
            (comboBoxIn.SelectedItem, comboBoxOut.SelectedItem) = (comboBoxOut.SelectedItem, comboBoxIn.SelectedItem);
            _dexRouter.IsExactIn = !exactIn;
        }

        private void buttonSwap_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
