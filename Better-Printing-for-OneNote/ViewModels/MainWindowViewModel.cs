using System;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.Windows.Shell;
using Better_Printing_for_OneNote.Models;
using Microsoft.Win32;
using Better_Printing_for_OneNote.Views.Controls;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Printing;
using System.Diagnostics;
using static Better_Printing_for_OneNote.Views.Controls.InteractiveFixedDocumentViewer;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {
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
                var cts = new CancellationTokenSource();
                var busyDialog = new BusyIndicatorWindow(cts);
                if(Window.IsInitialized)
                    busyDialog.Owner = Window;

                _ = Task.Run(() =>
                {
                    try
                    {
                        var bitmap = Conversion.ConvertPsToOneImage(value, TEMP_FOLDER_PATH, cts.Token);
                        bitmap.Freeze();
                        GeneralHelperClass.ExecuteInUiThread(() =>
                        {
                            var cropHelper = new CropHelper(bitmap);
                            UpdatePrintFormat(cropHelper); // set the format and initialize the first pages
                            CropHelper = cropHelper;
                            busyDialog.Completed = true;
                            busyDialog.Close();
                            _filePath = value;
                            OnPropertyChanged("FilePath");
                        });
                    }
                    catch (ConversionFailedException e)
                    {
                        GeneralHelperClass.ExecuteInUiThread(() =>
                        {
                            busyDialog.Completed = true;
                            busyDialog.Close();
                            MessageBox.Show(e.Message);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        GeneralHelperClass.ExecuteInUiThread(() =>
                        {
                            busyDialog.Completed = true;
                            busyDialog.Close();
                        });
                    }
                });

                busyDialog.ShowDialog();
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

        public PageSplitRequestedHandler SplitPageRequestHandler { get; set; }
        public UndoRequestedHandler UndoRequestHandler { get; set; }
        public RedoRequestedHandler RedoRequestHandler { get; set; }
        public PageDeleteRequestedHandler DeleteRequestHandler { get; set; }

        private Window Window;

        public MainWindowViewModel(string argFilePath, Window window)
        {
            // command handler
            SplitPageRequestHandler = (sender, pageIndex, splitAtPercentage) => CropHelper.SplitPageAt(pageIndex, splitAtPercentage);
            UndoRequestHandler = (sender) => CropHelper.UndoChange();
            RedoRequestHandler = (sender) => CropHelper.RedoChange();
            DeleteRequestHandler = (sender, pageIndex) => CropHelper.SkipPage(pageIndex);

            Window = window;

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

        /// <summary>
        /// Open the print dialog without printing
        /// </summary>
        private void ChoosePrinter()
        {
            PrintDialog.ShowDialog();
            UpdatePrintFormat(CropHelper);
            OnPropertyChanged("PrintDialog");
        }

        /// <summary>
        /// Open the print dialog with printing
        /// </summary>
        private void Print()
        {
            var print = PrintDialog.ShowDialog();
            UpdatePrintFormat(CropHelper);
            OnPropertyChanged("PrintDialog");
            if (print.HasValue && print.Value)
                Task.Run(() => PrintDialog.PrintDocument(CropHelper.Document.DocumentPaginator, Properties.Resources.ApplicationTitle));
        }

        /// <summary>
        /// Updates the format of the Crophelper with the values from the print dialog
        /// </summary>
        /// <param name="cropHelper">the crop helper to edit</param>
        private void UpdatePrintFormat(CropHelper cropHelper)
        {
            var capabilities = PrintDialog.PrintQueue.GetPrintCapabilities(PrintDialog.PrintTicket);
            var pageWidth = capabilities.OrientedPageMediaWidth;
            var pageHeight = capabilities.OrientedPageMediaHeight;
            var contentHeight = pageHeight;
            var contentWidth = pageWidth;
            double paddingX = 0; // padding at the left and right
            double paddingY = 0; // padding at the bottom and top

            if (capabilities != null)
            {
                contentHeight = capabilities.PageImageableArea.ExtentHeight;
                contentWidth = capabilities.PageImageableArea.ExtentWidth;
                paddingX = capabilities.PageImageableArea.OriginWidth;
                paddingY = capabilities.PageImageableArea.OriginHeight;
            }

            cropHelper.UpdateFormat((double)pageHeight, (double)pageWidth, (double)contentHeight, (double)contentWidth, new Thickness(paddingX, paddingY, paddingX, paddingY));
        }

        /// <summary>
        /// Open dialog to search for a ps file
        /// </summary>
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