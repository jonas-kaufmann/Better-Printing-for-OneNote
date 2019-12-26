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
                        // load sourceimage once
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(pngPath);
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        CropHelper = new CropHelper(image);

                        OnPropertyChanged("FilePath");
                    }
                }
                else
                {
                    MessageBox.Show($"Die zu öffnende Postscript Datei ({value}) existiert nicht.");
                }
            }
        }


        private CropHelper _cropHelper;
        public CropHelper CropHelper
        {
            get
            {
                return _cropHelper;
            }
            set
            {
                _cropHelper = value;
                OnPropertyChanged("CropHelper");
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
            SplitPageRequestHandler = (sender, x, y) => CropHelper.SplitPageAt(x, y);
            UndoRequestHandler = (sender) => CropHelper.UndoChange();


            if (argFilePath != "")
                FilePath = argFilePath;

#if DEBUG
            FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Ringe_2_Seiten.ps";
#endif

            //might be unnecessary
            /*Task.Run(() =>
            {
                Thread.Sleep(2000);
                BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            });*/
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