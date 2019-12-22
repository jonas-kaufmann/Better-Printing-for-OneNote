using Better_Printing_for_OneNote.AdditionalClasses;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote.Models
{
    class Conversion
    {
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
        /// <param name="cropHeight">height to split</param>
        /// <param name="srcBitmap">just ignore (its for the helper Method with "int cropHeight")</param>
        /// <returns>the document</returns>
        public static FixedDocument PngToFixedDoc(string imagePath, string signature, int[] cropHeights, double documentWidth = 793.7007874, double documentHeight = 1122.519685, Bitmap src = null)
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
        /// <returns>the document, null if exception is thrown</returns>
        public static FixedDocument PngToFixedDoc(string imagePath, string signature, int cropHeight = 3500, double documentWidth = 793.7007874, double documentHeight = 1122.519685)
        {
            try
            {
                var srcBitmap = System.Drawing.Image.FromFile(imagePath) as Bitmap;
                var splits = (int)Math.Ceiling(decimal.Divide(srcBitmap.Height, cropHeight));
                var cropHeights = new int[splits];
                for (int i = 0; i < splits - 1; i++)
                    cropHeights[i] = cropHeight;
                cropHeights[splits - 1] = srcBitmap.Height - (splits - 1) * cropHeight;
                return PngToFixedDoc(imagePath, signature, cropHeights, documentWidth, documentHeight, srcBitmap);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.");
                Console.WriteLine($"PngToFixedDoc conversion failed [probably because of corrupt or empty Postscript file]:\n {ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Converts Ps file to a Png file and stores it in the Temp Directory (if ghostscript is not installed the program exits with error messages)
        /// </summary>
        /// <param name="filePath">Path to the PS file</param>
        /// <param name="localFolder">Path to the local folder for MessageBox</param>
        /// <returns>Path to converted file or empty string if ps file ist corrupted</returns>
        public static string PsToPng(string filePath, string localFolder)
        {
            if (GhostscriptVersionInfo.IsGhostscriptInstalled)
            {
                var temp = Path.GetTempPath();
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
                    MessageBox.Show($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.\n\nMehr Informationen sind in den Log-Dateien in { localFolder } hinterlegt.");
                    Console.WriteLine($"PsToPng conversion failed:\n {ex.ToString()}");
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
