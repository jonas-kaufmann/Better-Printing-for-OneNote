namespace Better_Printing_for_OneNote.Views.Controls.VectorGraphics
{
    public partial class EditVectorGraphic : MonochromeVectorGraphic
    {
        public EditVectorGraphic()
        {
            InitializeComponent();
            MainViewBox.DataContext = this;
        }
    }
}
