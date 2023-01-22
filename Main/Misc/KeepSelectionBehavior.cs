using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace VicTool.Main.Misc
{
    public class KeepSelectionBehavior : Behavior<TextBox>
    {
        private bool _wasAllTextSelected = false;
        private int inputKeysDown = 0;

        protected override void OnAttached()
        {
            base.OnAttached();

            CheckSelection();
            AssociatedObject.TextChanged += TextBox_TextChanged;
            AssociatedObject.SelectionChanged += TextBox_SelectionChanged;
            AssociatedObject.PreviewKeyDown += TextBox_PreviewKeyDown;
            AssociatedObject.KeyUp += TextBox_KeyUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.TextChanged -= TextBox_TextChanged;
            AssociatedObject.SelectionChanged -= TextBox_SelectionChanged;
            AssociatedObject.PreviewKeyDown -= TextBox_PreviewKeyDown;
            AssociatedObject.KeyUp -= TextBox_KeyUp;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_wasAllTextSelected && inputKeysDown == 0)
            {
                AssociatedObject.SelectAll();
            }
            CheckSelection();
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            CheckSelection();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsInputKey(e.Key))
            {
                inputKeysDown++;
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsInputKey(e.Key))
            {
                inputKeysDown--;
            }
        }

        private bool IsInputKey(Key key)
        {
            return
                key == Key.Space ||
                key == Key.Delete ||
                key == Key.Back ||
                (key >= Key.D0 && key <= Key.Z) ||
                (key >= Key.Multiply && key <= Key.Divide) ||
                (key >= Key.Oem1 && key <= Key.OemBackslash);
        }

        private void CheckSelection()
        {
            _wasAllTextSelected = AssociatedObject.SelectionLength == AssociatedObject.Text.Length;
        }
    }
}
