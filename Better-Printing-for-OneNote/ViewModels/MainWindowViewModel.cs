using System;
using System.Windows;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Xps.Packaging;
using System.IO;
using Ghostscript.NET.Processor;
using System.Collections.Generic;
using Ghostscript.NET;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Shell;
using Better_Printing_for_OneNote.Models;
using Microsoft.Win32;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {

        #region Properties

        public event EventHandler BringWindowToFrontEvent;

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
                    if(pngPath != "")
                    {
                        var doc = Conversion.PngToFixedDoc(pngPath, Signature);
                        if (doc != null)
                        {
                            Document = doc;
                            _filePath = value;
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

# if DEBUG
            FilePath = @"C:\Users\jokau\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Zahlen.ps";
#endif

            //might be unnecessary
            //Task.Run(() =>
            //{
            //    Thread.Sleep(2000);
            //    BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            //});
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