using Better_Printing_for_OneNote.AdditionalClasses;
using Better_Printing_for_OneNote.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Better_Printing_for_OneNote.Views.Controls
{
    public partial class EditablePresetMenuItem : MenuItem
    {
        public PresetsMenuItem ParentMenuItem;

        public EditablePresetMenuItem()
        {
            InitializeComponent();

        }

        //private int LastChangeMs = 0;
        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            //var readOnly = (bool)Resources["ReadOnly"];
            Resources["ReadOnly"] = !((bool)Resources["ReadOnly"]);
            //if (readOnly && Editing)
            //    Resources["ReadOnly"] = Editing = false;
            //else if(!readOnly && !Editing)
            //    Resources["ReadOnly"] = Editing = true;
            //else if(readOnly && !Editing)
            //    Resources["ReadOnly"] = Editing = true;
            //else 
            //    Resources["ReadOnly"] = Editing = true;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            ParentMenuItem.DeletePreset(this);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ParentMenuItem.SelectionChanged(this);
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            //Resources["ReadOnly"] = true;
        }

        private void NameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Resources["ReadOnly"] = e.Handled = true;
            }

        }

        private void MenuItem_LostFocus(object sender, RoutedEventArgs e)
        {
            //Resources["ReadOnly"] = true;
        }

        private void NameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //Resources["ReadOnly"] = true;
        }
    }
}
