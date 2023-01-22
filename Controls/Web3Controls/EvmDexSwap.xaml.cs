using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.EVM;

namespace VicTool.Controls.Web3Controls
{
    /// <summary>
    /// Interaction logic for EvmDexSwap.xaml
    /// </summary>
    public partial class EvmDexSwap : UserControl
    {
        private EVMDex _dex;
        private EvmNetwork _network;
        private List<EvmToken> _tokens;
        public EvmDexSwap()
        {
            InitializeComponent();
            comboBoxNetwork.ItemsSource = Global.Paths.GetNetworksFromFileEVM();
            comboBoxNetwork.SelectionChanged += ComboBoxNetwork_SelectionChanged;
            comboBoxNetwork.DisplayMemberPath = "NetworkName";
            comboBoxDex.SelectionChanged += ComboBoxDex_SelectionChanged;
            comboBoxIn.SelectionChanged += ComboBoxIn_SelectionChanged;
            comboBoxOut.SelectionChanged += ComboBoxOut_SelectionChanged;
            decimalUpDownIn.GotFocus += DecimalUpDownIn_GotFocus;
            decimalUpDownOut.GotFocus += DecimalUpDownOut_GotFocus;
            Task.Run(async () =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while (true)
                {
                    if (_changingDex)
                    {
                        await Task.Delay(50);
                        continue;
                    }
                    if (_changeNetwork != null)
                    {
                        _changingDex = true;
                        Dispatcher.Invoke(_changeNetwork);
                    }

                    watch.Restart();
                    try
                    {
                        if (_dex != null)
                            await _dex.ProcessData();
                    }
                    catch (Exception e)
                    {

                    }

                    var time = 1500 - watch.ElapsedMilliseconds;
                    if (time > 0)
                        await Task.Delay(TimeSpan.FromMilliseconds(time));
                    
                        
                }

            });

            Core.OnUiTick += OnUiTick;
        }

        private void OnUiTick(object sender, EventArgs e)
        {
            var seconds = TrackedRpcClient.TotalTime / 1000;
            var count = TrackedRpcClient.CountTotal;


            labelRefreshTracker.Content = TrackedRpcClient.CountTotal + " (" + count / seconds + "/sec)";
        }

        private bool _changingDex;

        private void DecimalUpDownOut_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_dex != null)
                _dex.IsExactIn = false;
        }

        private void DecimalUpDownIn_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_dex != null)
                _dex.IsExactIn = true;
        }

        private void ComboBoxOut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                var tInfo = e.AddedItems[0] as EvmToken;
                _dex.TokenOut = new EvmToken(tInfo.Name,tInfo.Symbol,tInfo.Decimals,tInfo.Address);
            }
        }

        private void ComboBoxIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                var tInfo = e.AddedItems[0] as EvmToken;
                _dex.TokenIn = new EvmToken(tInfo.Name, tInfo.Symbol, tInfo.Decimals, tInfo.Address);
            }
        }

        private void ComboBoxDex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                _dex = null;
                return;
            }
            var dexInfo = (e.AddedItems[0] as Web3DexInfo);
            _dex = EVMDex.Load(Core.Web3.Account, _network, dexInfo.Router);
            decimalUpDownIn.DataContext = _dex;
            decimalUpDownOut.DataContext = _dex;
            labelMinimumValue.DataContext = _dex;
            labelPriceImpactValue.DataContext = _dex;
            labelInPerOutValue.DataContext = _dex;
            labelOutPerInValue.DataContext = _dex;
            decimalUpDownSlippage.DataContext = _dex;
            _changingDex = false;
            _changeNetwork = null;



            Dispatcher.InvokeAsync(async () =>
            {
                var mainToken = _tokens.FirstOrDefault(o => o.Symbol == _network.CurrencySymbol);
                var wrappedToken = _tokens.FirstOrDefault(o => o.Symbol.ToUpper() == "W" + _network.CurrencySymbol);

                if (wrappedToken == null)
                {
                    wrappedToken = await _dex.Router.GetWrappedTokenAsync(_dex.Web3);
                    _tokens.Insert(0, wrappedToken);
                }

                if (mainToken == null)
                {
                    mainToken = new EvmToken(wrappedToken.Name.Substring(7), wrappedToken.Symbol.Substring(1),
                        wrappedToken.Decimals, wrappedToken.Address);
                    _tokens.Insert(0, mainToken);
                }
                comboBoxIn.ItemsSource = null;
                comboBoxOut.ItemsSource = null;
                comboBoxIn.ItemsSource = _tokens;
                comboBoxOut.ItemsSource = _tokens;
            });


        }

        private Action _changeNetwork;
        private void ComboBoxNetwork_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            _changeNetwork = () =>
            {
                var network = (e.AddedItems[0] as EvmNetwork);
                _network = network;

                var dexs = Global.Paths.GetDexsFromFileByNetwork(network.ChainId);

                comboBoxDex.ItemsSource = dexs;
                comboBoxDex.DisplayMemberPath = "Name";
                comboBoxDex.SelectedItem = comboBoxDex.Items[0];
                
                

                _tokens = Global.Paths.GetTokensFromFileByNetworkEVM(network.ChainId);
                    

                comboBoxIn.ItemsSource = _tokens;
                comboBoxOut.ItemsSource = _tokens;
                comboBoxIn.DisplayMemberPath = "Symbol";
                comboBoxOut.DisplayMemberPath = "Symbol";
            };



        }
    }
}
