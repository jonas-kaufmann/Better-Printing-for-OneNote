﻿using System;
using System.Windows;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.IO;
using System.Windows.Shell;
using Better_Printing_for_OneNote.Models;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Better_Printing_for_OneNote.Views.Controls;
using Better_Printing_for_OneNote.Properties;
using System.Diagnostics;

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

                var output = Conversion.ConvertPsToOneImage(value, LOCAL_FOLDER_PATH, TEMP_FOLDER_PATH);
                if (!output.Error)
                {
                    CropHelper = new CropHelper(output.Bitmap);
                    _filePath = value;
                    OnPropertyChanged("FilePath");
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
        public InteractiveFixedDocumentViewer.RedoRequestedHandler RedoRequestHandler { get; set; }
        public InteractiveFixedDocumentViewer.PageDeleteRequestedHandler DeleteRequestHandler { get; set; }

        #endregion

        public MainWindowViewModel(string argFilePath)
        {
            // command handler
            SplitPageRequestHandler = (sender, pageIndex, splitAtPercentage) => CropHelper.SplitPageAt(pageIndex, splitAtPercentage);
            UndoRequestHandler = (sender) => CropHelper.UndoChange();
            RedoRequestHandler = (sender) => CropHelper.RedoChange();
            DeleteRequestHandler = (sender, pageIndex) => CropHelper.SkipPage(pageIndex);


            if (argFilePath != "")
                FilePath = argFilePath;

#if DEBUG
            FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Zahlen.ps";
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