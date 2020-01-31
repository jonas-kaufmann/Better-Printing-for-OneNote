using System;
using Better_Printing_for_OneNote.AdditionalClasses;
using Better_Printing_for_OneNote.Models;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using static Better_Printing_for_OneNote.Views.Controls.InteractiveFixedDocumentViewer;
using Better_Printing_for_OneNote.Properties;
using System.IO;
using WpfCropableImageControl;
using System.Collections.Generic;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {
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

        private RelayCommand _choosePrinterCommand;
        public RelayCommand ChoosePrinterCommand
        {
            get
            {
                return _choosePrinterCommand ?? (_choosePrinterCommand = new RelayCommand(c => ChoosePrinter()));
            }
        }

        private RelayCommand _printCommand;
        public RelayCommand PrintCommand
        {
            get
            {
                return _printCommand ?? (_printCommand = new RelayCommand(c => Print()));
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
                ProcessNewFilePath(value);
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

        private PrintDialog _printDialog;
        public PrintDialog PrintDialog
        {
            get
            {
                return _printDialog ?? (_printDialog = new PrintDialog());
            }
        }

        #region window title
        private string _windowTitle = Resources.ApplicationTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (value != _windowTitle)
                {
                    _windowTitle = value;
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }
        #endregion

        public PageSplitRequestedHandler SplitPageRequestHandler { get; set; }
        public UndoRequestedHandler UndoRequestHandler { get; set; }
        public RedoRequestedHandler RedoRequestHandler { get; set; }
        public PageDeleteRequestedHandler DeleteRequestHandler { get; set; }
        public AddSignatureRequestedHandler AddControlToDocRequestHandler { get; set; }
        public AreaDeleteRequestedHandler AreaDeleteRequestedHandler { get; set; }

        private Window Window;

        public MainWindowViewModel(string argFilePath, Window window)
        {
            // command handler
            SplitPageRequestHandler = (sender, pageIndex, splitAtPercentage) => CropHelper.SplitPageAt(pageIndex, splitAtPercentage);
            UndoRequestHandler = (sender) => CropHelper.UndoChange();
            RedoRequestHandler = (sender) => CropHelper.RedoChange();
            DeleteRequestHandler = (sender, pageIndex) => CropHelper.SkipPage(pageIndex);
            AddControlToDocRequestHandler = (sender, x, y) => CropHelper.AddSignatureTb(x, y);
            AreaDeleteRequestedHandler = (sender, x, y, z) => CropHelper.DeleteArea(x, y, z);

            Window = window;

            if (argFilePath != "")
                FilePath = argFilePath;

#if DEBUG
            //FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Normales Dokument\Diskrete Signale.pdf";
            FilePath = @"C:\Users\fabit\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Normales Dokument\Diskrete Signale.pdf";
            //Print();
#endif

        }

        private void ProcessNewFilePath(string filePath)
        {
            var cts = new CancellationTokenSource();
            var reporter = new ProgressReporter();
            var busyDialog = new BusyIndicatorWindow(cts, reporter);
            if (Window.IsInitialized)
                busyDialog.Owner = Window;

            _ = Task.Run(() =>
            {
                try
                {
                    BitmapSource[] bitmaps = Conversion.ConvertPDFToBitmaps(filePath, cts.Token, reporter);
                    foreach (var bitmap in bitmaps)
                        bitmap.Freeze();
                    GeneralHelperClass.ExecuteInUiThread(() =>
                    {
                        var cropHelper = new CropHelper(bitmaps);
                        UpdatePrintFormat(cropHelper); // set the format and initialize the first pages
                        CropHelper = cropHelper;
                        busyDialog.Completed = true;
                        busyDialog.Close();
                        _filePath = filePath;
                        OnPropertyChanged("FilePath");

                        WindowTitle = $"{Resources.ApplicationTitle} - {Path.GetFileName(_filePath)}";
                    });
                }
                catch (ConversionFailedException e)
                {
                    GeneralHelperClass.ExecuteInUiThread(() => busyDialog.SetError(e.Message));
                }
                catch (OperationCanceledException)
                {
                    GeneralHelperClass.ExecuteInUiThread(() =>
                    {
                        busyDialog.Completed = true;
                        busyDialog.Close();
                    });
                }

                GC.Collect();
            });

            busyDialog.ShowDialog();
        }


        private void ChoosePrinter()
        {
            PrintDialog.ShowDialog();
            UpdatePrintFormat(CropHelper);
            OnPropertyChanged("PrintDialog");
        }

        private void Print()
        {
            var print = PrintDialog.ShowDialog();
            UpdatePrintFormat(CropHelper);
            OnPropertyChanged("PrintDialog");
            if (print.HasValue && print.Value)
                PrintDialog.PrintDocument(CropHelper.Document.DocumentPaginator, Path.GetFileName(FilePath));
        }

        private const double CmToPx = 96d / 2.54;
        /// <summary>
        /// Updates the format of the Crophelper with the values from the print dialog
        /// </summary>
        private void UpdatePrintFormat(CropHelper cropHelper)
        {
            double pageWidth;
            double pageHeight;
            double contentHeight;
            double contentWidth;
            double paddingX = 0; // padding at the left and right
            double paddingY = 0; // padding at the bottom and top

            if (PrintDialog == null || PrintDialog.PrintTicket == null || PrintDialog.PrintQueue == null)
            {
                pageWidth = 21 * CmToPx;
                pageHeight = 29.7 * CmToPx;
                contentWidth = pageWidth;
                contentHeight = pageHeight;
            }
            else
            {
                var capabilities = PrintDialog.PrintQueue.GetPrintCapabilities(PrintDialog.PrintTicket);
                pageWidth = capabilities.OrientedPageMediaWidth.Value;
                pageHeight = capabilities.OrientedPageMediaHeight.Value;
                contentHeight = capabilities.PageImageableArea.ExtentHeight;
                contentWidth = capabilities.PageImageableArea.ExtentWidth;
                paddingX = capabilities.PageImageableArea.OriginWidth;
                paddingY = capabilities.PageImageableArea.OriginHeight;
            }

            cropHelper.UpdateFormat(pageHeight, pageWidth, contentHeight, contentWidth, new Thickness(paddingX, paddingY, paddingX, paddingY));
        }

        private void SearchFile()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "PDF-Files|*.pdf";
            if (fileDialog.ShowDialog() == true)
                FilePath = fileDialog.FileName;
        }
    }
}