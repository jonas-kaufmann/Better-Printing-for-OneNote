using Better_Printing_for_OneNote.AdditionalClasses;
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
    public partial class EditableMenuItem : MenuItem
    {

        #region properties

        private MenuItem AddItem_MenuItem;
        private MenuItem Selected_MenuItem;
        public ObservableCollection<MenuItem> MenuItems { get; set; }
        private DataTemplate MenuItem_Template;

        #region ItemCollection
        public static readonly DependencyProperty ItemCollection_Property = DependencyProperty.Register(nameof(ItemCollection), typeof(ObservableCollection<object>), typeof(EditableMenuItem), new PropertyMetadata(ItemCollection_Changed));
        public ObservableCollection<object> ItemCollection
        {
            get => (ObservableCollection<object>)GetValue(ItemCollection_Property);
            set => SetValue(ItemCollection_Property, value);
        }

        private static void ItemCollection_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is EditableMenuItem emi)
                foreach (var item in e.NewValue as ObservableCollection<object>)
                    emi.MenuItems.Insert(emi.MenuItems.Count - 1, emi.CreateNewMenuItem(item));
        }
        #endregion

        #region HeaderTextBinding
        public static readonly DependencyProperty HeaderTextBinding_Property = DependencyProperty.Register(nameof(HeaderTextBinding), typeof(string), typeof(EditableMenuItem), new PropertyMetadata(HeaderTextBinding_Changed));
        public string HeaderTextBinding
        {
            get => (string)GetValue(HeaderTextBinding_Property);
            set => SetValue(HeaderTextBinding_Property, value);
        }

        private static void HeaderTextBinding_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is EditableMenuItem emi)
            {
                emi.MenuItem_Template = (DataTemplate)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(
                    @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <TextBox FontSize=""12"" Text=""{" + e.NewValue + @"}"" MinWidth=""170"" MaxWidth=""170"" Margin=""0 2 -45 2"">
                            <TextBox.Background>
                                <SolidColorBrush Opacity=""0""/>
                            </TextBox.Background>
                            <TextBox.BorderBrush>
                                <SolidColorBrush Opacity=""0""/>
                            </TextBox.BorderBrush>
                        </TextBox>
                    </DataTemplate>")));

                //emi.Editable_Template = (DataTemplate)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(
                //    @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
                //            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                //        <TextBox Text=""{" + e.NewValue + @"}"" MinWidth=""100"" MaxWidth=""350"">
                //            <TextBox.Background>
                //                <SolidColorBrush Opacity=""0""/>
                //            </TextBox.Background>
                //        </TextBox>
                //    </DataTemplate>")));
            }
        }
        #endregion

        #region AddItemHeader
        public static readonly DependencyProperty AddItemHeader_Property = DependencyProperty.Register(nameof(AddItemHeader), typeof(string), typeof(EditableMenuItem), new PropertyMetadata(AddItemHeader_Changed));
        public string AddItemHeader
        {
            get => (string)GetValue(AddItemHeader_Property);
            set => SetValue(AddItemHeader_Property, value);
        }

        private static void AddItemHeader_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is EditableMenuItem emi && e.OldValue != e.NewValue && emi.AddItem_MenuItem != null)
                emi.AddItem_MenuItem.Header = e.NewValue;
        }
        #endregion

        #region request new item command
        public NewItemRequested_Handler NewItemRequested_Command
        {
            get => (NewItemRequested_Handler)GetValue(NewItemRequested_Command_Property);
            set => SetValue(NewItemRequested_Command_Property, value);
        }

        public delegate object NewItemRequested_Handler(object sender);
        public static readonly DependencyProperty NewItemRequested_Command_Property = DependencyProperty.Register(nameof(NewItemRequested_Command), typeof(NewItemRequested_Handler), typeof(EditableMenuItem));
        #endregion

        #region item checked command
        public ItemChecked_Handler ItemChecked_Command
        {
            get => (ItemChecked_Handler)GetValue(ItemChecked_Command_Property);
            set => SetValue(ItemChecked_Command_Property, value);
        }

        public delegate void ItemChecked_Handler(object sender, object item);
        public static readonly DependencyProperty ItemChecked_Command_Property = DependencyProperty.Register(nameof(ItemChecked_Command), typeof(ItemChecked_Handler), typeof(EditableMenuItem));
        #endregion

        #endregion

        public EditableMenuItem()
        {
            var template = (DataTemplate)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(
                @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <TextBlock Text=""{Binding .}"" Margin=""3 0 0 0"">
                            <TextBlock.Background>
                                <SolidColorBrush Opacity=""0""/>
                            </TextBlock.Background>
                        </TextBlock>
                    </DataTemplate>")));

            MenuItems = new ObservableCollection<MenuItem>();
            AddItem_MenuItem = new MenuItem() { Header = AddItemHeader, StaysOpenOnClick = true, IsCheckable = false, CommandParameter = this, HeaderTemplate = template };
            AddItem_MenuItem.Click += AddItem_Click;
            MenuItems.Add(AddItem_MenuItem);

            DataContext = this;

            InitializeComponent();

        }

        private MenuItem CreateNewMenuItem(object item)
        {
            var mi = new MenuItem { Header = item, IsCheckable = false, StaysOpenOnClick = true, CommandParameter = this, HeaderTemplate = MenuItem_Template };
            mi.Click += MenuItem_Click;
            mi.KeyUp += MenuItem_KeyUp;
            //mi.MouseDoubleClick += MenuItem_DoubleClick;
            //mi.MouseLeave += MenuItem_MouseLeave;
            //mi.MouseEnter += MenuItem_MouseEnter;
            //mi.IsKeyboardFocusedChanged += MenuItem_IsKeyboardFocusedChanged;

            return mi;
        }

        private void MenuItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is MenuItem mi && e.Key == Key.Delete)
            {
                MenuItems[MenuItems.Count - 1].Focus();
                MenuItems.Remove(mi);
                ItemCollection.Remove(mi.Header);
            }
        }

        private void MenuItem_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if (sender is MenuItem mi && e.NewValue is bool b)
            //{
            //    //if (!b)
            //    //    mi.HeaderTemplate = NotEditable_Template;
            //    //else
            //    //    mi.HeaderTemplate = Editable_Template;
            //    Trace.WriteLine(b);
            //}
        }

        private void MenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            //if (sender is MenuItem mi)
            //    mi.HeaderTemplate = NotEditable_Template;
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            //if (sender is MenuItem mi)
            //    mi.HeaderTemplate = Editable_Template;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.CommandParameter is EditableMenuItem emi)
            {
                if (Selected_MenuItem != null)
                    Selected_MenuItem.IsChecked = false;
                Selected_MenuItem = mi;
                Selected_MenuItem.IsChecked = true;

                ItemChecked_Command?.Invoke(this, mi.Header);
            }
        }

        //private void MenuItem_DoubleClick(object sender, RoutedEventArgs e)
        //{
        //    if (sender is MenuItem mi && mi.CommandParameter is EditableMenuItem emi)
        //        mi.HeaderTemplate = Editable_Template;
        //}

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem _mi && _mi.CommandParameter is EditableMenuItem emi)
            {
                var newItem = emi.NewItemRequested_Command?.Invoke(emi);
                emi.ItemCollection.Add(newItem);
                var mi = CreateNewMenuItem(newItem);
                emi.MenuItems.Insert(emi.MenuItems.Count - 1, mi);

                mi.Focus();
            }
        }
    }
}
