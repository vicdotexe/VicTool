using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace VicTool.Windows
{
    /// <summary>
    /// Interaction logic for AddAccountWindow.xaml
    /// </summary>
    public partial class AddAccountWindow : Window
    {
        public AddAccountWindow()
        {
            InitializeComponent();
        }

        private void buttonConfirm_Click(object sender, RoutedEventArgs e)
        {
            var name = textBoxName.Text;
            var key = textBoxPrivateKey.Text;

            string json = File.ReadAllText("Files/accountlist.json");
            var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(json);
            if (jsonObj == null)
                jsonObj = new Dictionary<string, string>();
            if (jsonObj.ContainsKey(name))
            {
                MessageBoxResult result = MessageBox.Show("Name already taken, overwrite?", "Conflict",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return;
            }
            jsonObj[name] = key;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("Files/accountlist.json", output);
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
