using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Better_Printing_for_OneNote.Views.Controls.VectorGraphics
{
    public partial class VectorGraphicToggleButton : UserControl
    {
        public MonochromeVectorGraphic VectorGraphic
        {
            get => (MonochromeVectorGraphic)GetValue(VectorGraphicProperty);
            set => SetValue(VectorGraphicProperty, value);
        }
        public static DependencyProperty VectorGraphicProperty = DependencyProperty.Register(nameof(VectorGraphic), typeof(MonochromeVectorGraphic), typeof(VectorGraphicToggleButton), new PropertyMetadata(null, VectorGraphicProperty_Changed));
        private static void VectorGraphicProperty_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue && sender is VectorGraphicToggleButton tb)
            {
                tb.UpdateLayout();
            }
        }

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        public static DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(VectorGraphicToggleButton), new PropertyMetadata(false, IsCheckedProperty_Changed));
        private static void IsCheckedProperty_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue && sender is VectorGraphicToggleButton tb)
            {
                tb.UpdateLayout();
            }
        }

        public VectorGraphicToggleButton()
        {
            InitializeComponent();
           MainBorder.DataContext = this;
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
        }

        public new void UpdateLayout()
        {
            var DarkPrimaryResource = TryFindResource("DarkPrimary");
            var LightPrimaryResource = TryFindResource("LightPrimary");

            if (DarkPrimaryResource is Brush darkPrimaryBrush && LightPrimaryResource is Brush lightPrimaryBrush)
            {
                if (IsChecked)
                {
                    MainBorder.Background = lightPrimaryBrush;
                    MainBorder.BorderBrush = lightPrimaryBrush;

                    if (VectorGraphic != null)
                        VectorGraphic.Stroke = darkPrimaryBrush;
                }
                else
                {
                    MainBorder.Background = darkPrimaryBrush;
                    MainBorder.BorderBrush = lightPrimaryBrush;

                    if (VectorGraphic != null)
                        VectorGraphic.Stroke = lightPrimaryBrush;
                }
            }
        }
    }
}
