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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VicTool.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleControl.xaml
    /// </summary>
    public partial class ConsoleControl : UserControl
    {
        
        public ConsoleControl()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            Com.SetOutput(richTextBoxOutput);
            Com.OnInput += OnInput;
        }
        private void OnInput(object sender, string e)
        {
            Com.WriteLine(e);

        }


        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Com.SubmitLine(textBoxInput.Text);
                textBoxInput.Text = "";
                e.Handled = true;
            }
            
        }
    }

    public static class Com
    {
        private static RichTextBox _output;
        private static List<string> _lines = new List<string>();
        private static List<string> _copy = new List<string>();
        public static void SetOutput(RichTextBox textBox)
        {
            _output = textBox;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private static bool _busy;
        private static void Timer_Tick(object sender, EventArgs e)
        {

            lock (_lines)
            {
                _copy.Clear();
                _copy.AddRange(_lines);
            }
            foreach (var line in _copy)
            {
                _output.AppendText(line);
            }
            lock (_lines)
            {
                foreach (var line in _copy)
                    _lines.Remove(line);
            }
        }

        public static EventHandler<string> OnInput;
        private static void Worker_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        public static void WriteLine(string data)
        {
            lock (_lines)
            {
                _lines.Add(data + "\r");
            }
        }

        public static void SubmitLine(string data)
        {
            OnInput?.Invoke(null, data);
        }
    }
}
