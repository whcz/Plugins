﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace WindowOffset.Views
{
    public partial class EditOffsetDialog : Window
    {
        public EditOffsetDialog()
        {
            InitializeComponent();
        }

        public virtual bool ShowDialog(IntPtr hWndParent)
        {
            if (hWndParent != IntPtr.Zero)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                helper.Owner = hWndParent;
            }

            return this.ShowDialog() == true;
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                bool shiftPressed = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                FocusNextParameter(e, shiftPressed);
            }
            else if (e.Key == Key.Down)
            {
                FocusNextParameter(e, false);
            }
            else if (e.Key == Key.Up)
            {
                FocusNextParameter(e, true);
            }
        }
        private void FocusNextParameter(KeyEventArgs e, bool reversed)
        {
            //int index = grid.SelectedIndex;
            //int newIndex = (reversed) ? index - 1 : index + 1;

            //if (newIndex < 0)
            //{
            //    if (reversed)
            //    {
            //        cmdRun.Focus();
            //        e.Handled = true;
            //    }
            //}
            //else if (newIndex < grid.Items.Count)
            //{
            //    FocusTextBoxInRow(e, newIndex);
            //}
            //else
            //{
            //    // aktivovat tlačítko "Run"
            //    e.Handled = true;
            //    cmdRun.Focus();
            //}
        }

        private void FocusTextBoxInRow(KeyEventArgs e, int newIndex)
        {
            //// vybrat nový řádek
            //DataGridRow newRow = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(newIndex);

            //TextBox txt = FindTextBox(newRow);
            //if (txt != null)
            //{
            //    e.Handled = true;
            //    grid.SelectedIndex = newIndex;
            //    txt.Focus();
            //}
        }

        private TextBox FindTextBox(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject obj = VisualTreeHelper.GetChild(parent, i);
                if (obj is TextBox)
                {
                    return (TextBox)obj;
                }
                TextBox result = FindTextBox(obj);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}