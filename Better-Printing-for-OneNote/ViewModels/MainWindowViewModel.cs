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

        private RelayCommand _testCommand;
        public RelayCommand TestCommand
        {
            get
            {
                return _testCommand ?? (_testCommand = new RelayCommand(c => Test()));
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
                // split document
                if (File.Exists(value))
                {
                    _filePath = value;
                    OnPropertyChanged("FilePath");
                    Document = PngToFixedDoc(PsToPng(value), Signature);
                }
                else
                {
                    MessageBox.Show("Die zu öffnende Postscript Datei existiert nicht oder ist nicht valide. Bitte mit valider Datei neu versuchen.");
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
                ChangeSignature(Document, value);
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

            //eventuell unnötig
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            });
        }

        private void Test()
        {
        }

        /// <summary>
        /// Changes the signature of all Sites without recreating the document
        /// </summary>
        /// <param name="document">modified Fixeddocument</param>
        /// <param name="signature">signature to set</param>
        private void ChangeSignature(FixedDocument document, string signature)
        {
            // Signatur anpassen
            foreach (var page in Document.Pages)
                ((page.Child.Children[0] as StackPanel).Children[0] as TextBlock).Text = signature;
        }

        /// <summary>
        /// Converts png to a FixedDocument
        /// </summary>
        /// <param name="imagePath">path to the image to convert</param>
        /// <param name="documentWidth">width of the output document; default: DINA4</param>
        /// <param name="documentHeight">height of the output document; default: DINA4</param>
        /// <param name="signature">signature on every site of the document</param>
        /// <param name="cropHeight">height to split</param>
        /// <param name="srcBitmap">just ignore (its for the helper Method with "int cropHeight")</param>
        /// <returns>the document</returns>
        private FixedDocument PngToFixedDoc(string imagePath, string signature, int[] cropHeights, double documentWidth = 793.7007874, double documentHeight = 1122.519685, Bitmap src = null)
        {
            FixedDocument document = new FixedDocument();
            Bitmap srcBitmap = src ?? System.Drawing.Image.FromFile(imagePath) as Bitmap;
            var cropPostionY = 0;
            foreach (var cropHeight in cropHeights)
            {
                // Crop Image
                Rectangle cropRect;
                cropRect = new Rectangle(0, cropPostionY, srcBitmap.Width, cropHeight);
                cropPostionY += cropHeight;
                var targetBitmap = new Bitmap(cropRect.Width, cropRect.Height);
                using (Graphics g = Graphics.FromImage(targetBitmap))
                {
                    g.DrawImage(srcBitmap, new Rectangle(0, 0, targetBitmap.Width, targetBitmap.Height), cropRect, GraphicsUnit.Document);
                }

                // Convert Bitmap to BitmapImage
                BitmapImage croppedImage = new BitmapImage();
                MemoryStream ms = new MemoryStream();
                targetBitmap.Save(ms, srcBitmap.RawFormat);
                croppedImage.BeginInit();
                ms.Seek(0, SeekOrigin.Begin);
                croppedImage.StreamSource = ms;
                croppedImage.EndInit();

                // Create page and add it to the FixedDocument
                var page = new PageContent();
                var fixedPage = new FixedPage();
                fixedPage.Width = documentWidth;
                fixedPage.Height = documentHeight;
                page.Child = fixedPage;
                var stackpanel = new StackPanel();
                var imageControl = new System.Windows.Controls.Image() { Source = croppedImage };
                imageControl.Width = documentWidth;
                var signatureTB = new TextBlock() { Text = signature };
                stackpanel.Children.Add(signatureTB);
                stackpanel.Children.Add(imageControl);
                page.Child.Children.Add(stackpanel);
                document.Pages.Add(page);
            }

            return document;
        }

        /// <summary>
        /// Converts png to a FixedDocument
        /// </summary>
        /// <param name="imagePath">path to the image to convert</param>
        /// <param name="documentWidth">width of the output document; default: DINA4</param>
        /// <param name="documentHeight">height of the output document; default: DINA4</param>
        /// <param name="signature">signature on every site of the document</param>
        /// <param name="cropHeight">height to split</param>
        /// <returns>the document</returns>
        private FixedDocument PngToFixedDoc(string imagePath, string signature, int cropHeight = 3500, double documentWidth = 793.7007874, double documentHeight = 1122.519685)
        {
            var srcBitmap = System.Drawing.Image.FromFile(imagePath) as Bitmap;
            var splits = (int)Math.Ceiling(decimal.Divide(srcBitmap.Height, cropHeight));
            var cropHeights = new int[splits];
            for (int i = 0; i < splits - 1; i++)
                cropHeights[i] = cropHeight;
            cropHeights[splits - 1] = srcBitmap.Height - (splits - 1) * cropHeight;
            return PngToFixedDoc(imagePath, signature, cropHeights, documentWidth, documentHeight, srcBitmap);
        }

        /// <summary>
        /// Converts Ps file to a Png file and stores it in the Temp Directory (if the conversion fails the program exits with error messages)
        /// </summary>
        /// <param name="filePath">Path to the PS file</param>
        /// <returns>Path to converted file</returns>
        private string PsToPng(string filePath)
        {
            if (GhostscriptVersionInfo.IsGhostscriptInstalled)
            {
                var temp = LOCAL_FOLDER + "\\Temp";
                FileInfo fileInfo = new FileInfo(filePath);
                var outputPath = Path.Combine(temp,
                    string.Format("{0}.png", Path.GetFileNameWithoutExtension(fileInfo.Name)));

                try
                {
                    using (GhostscriptProcessor processor = new GhostscriptProcessor())
                    {
                        List<string> switches = new List<string>();
                        switches.Add("empty");
                        switches.Add("-dSAFER");
                        switches.Add("-dBATCH");
                        switches.Add("-r300");
                        switches.Add("-sDEVICE=png16m");
                        switches.Add("-sOutputFile=" + outputPath);
                        switches.Add(filePath);

                        GeneralHelperClass.CreateDirectoryIfNotExists(temp);
                        processor.StartProcessing(switches.ToArray(), null);

                        return outputPath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Es kam zu einem Fehler. Mehr Informationen sind in den Log-Dateien in { LOCAL_FOLDER } hinterlegt.");
                    Console.WriteLine($"Application Shutdown: PsToPng conversion failed:\n {ex.ToString()}");
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show("Bitte installieren Sie Ghostscript. (Es wird die 64-bit Version benötigt)");
                Console.WriteLine("Application Shutdown: Ghostscript is not installed (64-bit needed)");
                Application.Current.Shutdown();
            }
            return "";
        }
    }
}