using Better_Printing_for_OneNote.AdditionalClasses;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote
{
    class Conversion
    {
        private const int DPI = 600; // 300
        private const int ROWS_TO_CHECK = 30; // 30
        private const int MAX_WRONG_PIXELS = 150; // 50
        private const double SECTION_TO_CHECK = 0.15;

        /// <summary>
        /// Converts a Postscript document with (multiple) pages to one Bitmap (removes the created files after conversion) (throws ConversionFailedException if something went wrong)
        /// </summary>
        /// <param name="filePath">Path to the Ps file</param>
        /// <param name="tempFolder">Path to the temp folder (or another folder to store temporary files in)</param>
        /// <param name="ct">the cancellation token</param>
        /// <returns>the bitmap</returns>
        public static WriteableBitmap ConvertPsToOneImage(string filePath, string tempFolder, CancellationToken ct)
        {
            if (File.Exists(filePath))
            {
                var paths = PsToBmp(filePath, tempFolder);
                ct.ThrowIfCancellationRequested();
                var bitmap = CombineImages(paths, ct);
                // run Task to clear the files
                Task.Run(() =>
                {
                    while (paths.Count > 0)
                    {
                        try
                        {
                            File.Delete(paths[0]);
                            paths.RemoveAt(0);
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine($"Could not delete the file {paths[0]}. Trying again. Exception:\n{e.ToString()}");
                            Thread.Sleep(500);
                        }
                    }
                });

                return bitmap;
            }
            else
                throw new ConversionFailedException($"Die zu öffnende Postscript Datei ({filePath}) existiert nicht.");
        }

        /// <summary>
        /// Converts a Postscript file to multiple Bitmap files and stores them in the Temp Directory (throws ConversionFailedException if something went wrong)
        /// </summary>
        /// <param name="filePath">Path to the Ps file</param>
        /// <param name="tempFolderPath">Path to the temp folder (or another folder to store temporary files in)</param>
        /// <returns>Paths to the converted files</returns>
        private static List<string> PsToBmp(string filePath, string tempFolderPath)
        {
            if (GhostscriptVersionInfo.IsGhostscriptInstalled)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                var outputPath = Path.Combine(tempFolderPath, Path.GetFileNameWithoutExtension(fileInfo.Name));

                try
                {
                    using (GhostscriptProcessor processor = new GhostscriptProcessor())
                    {
                        List<string> switches = new List<string>();
                        switches.Add("empty");
                        switches.Add("-dSAFER");
                        switches.Add("-sDEVICE=bmp16m");
                        switches.Add($"-r{DPI}");
                        switches.Add("-o");
                        switches.Add($"\"{outputPath}_%d.bmp\"");
                        switches.Add($"\"{filePath}\"");

                        GeneralHelperClass.CreateDirectoryIfNotExists(tempFolderPath);
                        var outputHandler = new GSOutputStdIO();
                        processor.StartProcessing(switches.ToArray(), outputHandler);

                        var paths = new List<string>();
                        for (int i = 1; i <= outputHandler.Pages; i++)
                            paths.Add($"{outputPath}_{i}.bmp");

                        if (paths.Count < 1)
                        {
                            Trace.WriteLine($"\nPsToBmp conversion failed. Probably because there is no page in the document or the document is corrupted.");
                            throw new ConversionFailedException($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.");
                        }

                        return paths;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ConversionFailedException)
                        throw ex;
                    else
                    {
                        Trace.WriteLine($"\nPsToBmp conversion failed:\n {ex.ToString()}");
                        throw new ConversionFailedException($"Es kam zu einem Fehler bei der Konvertierung der PostScript Datei. Bitte mit anderer PostScript Datei erneut versuchen.\n\nMehr Informationen sind in den Log-Dateien hinterlegt.");
                    }
                }
            }
            else
            {
                Trace.WriteLine("\nApplication Shutdown: Ghostscript is not installed (64-bit needed)");
                throw new ConversionFailedException("Bitte installieren Sie Ghostscript. (Es wird die 64 - bit Version benötigt)");
            }
        }

        /// <summary>
        /// Combines multiple images to a big one by removing white rows at the top and bottom and removing duplicate rows
        /// </summary>
        /// <param name="imagePaths">the paths to the images</param>
        /// <returns>the final combined bitmap</returns>
        private static WriteableBitmap CombineImages(List<string> imagePaths, CancellationToken ct)
        {
            var previousImage = LoadBitmapIntoArray(imagePaths[0]); // first image
            var stride = previousImage.Stride;
            var height = previousImage.Height;
            var width = previousImage.Width;
            var format = previousImage.Format;
            var palette = previousImage.Palette;

            ct.ThrowIfCancellationRequested();

            // find first not white row in the first image
            int topOffset1 = 0;
            try
            {
                topOffset1 = FirstNotWhiteRow(previousImage.Pixels, stride, previousImage.Height);
            }
            catch (RowNotFoundException e)
            {
                Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[0]}\" has no not white row. Using the full image. Exception:\n {e.ToString()}");
            }

            ct.ThrowIfCancellationRequested();

            byte[] finalImageArray;
            if (imagePaths.Count > 1)
            {
                var finalImageList = new List<byte>();
                // go through all images after the first
                for (int i = 1; i < imagePaths.Count; i++)
                {
                    var image = LoadBitmapIntoArray(imagePaths[i]);

                    ct.ThrowIfCancellationRequested();

                    // find offset to the first not white row in the image
                    var topOffset2 = 0;
                    try
                    {
                        topOffset2 = FirstNotWhiteRow(image.Pixels, stride, image.Height);
                    }
                    catch (RowNotFoundException e)
                    {
                        Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[i]}\" has no not white row. Using the full image. Exception:\n {e.ToString()}");
                    }

                    ct.ThrowIfCancellationRequested();

                    // build the rowsequence after the first not white row (inclusive)
                    var rowSequence = new List<byte[]>();
                    for (int c = 0; c < ROWS_TO_CHECK; c++)
                    {
                        var row = new byte[stride];
                        for (int j = 0; j < stride; j++)
                            row[j] = image.Pixels[topOffset2 + j + c * stride];
                        rowSequence.Add(row);
                    }

                    ct.ThrowIfCancellationRequested();

                    // find equal row
                    var bottomOffset1 = 0;
                    try
                    {
                        bottomOffset1 = FindMatchingRowSequence(previousImage.Pixels, rowSequence, stride, previousImage.Height);
                    }
                    catch (RowNotFoundException e)
                    {
                        Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[i - 1]}\" and \"{imagePaths[i]}\" have no equal row. Using the full image. Exception:\n {e.ToString()}");
                        // cut off the white space under image and use that in case of no equal row
                        try
                        {
                            bottomOffset1 = LastNotWhiteRow(previousImage.Pixels, stride, previousImage.Height);
                        }
                        catch (RowNotFoundException ex)
                        {
                            Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[i - 1]}\" has no not white row. Using the full image. Exception:\n {ex.ToString()}");
                        }
                    }

                    ct.ThrowIfCancellationRequested();

                    // copy pixels from the previous to the final image
                    for (int j = topOffset1; j < bottomOffset1; j++)
                        finalImageList.Add(previousImage.Pixels[j]);

                    previousImage = image;
                    topOffset1 = topOffset2;

                    // copy pixels from the last image
                    if (i == imagePaths.Count - 1)
                    {
                        var bottomOffset = 0;
                        try
                        {
                            bottomOffset = LastNotWhiteRow(image.Pixels, stride, image.Height);
                        }
                        catch (RowNotFoundException ex)
                        {
                            Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[i]}\" has no not white row. Using the full image. Exception:\n {ex.ToString()}");
                        }

                        ct.ThrowIfCancellationRequested();

                        for (int j = topOffset2; j < bottomOffset; j++)
                            finalImageList.Add(image.Pixels[j]);
                    }
                }
                finalImageArray = finalImageList.ToArray();
                finalImageList = null;
            }
            else
            {
                // copy all pixels from the first except the white rows
                var bottomOffset = 0;
                try
                {
                    bottomOffset = LastNotWhiteRow(previousImage.Pixels, stride, previousImage.Height);
                }
                catch (RowNotFoundException ex)
                {
                    Trace.WriteLine($"\nCombining the images failed because \"{imagePaths[0]}\" has no not white row. Using the full image. Exception:\n {ex.ToString()}");
                }

                ct.ThrowIfCancellationRequested();

                finalImageArray = new byte[bottomOffset - topOffset1];
                for (int j = 0; j < bottomOffset - topOffset1; j++)
                    finalImageArray[j] = previousImage.Pixels[j + topOffset1];
            }
            previousImage = null;

            ct.ThrowIfCancellationRequested();

            // rewrite bytes to Bitmap
            var heightFinal = finalImageArray.Length / stride;
            var finalBitmap = new WriteableBitmap(width, heightFinal, DPI, DPI, format, palette);
            finalBitmap.WritePixels(new Int32Rect(0, 0, width, heightFinal), finalImageArray, stride, 0);

            return finalBitmap;
        }

        /// <summary>
        /// Loads a bitmap file from the harddrive and loads the data into an bytearray
        /// </summary>
        /// <param name="path">the path to the .bmp file</param>
        /// <returns>the array and some information about the image</returns>
        private static BitmapInformation LoadBitmapIntoArray(string path)
        {
            // load the image
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            // write information to the output object
            var output = new BitmapInformation() { Palette = bitmapImage.Palette, Format = bitmapImage.Format, Height = bitmapImage.PixelHeight, Width = bitmapImage.PixelWidth };

            // copy pixels to the array
            var bytesPerPixel = bitmapImage.Format.BitsPerPixel / 8; // BitsPerPixel: 32 -> BytesPerPixel: hier 4 (s, R, G, B)
            output.Stride = bitmapImage.PixelWidth * bytesPerPixel;  // stride: Bytes in einer Reihe
            output.Pixels = new byte[output.Stride * bitmapImage.PixelHeight];
            bitmapImage.CopyPixels(output.Pixels, output.Stride, 0);

            return output;
        }

        /// <summary>
        /// Searches for the first not white row from the top (throws RowNotFoundException)
        /// </summary>
        /// <param name="pixels">the pixel array to search in</param>
        /// <param name="stride">the bytes per row</param>
        /// <param name="imageHeight">the pixel height of the pixels</param>
        private static int FirstNotWhiteRow(byte[] pixels, int stride, int imageHeight)
        {
            // find first not white row
            for (int y = 0; y < imageHeight; y++)
            {
                // check all bytes of one row
                for (int b = 0; b < stride; b++)
                {
                    if (pixels[y * stride + b] != 255)
                        return y * stride;
                }
            }
            throw new RowNotFoundException("Could not find a not white row.");
        }

        /// <summary>
        /// Searches for the first not white row from the bottom (throws RowNotFoundException)
        /// </summary>
        /// <param name="pixels">the pixel array to search in</param>
        /// <param name="stride">the bytes per row</param>
        /// <param name="imageHeight">the pixel height of the pixels</param>
        private static int LastNotWhiteRow(byte[] pixels, int stride, int imageHeight)
        {
            // find first not white row
            for (int y = imageHeight - 1; y >= 0; y--)
            {
                // check all bytes of one row
                for (int b = 0; b < stride; b++)
                {
                    if (pixels[y * stride + b] != 255)
                        return y * stride;
                }
            }
            throw new RowNotFoundException("Could not find a not white row.");
        }

        /// <summary>
        /// Searches for the first matching rowsequence in the Pixelarray (throws RowNotFoundException)
        /// </summary>
        /// <param name="pixels">the pixel array to search in</param>
        /// <param name="rowSequence">the rowsequence to search</param>
        /// <param name="stride">the bytes per row</param>
        /// <param name="height">the pixel height of the pixels</param>
        /// <returns>the offset from the bottom (of the pixel array)</returns>
        private static int FindMatchingRowSequence(byte[] pixels, List<byte[]> rowSequence, int stride, int height)
        {
            int rowIndex = rowSequence.Count - 1;
            bool sequenceFound = false;
            int supposedPositionY = 0;
            for (int y = height - 1; y >= (1 - SECTION_TO_CHECK) * height; y--)
            {
                // check all bytes of one row
                var wrongPixels = 0;
                for (int b = 0; b < stride; b++)
                {
                    if (rowSequence[rowIndex][b] != pixels[y * stride + b])
                        wrongPixels++;
                    if (wrongPixels > MAX_WRONG_PIXELS)
                        goto NotEqual;
                }

                // row is a match 
                rowIndex--;
                if (rowIndex < 0)
                    return y * stride;
                if (!sequenceFound)
                {
                    supposedPositionY = y;
                    sequenceFound = true;
                }

                goto End;

            // row is no match
            NotEqual:
                if (sequenceFound)
                {
                    // reset the values if not the full sequence is matching
                    sequenceFound = false;
                    rowIndex = rowSequence.Count - 1;
                    y = supposedPositionY;
                }

            End:;

            }
            throw new RowNotFoundException("Could not find an equal row.");
        }

        class BitmapInformation
        {
            public System.Windows.Media.PixelFormat Format;
            public BitmapPalette Palette;
            public byte[] Pixels;
            public int Stride;
            public int Height;
            public int Width;
        }

        class RowNotFoundException : Exception
        {
            public RowNotFoundException(string message) : base(message) { }
        }

        class GSOutputStdIO : GhostscriptStdIO
        {
            public GSOutputStdIO() : base(false, true, false) { }

            private bool FirstOutput = true;
            public int Pages;

            public override void StdOut(string output)
            {
                if (output.Trim().Contains("LastPage")) Pages = output.Split("Page").Length - 2;

                // output to log/trace
                if (FirstOutput)
                {
                    FirstOutput = false;
                    Trace.Write($"\nGhostScript Protocoll:\n{output}");
                }
                else Trace.Write(output);
            }

            public override void StdError(string error) { }

            public override void StdIn(out string input, int count)
            {
                input = "";
            }
        }
    }

    class ConversionFailedException : Exception
    {
        public ConversionFailedException(string message) : base(message) { }
    }
}
