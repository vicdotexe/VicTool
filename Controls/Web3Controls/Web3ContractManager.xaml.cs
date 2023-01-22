using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Nethereum.Util;
using Nethereum.Web3;
using Org.BouncyCastle.Bcpg.OpenPgp;
using VicTool.Main;
using VicTool.Main.Eth;
using VicTool.Main.Misc;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for Web3ContractManager.xaml
    /// </summary>
    public partial class Web3ContractManager : UserControl
    {
        public Dictionary<string, BindingList<Web3Token>> _tokens = new();
        public Dictionary<string, BindingList<Web3DexInfo>> _dexs = new();

        public BindingList<Web3Network> _networks = new();

        private Web3Network _currentNetwork;
        private Web3 _web3;

        public Web3ContractManager()
        {
            InitializeComponent();
            RefreshNetworks();
            FileHelper.DeserializeJsonFile(Global.Paths.TokensPath, out Dictionary<string, BindingList<Web3Token>> tokens);
            foreach (var list in tokens)
            {
                _tokens.Add(list.Key, new BindingList<Web3Token>());
                foreach (var token in list.Value)
                {
                    _tokens[list.Key].Add(token);
                }
            }

            FileHelper.DeserializeJsonFile(Global.Paths.DexsPath, out Dictionary<string, List<Web3DexInfo>> dexs);
            foreach (var list in dexs)
            {
                _dexs.Add(list.Key, new BindingList<Web3DexInfo>());
                foreach (var dex in list.Value)
                {
                    _dexs[list.Key].Add(dex);
                }
            }

            listboxNetwork.ItemsSource = _networks;
            listboxNetwork.DisplayMemberPath = "NetworkName";
            listboxNetwork.SelectionChanged += ListboxNetwork_SelectionChanged;
            listboxNetwork.SelectedItem = listboxNetwork.Items[0];

            textBoxTokenContract.TextChanged += TextBoxTokenContract_TextChanged;
            buttonAddToken.Click += ButtonAddToken_Click;
            textBoxTokenName.IsEnabled = false;
            textBoxSymbol.IsEnabled = false;
            textBoxDecimals.IsEnabled = false;
            listBoxTokens.SelectionChanged += ListBoxTokens_SelectionChanged;
            buttonAddToken.IsEnabled = false;

            buttonAddDex.Click += ButtonAddDex_Click;

            
        }

        private void ButtonAddDex_Click(object sender, RoutedEventArgs e)
        {
            _dexs[_currentNetwork.NetworkUrl].Add(new Web3DexInfo()
            {
                Factory = textBoxFactory.Text,
                Router = textBoxRouter.Text,
                Name = textBoxDexName.Text
            });
            _dexs.SerializeToJsonFile(Global.Paths.DexsPath);
        }

        private void ListBoxTokens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var selection = e.AddedItems[0] as Web3Token;

            textBoxTokenName.Text = selection.Name;
            textBoxSymbol.Text = selection.Symbol;
            textBoxDecimals.Text = selection.Decimals.ToString();
            textBoxTokenContract.Text = selection.Contract;
        }

        private void ButtonAddToken_Click(object sender, RoutedEventArgs e)
        {
            _tokens[_currentNetwork.NetworkUrl].Add(new Web3Token()
            {
                Contract=textBoxTokenContract.Text,
                Decimals=int.Parse(textBoxDecimals.Text),
                Name=textBoxTokenName.Text,
                Symbol=textBoxSymbol.Text
            });
            buttonAddToken.IsEnabled = false;
            _tokens.SerializeToJsonFile(Global.Paths.TokensPath);
        }

        private void TextBoxTokenContract_TextChanged(object sender, TextChangedEventArgs e)
        {
            buttonAddToken.IsEnabled = false;
            foreach (var token in _tokens[_currentNetwork.NetworkUrl])
            {
                if (token.Contract == textBoxTokenContract.Text)
                {
                    buttonAddToken.IsEnabled = false;
                    //textBoxTokenName.IsEnabled = false;
                    //textBoxSymbol.IsEnabled = false;
                    //textBoxDecimals.IsEnabled = false;
                    return;
                }
            }
            

            textBoxSymbol.Text = "";
            textBoxTokenName.Text = "";
            textBoxDecimals.Text = "";

            var contractInput = textBoxTokenContract.Text;

            if (contractInput.IsValidEthereumAddressLength() && contractInput.IsValidEthereumAddressHexFormat())
            {

                Dispatcher.InvokeAsync(async()=>
                {
                    await GetInfo(contractInput);
                });
            }
            else
            {
                buttonAddToken.IsEnabled = false;
            }

            

        }

        private async Task GetInfo(string contract)
        {
            int decimals = 0;
            try
            {
                decimals = await _web3.Eth.ERC20.GetContractService(contract)
                    .DecimalsQueryAsync();
            }
            catch (Exception e)
            {

            }
            
            var symbol = await _web3.Eth.ERC20.GetContractService(contract).SymbolQueryAsync();
            var name = await _web3.Eth.ERC20.GetContractService(contract).NameQueryAsync();
            OnReturnInfo(name, symbol, decimals);
        }
        private void OnReturnInfo(string name, string symbol, int decimals)
        {
            Dispatcher.Invoke(() =>
            {
                textBoxSymbol.Text = symbol;
                textBoxTokenName.Text = name;
                textBoxDecimals.Text = decimals.ToString();
                buttonAddToken.IsEnabled = true;
            });
        }

        private void ListboxNetwork_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentNetwork = e.AddedItems[0] as Web3Network;
            if (_currentNetwork != null)
                _web3 = new Web3(_currentNetwork.NetworkUrl);
            if (!_tokens.ContainsKey(_currentNetwork.NetworkUrl))
                _tokens.Add(_currentNetwork.NetworkUrl,new BindingList<Web3Token>());
            if (!_dexs.ContainsKey(_currentNetwork.NetworkUrl))
                _dexs.Add(_currentNetwork.NetworkUrl, new BindingList<Web3DexInfo>());

            listBoxTokens.ItemsSource = _tokens[_currentNetwork.NetworkUrl];
            listBoxTokens.DisplayMemberPath = "Symbol";

            listBoxDexs.ItemsSource = _dexs[_currentNetwork.NetworkUrl];
            listBoxDexs.DisplayMemberPath = "Name";

            _web3.TransactionManager.UseLegacyAsDefault = true;

        }

        private void RefreshNetworks()
        {
            _networks.Clear();
            FileHelper.DeserializeJsonFile(Global.Paths.NetworksPath, out List<Web3Network> networks);
            foreach (var network in networks)
                _networks.Add(network);
        }
    }

    public class Web3Token : ICloneable
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
        public string Contract { get; set; }
        public bool IsMain { get; set; }
        public bool IsRaw { get; set; }

        public object Clone()
        {
            return new Web3Token()
            {
                Name = Name,
                Symbol = Symbol,
                Decimals = Decimals,
                Contract = Contract,
                IsMain = IsMain,
                IsRaw = IsRaw
            };
        }
    }

}
