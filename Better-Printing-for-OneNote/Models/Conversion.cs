using Better_Printing_for_OneNote.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote
{
    class Conversion
    {
        private const int DPI = 300; // 600
        private const int ROWS_TO_CHECK = 30; // 30
        private const int MAX_WRONG_PIXELS = 150; // 50
        private const double SECTION_TO_CHECK = 0.15;

        /// <summary>
        /// Converts a PDF document with (multiple) pages to one or multiple bitmaps if the document doesn't fit in a single one
        /// </summary>
        /// <param name="filePath">Path to the Ps file</param>
        /// <param name="ct">the cancellation token</param>
        /// <exception cref="ConversionFailedException">thrown if something went wrong</exception>
        public static WriteableBitmap[] ConvertPDFToBitmaps(string filePath, CancellationToken ct)
        {
            if (File.Exists(filePath))
            {
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
                Parallel.For(1, pageCount + 1, i =>
                {
                    rawImages[i - 1] = XPDF.GetPageAsPPM(filePath, i, DPI, false, false, pipeName + i);
                });

                ct.ThrowIfCancellationRequested();

                return CombineImages(rawImages, ct);
            }
            else
                throw new ConversionFailedException($"The file to be opened ({filePath}) doesn't exist.");
        }

        /// <summary>
        /// Combines multiple images to a big one by removing white rows at the top and bottom and removing duplicate rows
        /// </summary>
        /// <param name="imagePaths">the paths to the images</param>
        /// <returns>the final combined bitmap</returns>
        private static WriteableBitmap[] CombineImages(PPMImage[] rawImages, CancellationToken ct)
        {
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
            if (rawImages.Length > 1)
            {
                var finalImageList = new List<byte>();
                // go through all images after the first
                for (int i = 1; i < rawImages.Length; i++)
                {
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
                            row[j] = image.Pixels[topOffset2 + j + c * stride];
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

                    // copy pixels from the previous to the final image
                    for (int j = defaultTopOffset; j < bottomOffset1; j++)
                        finalImageList.Add(previousImage.Pixels[j]);

                    previousImage = image;
                    defaultTopOffset = topOffset2;

                    // copy pixels from the last image
                    if (i == rawImages.Length - 1)
                    {
                        var bottomOffset = defaultBottomOffset;

                        ct.ThrowIfCancellationRequested();

                        for (int j = topOffset2; j < bottomOffset; j++)
                            finalImageList.Add(image.Pixels[j]);
                    }
                }
                finalImageArray = finalImageList.ToArray();
            }
            else
            {
                // copy all pixels from the first except the white rows

                ct.ThrowIfCancellationRequested();

                finalImageArray = new byte[defaultBottomOffset - defaultTopOffset];
                for (int j = 0; j < defaultBottomOffset - defaultTopOffset; j++)
                    finalImageArray[j] = previousImage.Pixels[j + defaultTopOffset];
            }

            ct.ThrowIfCancellationRequested();

            // rewrite bytes to Bitmap (split into more than one if array bigger than int.maxValue since WPF will choke on that
            long arraySizeOfBitmaps = finalImageArray.LongLength;
            int bitmapCount = (int)(arraySizeOfBitmaps / int.MaxValue) + 1;
            WriteableBitmap[] finalBitmaps = new WriteableBitmap[bitmapCount];
            //long beginReadAt = 0;
            //for (int i = 0; i < bitmapCount - 1; i++)
            //{
            //    int numberOfRows = int.MaxValue / stride;
            //    int bytesToWrite = numberOfRows * stride;
            //    WriteableBitmap bitmap = new WriteableBitmap(width, numberOfRows, DPI, DPI, pixelFormat, null);

            //    (byte[])finalImageArray.GetValue(beginReadAt, beginReadAt + bytesToWrite)

            //    bitmap.WritePixels(new Int32Rect(0, 0, width, numberOfRows), , stride, 0);
            //    finalImages[i] = bitmap;

            //    arraySizeOfBitmaps -= bytesToWrite;
            //}


            int numberOfRows = finalImageArray.Length / stride;
            WriteableBitmap finalBitmap = new WriteableBitmap(width, numberOfRows, DPI, DPI, pixelFormat, null);
            finalBitmap.WritePixels(new Int32Rect(0, 0, width, numberOfRows), finalImageArray, stride, 0);

            finalBitmaps[0] = finalBitmap;

            return finalBitmaps;
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
