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
using System.Windows.Threading;
using Nethereum.Web3.Accounts.Managed;
using Org.BouncyCastle.Bcpg;
using VicTool.Controls;
using VicTool.Main;

using System.Security.Cryptography;


namespace VicTool
{
    public struct ValuePair<T> : IEquatable<ValuePair<T>>
    {
        public T ObjectA { get; set; }
        public T ObjectB { get; set; }

        public ValuePair(T objectA, T objectB)
        {
            ObjectA = objectA;
            ObjectB = objectB;
        }

        public static bool operator ==(ValuePair<T> pairA, ValuePair<T> pairB)
        {
            return pairA.Equals(pairB);
        }

        public static bool operator !=(ValuePair<T> pairA, ValuePair<T> pairB)
        {
            return !pairA.Equals(pairB);
        }

        public bool Equals(ValuePair<T> other)
        {
            return (Equals(ObjectA, other.ObjectA) && Equals(ObjectB, other.ObjectB)) || (Equals(ObjectA, other.ObjectB) && Equals(ObjectB, other.ObjectA));
        }

        public override bool Equals(object obj)
        {
            return obj is ValuePair<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ObjectA != null ? ObjectA.GetHashCode() : 0) * 397) ^ (ObjectB != null ? ObjectB.GetHashCode() : 0);
            }
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Core _core;

        public Web3Data Data { get; set; }
        public MainWindow()
        {

            

            Initialize();
            InitializeComponent();
            checkBoxTG.Checked += delegate { Core.TBot.Enabled = true; };
            checkBoxTG.Unchecked += delegate { Core.TBot.Enabled = false; };
            Data = new Web3Data();
            var test = new BigInteger(1015966).ToString();



        }

        private void Initialize()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            _core = new Core(new DispatcherTimer());
        }

        private void ConsoleControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }

    [Serializable]
    public class Web3Data
    {
        public string Name { get; set; }
        public string RPC { get; set; }

    }
}
