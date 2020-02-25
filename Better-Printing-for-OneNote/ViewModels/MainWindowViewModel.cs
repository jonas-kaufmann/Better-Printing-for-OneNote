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
using System.Collections.Generic;
using System.Windows.Input;
using Better_Printing_for_OneNote.Views.Windows;
using System.Printing;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Threading;

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

        public ICommand ShowThirdPartyNotices { get; } = new RelayCommand(c => new ThirdPartyNoticesWindow { Notices = ThirdPartyNotices.Notices }.ShowDialog());

        private string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (_filePath != value)
                    ProcessNewFilePath(value);
            }
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

        public PrintDialog PrintDialog { get; } = new PrintDialog();


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
        public OptimalHeightRequestedHandler OptimalHeightRequestedHandler { get; set; }
        public PageMergeRequestedHandler PageMergeRequestedHandler { get; set; }

        #region Printing

        #region Menu Bar
        private readonly List<PrintQueue> printQueuesList = new List<PrintQueue>();

        private ObservableCollection<MenuItem> printQueueMenuItems = new ObservableCollection<MenuItem>();
        public ObservableCollection<MenuItem> PrintQueueMenuItems
        {
            get => printQueueMenuItems;
            set
            {
                if (value != printQueueMenuItems)
                {
                    printQueueMenuItems = value;
                    OnPropertyChanged(nameof(PrintQueueMenuItems));
                }
            }
        }
        public MenuItem SelectedPrintQueueMI { get; private set; }

        private ObservableCollection<MenuItem> paperSizeMenuItems = new ObservableCollection<MenuItem>();
        public ObservableCollection<MenuItem> PaperSizeMenuItems
        {
            get => paperSizeMenuItems;
            set
            {
                if (value != paperSizeMenuItems)
                {
                    paperSizeMenuItems = value;
                    OnPropertyChanged(nameof(PaperSizeMenuItems));
                }
            }
        }
        public MenuItem SelectedPaperSizeMI { get; private set; }

        private void UpdatePrintQueues()
        {

            var queues = new PrintServer().GetPrintQueues();
            printQueuesList.Clear();
            foreach (var queue in queues)
                printQueuesList.Add(queue);
            printQueuesList.OrderBy(q => q.FullName);

            PrintQueueMenuItems.Clear();

            foreach (var queue in printQueuesList)
            {
                MenuItem mi = new MenuItem { Header = queue.FullName, IsCheckable = false, StaysOpenOnClick = true, Command = PrintQueueMI_ClickCommand };
                mi.CommandParameter = mi;

                if (PrintDialog.PrintQueue != null && PrintDialog.PrintQueue.FullName == queue.FullName)
                {
                    mi.IsChecked = true;
                    SelectedPrintQueueMI = mi;
                }
                PrintQueueMenuItems.Add(mi);
            }

            UpdatePaperSizes();
            UpdatePrintFormat(CropHelper);
        }
        private void UpdatePaperSizes()
        {
            PaperSizeMenuItems.Clear();

            if (PrintDialog.PrintQueue == null)
                PaperSizeMenuItems.Add(new MenuItem { IsEnabled = false });
            else
            {
                var paperSizes = PrintDialog.PrintQueue.GetPrintCapabilities().PageMediaSizeCapability;
                foreach (var paperSize in paperSizes)
                {
                    MenuItem mi = new MenuItem { Header = paperSize.PageMediaSizeName, IsCheckable = false, StaysOpenOnClick = true, Command = PaperSizeMI_ClickCommand };
                    mi.CommandParameter = mi;

                    if (PrintDialog.PrintTicket != null && PrintDialog.PrintTicket.PageMediaSize.PageMediaSizeName == paperSize.PageMediaSizeName)
                    {
                        mi.IsChecked = true;
                        SelectedPaperSizeMI = mi;
                    }

                    PaperSizeMenuItems.Add(mi);
                }
            }
        }

        private ICommand printQueueMI_ClickCommand;
        public ICommand PrintQueueMI_ClickCommand { get => printQueueMI_ClickCommand ??= new RelayCommand((sender) => PrintQueueMI_Click(sender)); }
        private void PrintQueueMI_Click(object sender)
        {
            if (sender is MenuItem mi && mi != SelectedPrintQueueMI)
            {
                SelectedPrintQueueMI.IsChecked = false;
                SelectedPrintQueueMI = mi;
                SelectedPrintQueueMI.IsChecked = true;
                PrintDialog.PrintQueue = printQueuesList.Where(pq => pq.FullName == (string)SelectedPrintQueueMI.Header).First();
            }

            if (PrintDialog.PrintQueue != null)
            {
                UpdatePaperSizes();
                UpdatePrintFormat(CropHelper);
            }
        }


        private ICommand paperSizeMI_ClickCommand;
        public ICommand PaperSizeMI_ClickCommand { get => paperSizeMI_ClickCommand ??= new RelayCommand((sender) => PaperSizeMI_Click(sender)); }
        private void PaperSizeMI_Click(object sender)
        {
            if (sender is MenuItem mi && mi.Header is PageMediaSizeName newPageMediaSizeName && PrintDialog.PrintTicket.PageMediaSize.PageMediaSizeName != newPageMediaSizeName)
            {
                SelectedPaperSizeMI.IsChecked = false;
                SelectedPaperSizeMI = mi;
                SelectedPaperSizeMI.IsChecked = true;

                PrintDialog.PrintTicket.PageMediaSize = PrintDialog.PrintQueue.GetPrintCapabilities().PageMediaSizeCapability.Where(pmsc => pmsc.PageMediaSizeName == newPageMediaSizeName).First();
                UpdatePrintFormat(CropHelper);
            }
        }
        #endregion

        public PrintRequestedHandler PrintRequestedHandler { get; set; }
        public PrintDialogValuesChangedHandler PrintDialogValuesChangedHandler { get; set; }

        #endregion

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
            OptimalHeightRequestedHandler = (sender, pageIndex) => CropHelper.GetOptimalHeight(pageIndex);
            PageMergeRequestedHandler = (sender, fromPage, toPage) => CropHelper.MergePages(fromPage, toPage);

            // printing
            PrintRequestedHandler = (sender) => Print();
            PrintDialogValuesChangedHandler = (sender) => UpdatePrintQueues();
            UpdatePrintQueues();

            Window = window;

            if (argFilePath != "")
                FilePath = argFilePath;

#if DEBUG
            //FilePath = @"C:\Users\jokau\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Normales Dokument\Diskrete Signale.pdf";
            //FilePath = @"C:\Users\fabit\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Normales Dokument\Diskrete Signale.pdf";
            FilePath = @"D:\Daten\OneDrive\Freigabe Fabian-Jonas\BetterPrinting\Normales Dokument\Diskrete Signale.pdf";
#endif

        }

        private void ChoosePrinter()
        {
            PrintDialog.ShowDialog();
            UpdatePrintFormat(CropHelper);
            OnPropertyChanged("PrintDialog");
        }

        private void Print()
        {
            UpdatePrintFormat(CropHelper);


            Window.IsEnabled = false;
            var prevResizeMode = Window.ResizeMode;
            Window.ResizeMode = ResizeMode.NoResize;

            var left = Window.Left;
            var top = Window.Top;
            var width = Window.Width;
            var height = Window.Height;
            var dialogThread = new Thread(new ThreadStart(() =>
            {
                var busyWindow = new BusyWindow();
                busyWindow.Show();
                busyWindow.Left = left + width / 6 - busyWindow.Width / 2;
                busyWindow.Top = top + height / 6 - busyWindow.Height / 2;

                Dispatcher.Run();
            }));
            dialogThread.SetApartmentState(ApartmentState.STA);
            dialogThread.IsBackground = true;
            dialogThread.Start();
            

            PrintDialog.PrintDocument(CropHelper.Document.DocumentPaginator, Path.GetFileName(FilePath));

            
            Dispatcher.FromThread(dialogThread).InvokeShutdown();
            Window.IsEnabled = true;
            Window.ResizeMode = prevResizeMode; 
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

            if (cropHelper != null)
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