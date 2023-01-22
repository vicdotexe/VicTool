using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
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
using Nethereum.Signer;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using VicTool.Annotations;
using VicTool.Main;
using VicTool.Main.Misc;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for Web3AccountManager.xaml
    /// </summary>
    [Serializable]
    public class VicAccount
    {
        public string AccountName { get; set; }
        public string PublicAddress { get; set; }
        public string Cipher { get; set; }
    }

    public partial class Web3AccountManager : UserControl, INotifyPropertyChanged
    {
        public string Cipher
        {
            get => _cipher;
            set
            {
                _cipher = value;
                OnPropertyChanged();
            }
        }

        private string _cipher;

        public string PrivateKey
        {
            get => _privateKey;
            set
            {
                _privateKey = value;
                OnPropertyChanged();
            }
        }
        private string _privateKey;

        public string AccountName
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _name;

        public BindingList<VicAccount> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;
            }
        }

        private BindingList<VicAccount> _accounts = new BindingList<VicAccount>();
        public Web3AccountManager()
        {
            FileHelper.DeserializeJsonFile("Files/accountlist.json", out BindingList<VicAccount> accounts);
            foreach (var account in accounts)
                Accounts.Add(account);
            InitializeComponent();
            textBoxPrivateKey.DataContext = this;
            textBoxPassword.DataContext = this;
            textBoxName.DataContext = this;
            //propertyGridAccounts.DataContext = this;
            //propertyGridAccounts.SelectedObject = Accounts;
            
            listboxAccounts.DataContext = this;
            listboxAccounts.SelectionChanged += ListboxAccounts_SelectionChanged;
        }

        private void ListboxAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            var account = (e.AddedItems[0] as VicAccount);
            labelPublicKeyAccounts.Content = account.PublicAddress;
        }

        public void Save()
        {
            Cipher = StringCipher.Encrypt(_privateKey, textBoxPassword.Text);
            var newAccount = new VicAccount()
            {
                AccountName = AccountName,
                Cipher = Cipher,
                PublicAddress = Web3.GetAddressFromPrivateKey(_privateKey)
            };

            Accounts.Add(newAccount);
            PrivateKey = "";
            Accounts.SerializeToJsonFile("Files/accountlist.json");
            Core.Emitter.Emit(CoreEvents.AddRemoveAccounts);
        }

        public void Decrypt()
        {
            PrivateKey = StringCipher.Decrypt(_cipher, textBoxPassword.Text);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void buttonSave(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_privateKey) || string.IsNullOrEmpty(textBoxPassword.Text) ||
                !Web3.IsChecksumAddress(Web3.GetAddressFromPrivateKey(_privateKey)) ||
                Accounts.FirstOrDefault(o => o.AccountName == _name) != null)
            {
                AccountName = "Error";
                return;
            }
            Save();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            buttonSave(sender, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var items = listboxAccounts.SelectedItems;
            if (items.Count > 0)
            {
                Accounts.Remove(items[0] as VicAccount);
            }

            Accounts.SerializeToJsonFile("Files/accountlist.json");
            Core.Emitter.Emit(CoreEvents.AddRemoveAccounts);
        }
    }
}
