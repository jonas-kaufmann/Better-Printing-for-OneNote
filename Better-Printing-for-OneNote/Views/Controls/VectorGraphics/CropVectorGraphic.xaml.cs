
namespace Better_Printing_for_OneNote.Views.Controls.VectorGraphics
{
    public partial class CropVectorGraphic : MonochromeVectorGraphic
    {
        public CropVectorGraphic()
        {
            InitializeComponent();
            MainViewBox.DataContext = this;
        }
    }
}
