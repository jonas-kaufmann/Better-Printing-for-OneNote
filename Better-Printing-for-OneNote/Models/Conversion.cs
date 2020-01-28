using Better_Printing_for_OneNote.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote
{
    class Conversion
    {
        private const int DPI = 600;
        private const double PERCENTAGE_ROWS_FOR_MATCHING = 0.01;
        private const double ROWS_TO_CHECK_PERCENTAGE = .05;
        private const double MAX_WRONG_PIXELS_PERCENTAGE = 0.01;

        /// <summary>
        /// Converts a PDF document with (multiple) pages to one or multiple bitmaps if the document doesn't fit in a single one
        /// </summary>
        /// <param name="filePath">Path to the Ps file</param>
        /// <param name="ct">the cancellation token</param>
        /// <exception cref="ConversionFailedException">thrown if something went wrong</exception>
        public static BitmapSource[] ConvertPDFToBitmaps(string filePath, CancellationToken ct, ProgressReporter reporter)
        {
            if (File.Exists(filePath))
            {
                // retrieve number of pages
                int pageCount;
                try
                {
                    pageCount = XPDF.GetPageCount(filePath);
                }
                catch
                {
                    throw new ConversionFailedException("Could not read number of pages");
                }

                ct.ThrowIfCancellationRequested();

                reporter.ReportProgress("Processing page 1");

                // read first page
                List<List<byte>> finalImages = new List<List<byte>>();
                finalImages.Add(new List<byte>());
                string pipeName = "BPfON";
                PPMImage currentImage;
                try
                {
                    currentImage = XPDF.GetPageAsPPM(filePath, 1, DPI, false, false, pipeName);
                }
                catch
                {
                    throw new ConversionFailedException("Page 1 couldn't be read");
                }
                int width = currentImage.Width;
                int height = currentImage.Height;
                int bytesPerPixel = currentImage.BytesPerPixel;
                int stride = currentImage.Width * bytesPerPixel;

                ct.ThrowIfCancellationRequested();

                // load next image asynchronously
                PPMImage nextImage = null;
                int nextFirstNonWhiteRow = 0;
                int nextLastNonWhiteRow = 0;
                Task task = null;
                if (pageCount > 1)
                    try
                    {
                        task = Task.Run(() => nextImage = XPDF.GetPageAsPPM(filePath, 2, DPI, false, false, pipeName));
                    }
                    catch
                    {
                        throw new ConversionFailedException("Page 2 couldn't be read");
                    }

                // Add first image without white borders to final list
                int firstNonWhiteRow = FirstNonWhiteRow(currentImage.Pixels, width, height, bytesPerPixel);
                ct.ThrowIfCancellationRequested();
                int lastNonWhiteRow = LastNonWhiteRow(currentImage.Pixels, width, height, bytesPerPixel);
                ct.ThrowIfCancellationRequested();
                finalImages[0].AddRange(currentImage.Pixels[(firstNonWhiteRow * stride)..(lastNonWhiteRow * stride)]);

                int numberRowsForMatching = (int)(height * PERCENTAGE_ROWS_FOR_MATCHING);
                int currentFinalImageIndex = 0;
                for (int i = 1; i < pageCount; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    if (task != null)
                    {
                        task.Wait();
                        currentImage = nextImage;
                        firstNonWhiteRow = nextFirstNonWhiteRow;
                        lastNonWhiteRow = nextLastNonWhiteRow;
                    }

                    ct.ThrowIfCancellationRequested();

                    // load next image asynchronously
                    task = null;
                    if (i < pageCount - 1)
                        try
                        {
                            task = Task.Run(() => nextImage = XPDF.GetPageAsPPM(filePath, i + 2, DPI, false, false, pipeName));
                        }
                        catch
                        {
                            throw new ConversionFailedException($"Page {i + 2} couldn't be read");
                        }

                    ct.ThrowIfCancellationRequested();

                    firstNonWhiteRow = FirstNonWhiteRow(currentImage.Pixels, width, height, bytesPerPixel);
                    lastNonWhiteRow = LastNonWhiteRow(currentImage.Pixels, width, height, bytesPerPixel);

                    try
                    {
                        int yOffset = FindMatchingRows(finalImages[currentFinalImageIndex], currentImage.Pixels[(stride * firstNonWhiteRow)..(stride * (firstNonWhiteRow + numberRowsForMatching))], (int)Math.Round(ROWS_TO_CHECK_PERCENTAGE * height), width, bytesPerPixel);
                        finalImages[currentFinalImageIndex].RemoveRange(finalImages[currentFinalImageIndex].Count - yOffset * stride, yOffset * stride);
                    }
                    catch (RowNotFoundException) { }

                    ct.ThrowIfCancellationRequested();

                    // if new image doesn't fit in list, create a new one
                    if ((long)finalImages[currentFinalImageIndex].Count + (lastNonWhiteRow * stride - firstNonWhiteRow * stride) > int.MaxValue / 4)
                    {
                        finalImages.Add(new List<byte>());
                        currentFinalImageIndex++;
                    }
                    finalImages[currentFinalImageIndex].AddRange(currentImage.Pixels[(firstNonWhiteRow * stride)..(lastNonWhiteRow * stride)]);

                    reporter.ReportProgress(i * 100 / pageCount, $"Processing page {i + 1}");
                }

                ct.ThrowIfCancellationRequested();

                BitmapSource[] bitmapSources = new BitmapSource[finalImages.Count];
                for (int i = 0; i < bitmapSources.Length; i++) {
                    bitmapSources[i] = BitmapSource.Create(width, finalImages[i].Count / stride, DPI, DPI, PixelFormats.Rgb24, null, finalImages[i].ToArray(), stride);
                }

                return bitmapSources;
            }
            else
                throw new ConversionFailedException($"The file to be opened ({filePath}) doesn't exist.");
        }

        /// <summary>
        /// Searches for the first non white row from the top (throws RowNotFoundException)
        /// </summary>
        private static int FirstNonWhiteRow(byte[] pixels, int width, int height, int bytesPerPixel)
        {
            int stride = width * bytesPerPixel;
            // find first not white row
            for (int a = 0; a < (int)(ROWS_TO_CHECK_PERCENTAGE * height) * stride; a++)
            {
                if (pixels[a] != 255)
                    return (a / stride);
            }
            return 0;
        }

        /// <summary>
        /// Searches for the last non white row from the bottom (throws RowNotFoundException)
        /// </summary>
        private static int LastNonWhiteRow(byte[] pixels, int width, int height, int bytesPerPixel)
        {
            int stride = width * bytesPerPixel;
            // find first not white row
            for (int a = pixels.Length - 1; a > (int)((1 - ROWS_TO_CHECK_PERCENTAGE) * height) * stride; a--)
            {
                if (pixels[a] != 255)
                    return a / stride;
            }

            return height - 1;
        }

        private static int FindMatchingRows(List<byte> searchImg, byte[] rowsToMatchAgainst, int numberRowsToCheckInSearchImg, int width, int bytesPerPixel)
        {
            int stride = width * bytesPerPixel;
            int numberRowsToMatch = rowsToMatchAgainst.Length / stride;
            int maxDifferentPixels = (int)Math.Round(MAX_WRONG_PIXELS_PERCENTAGE * width * numberRowsToMatch);
            int equalRowAtOffsetYBottom = numberRowsToMatch;
            int yOffsetBoundary = searchImg.Count - stride * numberRowsToCheckInSearchImg;
            for (int yOffset = searchImg.Count - stride * numberRowsToMatch; yOffset > yOffsetBoundary; yOffset -= stride)
            {
                int numberDifferentPixels = 0;

                for (int a = 0; a < numberRowsToMatch * stride; a += bytesPerPixel)
                {
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        if (searchImg[yOffset + a + i] != rowsToMatchAgainst[a + i])
                        {
                            numberDifferentPixels++;

                            if (numberDifferentPixels > maxDifferentPixels)
                            {
                                equalRowAtOffsetYBottom++;
                                goto StartAtNextRow;
                            }

                            break;
                        }
                    }
                }
                return equalRowAtOffsetYBottom;
            StartAtNextRow:;
            }

            throw new RowNotFoundException("No matching rows have been found");
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
