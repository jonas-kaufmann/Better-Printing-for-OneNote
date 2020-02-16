using System.Windows;
using System.Windows.Controls;

namespace Better_Printing_for_OneNote.Views.Controls.VectorGraphics
{
    public class MonochromeVectorGraphic : UserControl
    {
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }
        public static DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(MonochromeVectorGraphic), new PropertyMetadata(1.0));
    }
}