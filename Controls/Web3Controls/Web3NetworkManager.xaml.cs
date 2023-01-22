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
using VicTool.Main;
using VicTool.Main.Misc;

namespace VicTool.Controls
{
    public class Web3Network
    {
        public string NetworkName { get; set; }
        public string NetworkUrl { get; set; }
        public string ChainId { get; set; }
        public string CurrencySymbol { get; set; }
        public string WrappedMainContract { get; private set; }

        public Web3Token GetWrappedMainToken()
        {
            return new Web3Token()
            {
                Contract = WrappedMainContract,
                Decimals = 18,
                Name = "Wrapped " + CurrencySymbol,
                Symbol = "w" + CurrencySymbol,
                IsMain = true,
                IsRaw = false
            };
        }

        public Web3Token GetMainToken()
        {
            var token = GetWrappedMainToken();
            token.IsRaw = true;
            token.Name = token.Name.Substring(8);
            token.Symbol = token.Symbol.Substring(1);
            return token;
        }
        
    }
    /// <summary>
    /// Interaction logic for Web3RpcManager.xaml
    /// </summary>
    public partial class Web3RpcManager : UserControl
    {

        public BindingList<Web3Network> Networks { get; set; } = new BindingList<Web3Network>();

        public Web3RpcManager()
        {
            FileHelper.DeserializeJsonFile(Global.Paths.NetworksPath, out List<Web3Network> networks);

            foreach (var network in networks)
                Networks.Add(network);
            //if (Networks.Count==0)
                //Networks.Add(new Web3Network(){NetworkName="New Network"});
            
            InitializeComponent();

            listBoxNetworks.ItemsSource = Networks;
            listBoxNetworks.DisplayMemberPath = "NetworkName";
            listBoxNetworks.SelectionChanged += ListBoxNetworks_SelectionChanged;
            textBoxNetworkName.TextChanged += TextBoxNetworkNameOnTextChanged;
            if (listBoxNetworks.Items.Count>0)
                listBoxNetworks.SelectedItem = listBoxNetworks.Items[0];
        }

        private void TextBoxNetworkNameOnTextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var network in Networks)
            {
                if (network.NetworkName == textBoxNetworkName.Text)
                {
                    buttonAdd.IsEnabled = false;
                    return;
                }

            }

            buttonAdd.IsEnabled = true;
        }

        private void ListBoxNetworks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                SetSelection(e.AddedItems[0] as Web3Network);

        }

        private void SetSelection(Web3Network network)
        {
            textBoxChainID.Text = network.ChainId;
            textBoxCurrencySymbol.Text = network.CurrencySymbol;
            textBoxNetworkName.Text = network.NetworkName;
            textBoxURL.Text = network.NetworkUrl;
        }

        private void Add()
        {
            Web3Network network = new Web3Network()
            {
                ChainId = textBoxChainID.Text,
                NetworkName = textBoxNetworkName.Text,
                CurrencySymbol = textBoxCurrencySymbol.Text,
                NetworkUrl = textBoxURL.Text
            };
            Networks.Add(network);
            Networks.SerializeToJsonFile(Global.Paths.NetworksPath);
            buttonRemove.IsEnabled = true;
            Core.Web3.Emitter.Emit(Web3Events.NetworksAltered);
        }

        public void Remove()
        {
            if (listBoxNetworks.SelectedItem != null)
                Networks.Remove(listBoxNetworks.SelectedItem as Web3Network);
            Networks.SerializeToJsonFile(Global.Paths.NetworksPath);
            if (Networks.Count == 0)
                buttonRemove.IsEnabled = false;
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            Remove();
        }
    }
}
