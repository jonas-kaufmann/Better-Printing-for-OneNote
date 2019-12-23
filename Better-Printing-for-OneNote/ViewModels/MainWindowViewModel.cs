using System;
using System.Windows;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Shell;
using Better_Printing_for_OneNote.Models;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Better_Printing_for_OneNote.Views.Controls;
using Better_Printing_for_OneNote.Properties;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {

        #region Properties

        public event EventHandler BringWindowToFrontEvent;

        private const int CROP_HEIGHT = 3500;

        public string TEMP_FOLDER_PATH
        {
            get => GeneralHelperClass.FindResource("TempFolderPath");
        }
        public string LOCAL_FOLDER_PATH
        {
            get => GeneralHelperClass.FindResource("LocalFolderPath");
        }

        private RelayCommand _searchFileCommand;
        public RelayCommand SearchFileCommand
        {
            get
            {
                return _searchFileCommand ?? (_searchFileCommand = new RelayCommand(c => SearchFile()));
            }
        }

        /*private RelayCommand _pageSplitRequestCommand;
        public RelayCommand PageSplitRequestCommand
        {
            get
            {
                return _pageSplitRequestCommand ?? (_pageSplitRequestCommand = new RelayCommand(c => Test(c)));
            }
        }*/

        private string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (File.Exists(value))
                {
                    var pngPath = Conversion.PsToPng(value, LOCAL_FOLDER_PATH, TEMP_FOLDER_PATH);
                    if (pngPath != "")
                    {
                        var output = Conversion.PngToFixedDoc(pngPath, Signature, CROP_HEIGHT);
                        if (output != null)
                        {
                            Document = output.Document;
                            CropHeights = output.CropHeights;
                            _filePath = value;
                            _pngPath = pngPath;
                            OnPropertyChanged("FilePath");
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Die zu öffnende Postscript Datei ({value}) existiert nicht.");
                }
            }
        }

        private string _pngPath;

        private List<int> _lastChange;

        private List<int> _cropHeights;
        private List<int> CropHeights
        {
            get
            {
                return _cropHeights;
            }
            set
            {
                _lastChange = _cropHeights;
                _cropHeights = value;
            }
        }

        private string _signature;
        public string Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
                Conversion.ChangeSignature(Document, value);
            }
        }

        private FixedDocument _document;
        public FixedDocument Document
        {
            get
            {
                return _document;
            }
            set
            {
                _document = value;
                OnPropertyChanged("Document");
            }
        }

        private TaskbarItemProgressState _windowProgressState = TaskbarItemProgressState.None;
        public TaskbarItemProgressState WindowProgressState
        {
            get
            {
                return _windowProgressState;
            }
            set
            {
                _windowProgressState = value;
                OnPropertyChanged("WindowProgressState");
            }
        }

        public InteractiveFixedDocumentViewer.PageSplitRequestedHandler SplitPageRequestHandler { get; set; }
        public InteractiveFixedDocumentViewer.UndoRequestedHandler UndoRequestHandler { get; set; }

        #endregion

        public MainWindowViewModel(string argFilePath)
        {
            // command handler
            SplitPageRequestHandler = ChangeCropHeights;
            UndoRequestHandler = UndoChange;

            if (argFilePath != "")
                FilePath = argFilePath;

# if DEBUG
            FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Ringe_2_Seiten.ps";
#endif

            //might be unnecessary
            /*Task.Run(() =>
            {
                Thread.Sleep(2000);
                BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            });*/
        }

        private void UndoChange(object sender)
        {
            if (_lastChange != null && CropHeights != null && !GeneralHelperClass.CompareList<int>(CropHeights, _lastChange))
            {
                _cropHeights = null;
                ReCropDocument(_lastChange);
            }
        }

        private void ChangeCropHeights(object sender, int pageToEdit, double splitAtPercentage)
        {
            var page = Document.Pages[pageToEdit];
            var imageControl = (page.Child.Children[0] as StackPanel).Children[1] as Image;
            var imagePositionY = (imageControl.TransformToAncestor(page.Child).Transform(new Point(0, 0))).Y;
            var splitAt = splitAtPercentage * page.Child.ActualHeight;
            var yPosInImage = splitAt - imagePositionY;
            var cropHeight = (int)Math.Round((yPosInImage / imageControl.ActualHeight) * (imageControl.Source as BitmapImage).PixelHeight);

            // addopt all Siteheights before the page to edit, reset all pages after the eidted page
            var imageHeight = 0;
            foreach (var height in CropHeights)
                imageHeight += height;
            int i = 0;
            var newCropHeights = new List<int>();
            while (imageHeight > 0)
            {
                if (i < pageToEdit)
                {
                    newCropHeights.Add(CropHeights[i]);
                    imageHeight -= CropHeights[i];
                }
                else
                if (i > pageToEdit)
                {
                    if (imageHeight < CROP_HEIGHT)
                    {
                        newCropHeights.Add(imageHeight);
                        imageHeight = 0;
                    }
                    else
                    {
                        newCropHeights.Add(CROP_HEIGHT);
                        imageHeight -= CROP_HEIGHT;
                    }
                }
                else
                {
                    newCropHeights.Add(cropHeight);
                    imageHeight -= cropHeight;
                }
                i++;
            }

            ReCropDocument(newCropHeights);
        }

        private void ReCropDocument(List<int> cropHeights)
        {
            var output = Conversion.PngToFixedDoc(_pngPath, Signature, cropHeights);
            if (output != null)
            {
                Document = output.Document;
                CropHeights = output.CropHeights;
            }
        }

        private void SearchFile()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "PostScript Dateien (*.ps)|*.ps";
            if (fileDialog.ShowDialog() == true)
            {
                FilePath = fileDialog.FileName;
            }
        }
    }
}