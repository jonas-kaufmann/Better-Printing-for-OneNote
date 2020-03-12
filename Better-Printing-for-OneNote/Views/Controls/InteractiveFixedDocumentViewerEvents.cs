
using System.ComponentModel;
using System.Windows.Input;

namespace Better_Printing_for_OneNote.Views.Controls
{
    public partial class InteractiveFixedDocumentViewer : INotifyPropertyChanged
    {
        #region event handler delegates
        public delegate void SelectedToolChangedHandler(object sender, SelectedToolChangedEventArgs e);
        #endregion


        #region events
        public event SelectedToolChangedHandler SelectedToolChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }

        #region event handlers
        private void InteractiveFixedDocumentViewer_SelectedToolChanged(object sender, SelectedToolChangedEventArgs e)
        {
            if (e.NewTool == InteractiveFixedDocumentViewerTools.Crop)
                PagesGrid.Cursor = Cursors.Arrow;
            else
                PagesGrid.Cursor = Cursors.IBeam;
        }
        #endregion
    }


    public class SelectedToolChangedEventArgs
    {
        public readonly InteractiveFixedDocumentViewerTools OldTool;
        public readonly InteractiveFixedDocumentViewerTools NewTool;

        public SelectedToolChangedEventArgs(InteractiveFixedDocumentViewerTools oldTool, InteractiveFixedDocumentViewerTools newTool)
        {
            OldTool = oldTool;
            NewTool = newTool;
        }
    }

 
    public enum InteractiveFixedDocumentViewerTools
    {
        Crop,
        Signature,
        PageNumbers
    }
}
