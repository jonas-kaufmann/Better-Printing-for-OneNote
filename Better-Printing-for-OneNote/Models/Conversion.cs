using Better_Printing_for_OneNote.AdditionalClasses;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote.Models
{
    class Conversion
    {
        private const int PNG_FILE_TOP_MARGIN = 150;
        private const GraphicsUnit GRAPHICS_UNIT = GraphicsUnit.Document;

        /// <summary>
        /// Changes the signature of all Sites without recreating the document
        /// </summary>
        /// <param name="document">modified Fixeddocument</param>
        /// <param name="signature">signature to set</param>
        public static void ChangeSignature(FixedDocument document, string signature)
        {
            if (document != null)
                // Signatur anpassen
                foreach (var page in document.Pages)
                    ((page.Child.Children[0] as StackPanel).Children[0] as TextBlock).Text = signature;
        }

        /// <summary>
        /// Converts png to a FixedDocument
        /// </summary>
        /// <param name="imagePath">path to the image to convert</param>
        /// <param name="documentWidth">width of the output document; default: DINA4</param>
        /// <param name="documentHeight">height of the output document; default: DINA4</param>
        /// <param name="signature">signature on every site of the document</param>
        /// <param name="cropHeights">heights to split as array</param>
        /// <param name="src">just ignore (its for the helper Method with "int cropHeight")</param>
        /// <returns>the document and the cropheights, null if exception ist thrown</returns>
        public static PngToFdOutput PngToFixedDoc(string imagePath, string signature, List<int> cropHeights, double documentWidth = 793.7007874, double documentHeight = 1122.519685, Bitmap src = null)
        {
            try
            {
                FixedDocument document = new FixedDocument();
                Bitmap srcBitmap = src ?? System.Drawing.Image.FromFile(imagePath) as Bitmap;
                var cropPostionY = PNG_FILE_TOP_MARGIN;
                foreach (var cropHeight in cropHeights)
                {
                    // Crop Image
                    Rectangle cropRect;
                    cropRect = new Rectangle(0, cropPostionY, srcBitmap.Width, cropHeight);
                    cropPostionY += cropHeight;
                    var targetBitmap = new Bitmap(cropRect.Width, cropRect.Height);
                    using (Graphics g = Graphics.FromImage(targetBitmap))
                    {
                        g.DrawImage(srcBitmap, new Rectangle(0, 0, targetBitmap.Width, targetBitmap.Height), cropRect, GRAPHICS_UNIT);
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

                return new PngToFdOutput() { CropHeights = cropHeights, Document = document };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.");
                Trace.WriteLine($"\nPngToFixedDoc conversion failed [probably because of corrupt or empty Postscript file]:\n {ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Converts png to a FixedDocument
        /// </summary>
        /// <param name="imagePath">path to the image to convert</param>
        /// <param name="documentWidth">width of the output document; default: DINA4</param>
        /// <param name="documentHeight">height of the output document; default: DINA4</param>
        /// <param name="signature">signature on every site of the document</param>
        /// <param name="cropHeight">height to split</param>
        /// <returns>the document and the cropheights, null if exception is thrown</returns>
        public static PngToFdOutput PngToFixedDoc(string imagePath, string signature, int cropHeight, double documentWidth = 793.7007874, double documentHeight = 1122.519685)
        {
            try
            {
                var srcBitmap = System.Drawing.Image.FromFile(imagePath) as Bitmap;
                var splits = (int)Math.Ceiling(decimal.Divide(srcBitmap.Height, cropHeight));
                var cropHeights = new List<int>();
                for (int i = 0; i < splits - 1; i++)
                    cropHeights.Add(cropHeight);
                cropHeights.Add(srcBitmap.Height - (splits - 1) * cropHeight);
                return PngToFixedDoc(imagePath, signature, cropHeights, documentWidth, documentHeight, srcBitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.");
                Trace.WriteLine($"\nPngToFixedDoc conversion failed [probably because of corrupt or empty Postscript file]:\n {ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Converts Ps file to a Png file and stores it in the Temp Directory (if ghostscript is not installed the program exits with error messages)
        /// </summary>
        /// <param name="filePath">Path to the PS file</param>
        /// <param name="localFolder">Path to the local folder for MessageBox</param>
        /// <param name="tempFolderPath">Path to the temp folder (or antother folder to store temporary files in)</param>
        /// <returns>Path to converted file or empty string if ps file ist corrupted</returns>
        public static string PsToPng(string filePath, string localFolder, string tempFolderPath)
        {
            if (GhostscriptVersionInfo.IsGhostscriptInstalled)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                var outputPath = Path.Combine(tempFolderPath,
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

                        GeneralHelperClass.CreateDirectoryIfNotExists(tempFolderPath);
                        processor.StartProcessing(switches.ToArray(), null);

                        return outputPath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.\n\nMehr Informationen sind in den Log-Dateien in { localFolder } hinterlegt.");
                    Trace.WriteLine($"\nPsToPng conversion failed:\n {ex.ToString()}");
                }
            }
            else
            {
                MessageBox.Show("Bitte installieren Sie Ghostscript. (Es wird die 64-bit Version benötigt)");
                Trace.WriteLine("\nApplication Shutdown: Ghostscript is not installed (64-bit needed)");
                Application.Current.Shutdown();
            }
            return "";
        }
    }

    class PngToFdOutput
    {
        public FixedDocument Document;
        public List<int> CropHeights;
    }
}