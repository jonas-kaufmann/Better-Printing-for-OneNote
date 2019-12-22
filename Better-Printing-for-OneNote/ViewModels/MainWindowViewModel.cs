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

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {

        #region Properties

        public event EventHandler BringWindowToFrontEvent;

        private const int CROP_HEIGHT = 3500;

        private string LOCAL_FOLDER
        {
            get => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + GeneralHelperClass.FindResource("LocalFolderTitle");
        }

        private string TITLE
        {
            get => GeneralHelperClass.FindResource("Title");
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
                    var pngPath = Conversion.PsToPng(value, LOCAL_FOLDER);
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

        private List<List<int>> _changesList = new List<List<int>>();
        public List<List<int>> ChangesList
        {
            get
            {
                return _changesList;
            }
            set
            {
                _changesList = value;
                OnPropertyChanged("ChangesList");
            }
        }

        private List<int> _cropHeights;
        private List<int> CropHeights
        {
            get
            {
                return _cropHeights;
            }
            set
            {
                _cropHeights = value;
                _changesList.Add(_cropHeights);
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

        #endregion

        public MainWindowViewModel(string argFilePath)
        {
            if (argFilePath != "")
                FilePath = argFilePath;

            FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Zahlen.ps";
            Signature = "Test";

            //eventuell unnötig
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            });

            /*_cropHeights[0] = 2100;
            ReCropImage(_cropHeights);*/

        }

        private void ChangeCropHeights(int pageToEdit, double splitAt)
        {
            var page = Document.Pages[pageToEdit];
            var imageControl = (page.Child.Children[0] as StackPanel).Children[1] as Image;
            var imagePositionY = (imageControl.TransformToAncestor(page.Child).Transform(new Point(0, 0))).Y;
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

        public FixedDocument Test(int pageToEdit, int splitAt)
        {

            //var percentage = 0.5;
            //var pageToEdit = 0;
            ChangeCropHeights(pageToEdit, splitAt);
            return Document;

            /*for (int i = pageToEdit; i < Document.Pages.Count; i++)
            {

            }*/

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