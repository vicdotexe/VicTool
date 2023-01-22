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
using Nethereum.Web3.Accounts;
using Nethereum.Web3.Accounts.Managed;
using VicTool.Main;
using VicTool.Main.Misc;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for Web3ConnectionManager.xaml
    /// </summary>
    public partial class Web3ConnectionManager : UserControl
    {
        private BindingList<VicAccount> _accounts = new BindingList<VicAccount>();
        private BindingList<Web3Network> _networks = new BindingList<Web3Network>();

        public Web3ConnectionManager()
        {

            InitializeComponent();
            OnAccountsAddRemove();

            FileHelper.DeserializeJsonFile(Global.Paths.NetworksPath, out List<Web3Network> networks);
            foreach (var network in networks)
                _networks.Add(network);
            Core.Web3.Emitter.AddObserver(Web3Events.NetworksAltered, () =>
            {
                _networks.Clear();
                foreach (var network in Global.Paths.GetNetworksFromFile())
                    _networks.Add(network);
            });

            Core.Emitter.AddObserver(CoreEvents.AddRemoveAccounts, OnAccountsAddRemove);
            buttonConnect.Click += ButtonConnect_Click;
            comboBoxAccounts.SelectionChanged += ComboBoxAccounts_SelectionChanged;
            comboBoxAccounts.ItemsSource = _accounts;
            comboBoxNetwork.SelectionChanged += ComboBoxNetwork_SelectionChanged;
            comboBoxNetwork.ItemsSource = _networks;
            comboBoxNetwork.DisplayMemberPath = "NetworkName";


            if (comboBoxNetwork.Items.Count > 0)
                comboBoxNetwork.SelectedItem = comboBoxNetwork.Items[0];
            Core.Web3.Emitter.AddObserver(Web3Events.BalanceUpdate,OnBalanceChange);
        }

        private void OnBalanceChange()
        {
            Dispatcher.Invoke(()=>textBlockBalance.Text = Core.Web3.CurrentBalance.ToString());
        }

        private void ComboBoxNetwork_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            var network = e.AddedItems[0] as Web3Network;
            Core.Web3.Network = network;
        }

        private void ComboBoxAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Disconnect();
        }

        

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (buttonConnect.Content == "Connect")
            {
                if (string.IsNullOrEmpty(textBoxPassword.Text))
                    return;

                var account = comboBoxAccounts.SelectedItem as VicAccount;
                string privateKey = null;

                try
                {
                    privateKey = StringCipher.Decrypt(account.Cipher, textBoxPassword.Text);
                }
                catch
                {

                }

                if (privateKey != null)
                {
                    textBoxPassword.Text = "Connected";
                    textBoxPassword.IsEnabled = false;
                    circleConnection.Fill = Brushes.DarkSeaGreen;
                    buttonConnect.Content = "Disconnect";
                    Core.Web3.ConnectAccount(privateKey, account.AccountName);
                }
                else
                {
                    textBoxPassword.Text = "Wrong Password";
                }
            }
            else
            {
                Disconnect();
            }


        }

        private void Disconnect()
        {
            circleConnection.Fill = Brushes.IndianRed;
            buttonConnect.Content = "Connect";
            textBoxPassword.Text = "";
            textBoxPassword.IsEnabled = true;
            Core.Web3.DisconnectAccount();
        }
        private void OnAccountsAddRemove()
        {
            FileHelper.DeserializeJsonFile(Global.Paths.AccountsPath, out List<VicAccount> accounts);
            _accounts.Clear();
            foreach (var account in accounts)
                _accounts.Add(account);
        }

    }
}
