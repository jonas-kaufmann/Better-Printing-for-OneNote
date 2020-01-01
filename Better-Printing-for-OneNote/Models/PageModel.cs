﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote.Models
{
    class PageModel
    {
        private const double SIGNATURE_HEIGHT = 0; // 15.96
        private const double PAGENUMBER_HEIGHT = 0; // 15.96

        public PageContent Page { get; private set; } = new PageContent();
        private Border HeightBorder;
        private FixedPage FixedPage;
        private Grid Grid;
        public int MaxCropHeight { get; private set; }
        private double Scaling;
        private TranslateTransform ShiftTransform = new TranslateTransform(0, 0);
        private double DocumentHeight;
        private Thickness Padding;

        private int _cropHeight;
        public int CropHeight
        {
            get
            {
                return _cropHeight;
            }
            set
            {
                if (value != _cropHeight)
                {
                    _cropHeight = value;
                    HeightBorder.Height = value;
                }
            }
        }

        private int _cropShift = 0;
        public int CropShift
        {
            get
            {
                return _cropShift;
            }
            set
            {
                if (value != _cropShift)
                {
                    _cropShift = value;
                    ShiftTransform.Y = -value;
                }
            }
        }

        #region Signature

        private TextBlock SignatureTB;

        private string _signature;
        public string Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                if (_signature != value)
                {
                    _signature = value;
                    SignatureTB.Text = value;
                }
            }
        }

        private bool _signatureEnabled;
        public bool SignatureEnabled
        {
            get
            {
                return _signatureEnabled;
            }
            set
            {
                if (value != _signatureEnabled)
                {
                    SignatureTB.IsEnabled = value;
                    _signatureEnabled = value;
                }
            }
        }

        #endregion

        #region Page Numbers

        private TextBlock PageNumberTB;

        private string _pageNumber;
        public string PageNumber
        {
            get
            {
                return _pageNumber;
            }
            set
            {
                if (_pageNumber != value)
                {
                    PageNumberTB.Text = value;
                    _pageNumber = value;
                }
            }
        }

        private bool _pageNumberEnabled = false;
        public bool PageNumbersEnabled
        {
            get
            {
                return _pageNumberEnabled;
            }
            set
            {
                if (value != _pageNumberEnabled)
                {
                    PageNumberTB.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                    _pageNumberEnabled = value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a new Page (access the Page over the "Page" property to add it to a FixedDocument e.g.)
        /// </summary>
        /// <param name="image">the content image</param>
        /// <param name="contentHeight">the height of the content of the page (document height - padding)</param>
        /// <param name="contentWidth">the width of the content of the page (document width - padding)</param>
        /// <param name="documentHeight">the height of the page</param>
        /// <param name="documentWidth">the width of the page</param>
        /// <param name="padding">the padding of the page</param>
        public PageModel(WriteableBitmap image, double documentHeight, double documentWidth, double contentHeight, double contentWidth, Thickness padding)
        {
            FixedPage = new FixedPage() { Height = documentHeight, Width = documentWidth };
            DocumentHeight = documentHeight;
            Padding = padding;
            Page.Child = FixedPage;

            Grid = new Grid() { Height = contentHeight, Width = contentWidth, Margin = padding };
            Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(SIGNATURE_HEIGHT) });
            Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(PAGENUMBER_HEIGHT) });
            FixedPage.Children.Add(Grid);

            SignatureTB = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center };
            Grid.Children.Add(SignatureTB);

            var border = new Border() { VerticalAlignment = VerticalAlignment.Top };
            Grid.Children.Add(border);
            Grid.SetRow(border, 1);

            PageNumberTB = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Right, Visibility = Visibility.Hidden };
            Grid.Children.Add(PageNumberTB);
            Grid.SetRow(PageNumberTB, 2);

            HeightBorder = new Border();
            Scaling = contentWidth / image.PixelWidth;
            HeightBorder.LayoutTransform = new ScaleTransform(Scaling, Scaling);
            border.Child = HeightBorder;

            var constBorder = new Border() { Width = image.PixelWidth, Height = image.PixelHeight };
            HeightBorder.Child = constBorder;

            var imageControl = new Image() { Source = image };
            imageControl.RenderTransform = ShiftTransform;
            constBorder.Child = imageControl;

            MaxCropHeight = (int)Math.Round((image.PixelWidth * (contentHeight - SIGNATURE_HEIGHT - PAGENUMBER_HEIGHT)) / contentWidth);
            CropHeight = MaxCropHeight;
        }

        /// <summary>
        /// Calculates the height to split the image at to match the SplitAtPercentage
        /// </summary>
        /// <param name="splitAtPercentage">the position relative to the whole page</param>
        /// <returns>the height to split the image at</returns>
        public int CalculateSplitHeight(double splitAtPercentage)
        {
            var splitHeight = (int)Math.Round((splitAtPercentage * DocumentHeight - SIGNATURE_HEIGHT - Padding.Top) / Scaling);
            if (splitHeight > MaxCropHeight) return MaxCropHeight;
            else if (splitHeight < 0) return 0;
            else return splitHeight;
        }
    }
}