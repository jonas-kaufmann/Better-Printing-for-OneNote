using Better_Printing_for_OneNote.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote
{
    class Conversion
    {
        private const int DPI = 300;
        private const int ROWS_TO_CHECK = 30;
        private const double MAX_WRONG_PIXELS_PERCENTAGE = 8; // 1
        private const double SECTION_TO_CHECK = 0.15; // 0.15
        private const double FILE_CONVERSION_PROGRESS = 40;
        private const double ARRAY_TO_FINAL_BITMAP_CONVERSION = 7.5;

        /// <summary>
        /// Converts a PDF document with (multiple) pages to one or multiple bitmaps if the document doesn't fit in a single one
        /// </summary>
        /// <param name="filePath">Path to the Ps file</param>
        /// <param name="ct">the cancellation token</param>
        /// <exception cref="ConversionFailedException">thrown if something went wrong</exception>
        public static BitmapSource ConvertPDFToBitmaps(string filePath, CancellationToken ct, ProgressReporter reporter)
        {
            if (File.Exists(filePath))
            {
                reporter.ReportProgress("Converting document to bitmaps");

                int pageCount;
                try
                {
                    pageCount = XPDF.GetPageCount(filePath);
                }
                catch
                {
                    throw new ConversionFailedException("Could not read number of pages.");
                }

                ct.ThrowIfCancellationRequested();

                PPMImage[] rawImages = new PPMImage[pageCount];
                string pipeName = "BPfON";
                Parallel.For(1, pageCount + 1, new ParallelOptions() { CancellationToken = ct },i =>
                {
                    rawImages[i - 1] = XPDF.GetPageAsPPM(filePath, i, DPI, false, false, pipeName + i);
                });

                ct.ThrowIfCancellationRequested();

                reporter.ReportProgress(FILE_CONVERSION_PROGRESS);

                return CombineImages(rawImages, ct, reporter);
            }
            else
                throw new ConversionFailedException($"The file to be opened ({filePath}) doesn't exist.");
        }

        /// <summary>
        /// Combines multiple images to a big one by removing white rows at the top and bottom and removing duplicate rows
        /// </summary>
        /// <param name="imagePaths">the paths to the images</param>
        /// <returns>the final combined bitmap</returns>
        private static BitmapSource CombineImages(PPMImage[] rawImages, CancellationToken ct, ProgressReporter reporter)
        {
            reporter.ReportProgress("Loading Bitmap 1");

            var previousImage = rawImages[0]; // first image
            int width = previousImage.Width;
            int stride = width * previousImage.BytesPerPixel;
            var pixelFormat = PixelFormats.Rgb24;

            ct.ThrowIfCancellationRequested();

            // find first not white row in the first image
            int defaultTopOffset = 0;
            try
            {
                defaultTopOffset = FirstNotWhiteRow(previousImage.Pixels, previousImage.Width, previousImage.Height, previousImage.BytesPerPixel);
            }
            catch (RowNotFoundException e)
            {
                Trace.WriteLine($"\nCombining the images failed because image of page 0 doesn't have a non-white row. Using the full image. Exception:\n {e.ToString()}");
            }

            int defaultBottomOffset = previousImage.Height * stride - defaultTopOffset;

            ct.ThrowIfCancellationRequested();

            byte[] finalImageArray;
            var reportPercentagePerBitmap = (100 - FILE_CONVERSION_PROGRESS - ARRAY_TO_FINAL_BITMAP_CONVERSION) / rawImages.Length;
            if (rawImages.Length > 1)
            {
                var finalImageList = new List<byte>();
                // go through all images after the first
                for (int i = 1; i < rawImages.Length; i++)
                {
                    reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2, $"Loading Bitmap {i + 1}");

                    var image = rawImages[i];

                    ct.ThrowIfCancellationRequested();

                    // find offset to the first non-white row in the image
                    var topOffset2 = 0;
                    try
                    {
                        topOffset2 = FirstNotWhiteRow(image.Pixels, image.Width, image.Height, image.BytesPerPixel);
                    }
                    catch (RowNotFoundException e)
                    {
                        Trace.WriteLine($"\nCombining the images failed because page {i + 1} doesn't have a non-white row. Using the full image. Exception:\n {e.ToString()}");
                    }

                    ct.ThrowIfCancellationRequested();

                    // build the rowsequence after the first non-white row (inclusive)
                    var rowSequence = new List<byte[]>();
                    for (int c = 0; c < ROWS_TO_CHECK; c++)
                    {
                        var row = new byte[stride];
                        for (int j = 0; j < stride; j++)
                        {
                            row[j] = image.Pixels[topOffset2 + j + c * stride];
                        }
                        rowSequence.Add(row);
                    }

                    ct.ThrowIfCancellationRequested();

                    // find equal row
                    var bottomOffset1 = 0;
                    try
                    {
                        bottomOffset1 = FindMatchingRowSequence(previousImage.Pixels, rowSequence, previousImage.Width, previousImage.Height, previousImage.BytesPerPixel);
                    }
                    catch (RowNotFoundException e)
                    {
                        Trace.WriteLine($"\nCombining the images failed because pages {i} and {i + 1} don't have a matching row. Using the full image. Exception:\n {e.ToString()}");
                        // cut off the white space under image and use that in case of no equal row
                        bottomOffset1 = previousImage.Height * stride - defaultTopOffset;
                    }

                    ct.ThrowIfCancellationRequested();
                    reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2, $"Copying pixels from page {i + 1} into final Bitmap");

                    // copy pixels from the previous to the final image
                    for (int j = defaultTopOffset; j < bottomOffset1; j++)
                        finalImageList.Add(previousImage.Pixels[j]);

                    previousImage = image;
                    defaultTopOffset = topOffset2;

                    // copy pixels from the last image
                    if (i == rawImages.Length - 1)
                    {
                        reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2, $"Copying pixels from page {i + 1} into final Bitmap");

                        var bottomOffset = defaultBottomOffset;

                        ct.ThrowIfCancellationRequested();

                        int counter = 0;
                        for (int j = topOffset2; j < bottomOffset; j++)
                        {
                            
                            if (counter > 10000000)
                            {
                                ct.ThrowIfCancellationRequested();
                                counter = 0;
                            }
                                counter++;
                            finalImageList.Add(image.Pixels[j]);
                        }
                            
                    }
                }
                finalImageArray = finalImageList.ToArray();
                reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2);
            }
            else
            {
                // copy all pixels from the first except the white rows

                ct.ThrowIfCancellationRequested();

                reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2, "Copying pixels into final Bitmap");

                finalImageArray = new byte[defaultBottomOffset - defaultTopOffset];
                for (int j = 0; j < defaultBottomOffset - defaultTopOffset; j++)
                    finalImageArray[j] = previousImage.Pixels[j + defaultTopOffset];

                reporter.ReportProgress(reporter.PercentageCompleted + reportPercentagePerBitmap / 2);
            }

            ct.ThrowIfCancellationRequested();
            reporter.ReportProgress("Generating final Bitmap");
            return BitmapSource.Create(width, (int)(finalImageArray.LongLength / stride), DPI, DPI, PixelFormats.Rgb24, null, finalImageArray, stride);
        }

        /// <summary>
        /// Searches for the first not white row from the top (throws RowNotFoundException)
        /// </summary>
        private static int FirstNotWhiteRow(byte[] pixels, int width, int height, int bytesPerPixel)
        {
            int stride = width * bytesPerPixel;
            // find first not white row
            for (int y = 0; y < height; y++)
            {
                int rowStartIndex = y * stride;
                // check all bytes of one row
                for (int b = 0; b < stride; b++)
                {
                    if (pixels[rowStartIndex + b] != 255)
                        return y * stride;
                }
            }
            throw new RowNotFoundException("Could not find a not white row.");
        }

        /// <summary>
        /// Searches for the first matching rowsequence in the Pixelarray (throws RowNotFoundException)
        /// </summary>
        /// <param name="rowSequence">the rowsequence to search</param>
        /// <returns>the offset from the bottom (of the pixel array)</returns>
        private static int FindMatchingRowSequence(byte[] pixels, List<byte[]> rowSequence, int width, int height, int bytesPerPixel)
        {
            int rowIndex = rowSequence.Count - 1;
            bool sequenceFound = false;
            int supposedPositionY = 0;
            int stride = width * bytesPerPixel;
            var maxWrongBytes = Math.Round((MAX_WRONG_PIXELS_PERCENTAGE * stride) / 100);
            for (int y = height - 1; y >= (1 - SECTION_TO_CHECK) * height; y--)
            {
                // check all bytes of one row
                var wrongBytes = 0;
                for (int b = 0; b < stride; b++)
                {
                    if (rowSequence[rowIndex][b] != pixels[y * stride + b])
                        wrongBytes++;
                    if (wrongBytes > maxWrongBytes)
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

        class RowNotFoundException : Exception
        {
            public RowNotFoundException(string message) : base(message) { }
        }
    }

    class ConversionFailedException : Exception
    {
        public ConversionFailedException(string message) : base(message) { }
    }
}
