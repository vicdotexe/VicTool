using System;
using System.Collections.Generic;
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
using VicTool.Main.Kucoin;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for KucoinBaseArbitrage.xaml
    /// </summary>
    public partial class KucoinBaseArbitrage : UserControl
    {
        public KucoinBaseArbitrage()
        {
            InitializeComponent();
            ethPair.SetBaseConversion("ETH-USDT");
            btcPair.SetBaseConversion("BTC-USDT");
        }
    }
}
