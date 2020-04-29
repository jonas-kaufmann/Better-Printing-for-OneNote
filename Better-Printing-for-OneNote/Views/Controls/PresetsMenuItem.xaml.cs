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
    public partial class PresetsMenuItem : MenuItem
    {
        #region properties

        private ObservableCollection<MenuItem> MenuItems = new ObservableCollection<MenuItem>();

        #region Presets
        public static readonly DependencyProperty Presets_Property = DependencyProperty.Register(nameof(Presets), typeof(ObservableCollection<Preset>), typeof(PresetsMenuItem), new PropertyMetadata(ItemCollection_Changed));
        public ObservableCollection<Preset> Presets
        {
            get => (ObservableCollection<Preset>)GetValue(Presets_Property);
            set => SetValue(Presets_Property, value);
        }

        private static void ItemCollection_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PresetsMenuItem emi)
                foreach (var item in e.NewValue as ObservableCollection<Preset>)
                    emi.MenuItems.Add(emi.CreateNewMenuItem(item));
        }
        #endregion

        #region preset change requested command
        public PresetChangeRequestedHandler PresetChangeRequestedCommand
        {
            get => (PresetChangeRequestedHandler)GetValue(PresetChangeRequestedProperty);
            set => SetValue(PresetChangeRequestedProperty, value);
        }

        public delegate void PresetChangeRequestedHandler(object sender, object preset);
        public static readonly DependencyProperty PresetChangeRequestedProperty = DependencyProperty.Register(nameof(PresetChangeRequestedCommand), typeof(PresetChangeRequestedHandler), typeof(PresetsMenuItem));
        #endregion

        #region new preset requested command
        public NewPresetRequestedHandler NewPresetRequestedCommand
        {
            get => (NewPresetRequestedHandler)GetValue(NewPresetRequestedProperty);
            set => SetValue(NewPresetRequestedProperty, value);
        }

        public delegate Preset NewPresetRequestedHandler(object sender);
        public static readonly DependencyProperty NewPresetRequestedProperty = DependencyProperty.Register(nameof(NewPresetRequestedCommand), typeof(NewPresetRequestedHandler), typeof(PresetsMenuItem));
        #endregion

        #region clear signatures requested command
        public ClearSignaturesRequestedHandler ClearSignaturesRequestedCommand
        {
            get => (ClearSignaturesRequestedHandler)GetValue(ClearSignaturesRequestedProperty);
            set => SetValue(ClearSignaturesRequestedProperty, value);
        }

        public delegate void ClearSignaturesRequestedHandler(object sender);
        public static readonly DependencyProperty ClearSignaturesRequestedProperty = DependencyProperty.Register(nameof(ClearSignaturesRequestedCommand), typeof(ClearSignaturesRequestedHandler), typeof(PresetsMenuItem));
        #endregion

        #endregion

        public PresetsMenuItem()
        {
            InitializeComponent();

            MenuItemsContainer.Collection = MenuItems;
        }

        private MenuItem CreateNewMenuItem(Preset preset)
        {
            return new EditablePresetMenuItem() { DataContext = preset, ParentMenuItem = this, IsCheckable = false };
        }

        public void SelectionChanged(MenuItem mi)
        {
            PresetChangeRequestedCommand?.Invoke(this, (Preset)mi.DataContext);
        }

        public void DeletePreset(MenuItem mi)
        {
            MenuItems[MenuItems.Count - 1].Focus();
            MenuItems.Remove(mi);
            Presets.Remove((Preset)mi.DataContext);
        }

        private void ClearSignatures_Click(object sender, RoutedEventArgs e)
        {
            ClearSignaturesRequestedCommand?.Invoke(this);
        }

        private void AddPreset_Click(object sender, RoutedEventArgs e)
        {
            var newItem = NewPresetRequestedCommand?.Invoke(this);
            Presets.Add(newItem);
            var mi = CreateNewMenuItem(newItem);
            MenuItems.Add(mi);

            mi.Focus();
        }
    }
}
