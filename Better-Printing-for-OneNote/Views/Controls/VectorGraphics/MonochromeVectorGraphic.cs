using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Better_Printing_for_OneNote.Views.Controls.VectorGraphics
{
    public class MonochromeVectorGraphic : UserControl
    {
        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }
        public static DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(MonochromeVectorGraphic), new PropertyMetadata(Brushes.Black));

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }
        public static DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(MonochromeVectorGraphic), new PropertyMetadata(1.0));
    }
}
