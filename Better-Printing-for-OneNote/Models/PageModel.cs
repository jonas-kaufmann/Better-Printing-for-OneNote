using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote.Models
{
    class PageModel
    {
        private const double SIGNATURE_HEIGHT = 15.96; // 15.96
        private const double PAGENUMBERS_HEIGHT = 15.96; // 15.96

        public PageContent Page { get; private set; } = new PageContent();
        private Border HeightBorder;
        private FixedPage FixedPage;
        private Grid Grid;
        public int MaxCropHeight { get; private set; }
        private double Scaling;
        private TranslateTransform ShiftTransform = new TranslateTransform(0, 0);
        private double DocumentHeight;
        private Thickness Padding;
        private TextBlock SignatureTB;
        private bool SignatureEnabled;
        private TextBlock PageNumberTB;
        private bool PageNumbersEnabled;

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

        /// <summary>
        /// Creates a new Page (access the Page over the "Page" property to add it to a FixedDocument e.g.)
        /// </summary>
        /// <param name="image">the content image</param>
        /// <param name="contentHeight">the height of the content of the page (document height - padding)</param>
        /// <param name="contentWidth">the width of the content of the page (document width - padding)</param>
        /// <param name="documentHeight">the height of the page</param>
        /// <param name="documentWidth">the width of the page</param>
        /// <param name="padding">the padding of the page</param>
        /// <param name="pageNumbersEnabled">page numbers enabled</param>
        /// <param name="signature">the signature</param>
        /// <param name="signatureEnabled">signature enabled</param>
        public PageModel(BitmapSource image, double documentHeight, double documentWidth, double contentHeight, double contentWidth, Thickness padding, bool pageNumbersEnabled, bool signatureEnabled, string signature)
        {
            FixedPage = new FixedPage() { Height = documentHeight, Width = documentWidth };
            RenderOptions.SetBitmapScalingMode(FixedPage, BitmapScalingMode.HighQuality);
            DocumentHeight = documentHeight;
            Padding = padding;
            Page.Child = FixedPage;

            Grid = new Grid() { Height = contentHeight, Width = contentWidth, Margin = padding };
            FixedPage.Children.Add(Grid);

            // Signature
            double extraContentPadding = 0;
            SignatureEnabled = signatureEnabled;
            if (SignatureEnabled)
            {
                SignatureTB = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Center, Height = SIGNATURE_HEIGHT, Text = signature };
                Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(SIGNATURE_HEIGHT) });
                Grid.Children.Add(SignatureTB);
                Grid.SetRow(SignatureTB, Grid.Children.Count-1);
                extraContentPadding += SIGNATURE_HEIGHT;
            }

            // Image
            Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            var border = new Border() { VerticalAlignment = VerticalAlignment.Top };
            Grid.Children.Add(border);
            Grid.SetRow(border, Grid.Children.Count-1);

            HeightBorder = new Border();
            Scaling = contentWidth / image.PixelWidth;
            HeightBorder.LayoutTransform = new ScaleTransform(Scaling, Scaling);
            border.Child = HeightBorder;

            var constBorder = new Border() { Width = image.PixelWidth, Height = image.PixelHeight };
            HeightBorder.Child = constBorder;

            var imageControl = new Image() { Source = image };
            imageControl.RenderTransform = ShiftTransform;
            constBorder.Child = imageControl;

            // Page Numbers
            PageNumbersEnabled = pageNumbersEnabled;
            if (PageNumbersEnabled)
            {
                Grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(PAGENUMBERS_HEIGHT) });
                PageNumberTB = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Right };
                Grid.Children.Add(PageNumberTB);
                Grid.SetRow(PageNumberTB, Grid.Children.Count-1);
                extraContentPadding += PAGENUMBERS_HEIGHT;
            }

            MaxCropHeight = (int)Math.Round((image.PixelWidth * (contentHeight - extraContentPadding)) / contentWidth);
            PageNumbersEnabled = pageNumbersEnabled;
            CropHeight = MaxCropHeight;
        }

        /// <summary>
        /// Sets the page number
        /// </summary>
        /// <param name="pageNumber">the page number text (e.g. 1/5)</param>
        public void SetPageNumber(string pageNumber)
        {
            if (PageNumbersEnabled)
                PageNumberTB.Text = pageNumber;
        }

        /// <summary>
        /// Sets the signature
        /// </summary>
        /// <param name="signature">the signature</param>
        public void SetSignature(string signature)
        {
            if (SignatureEnabled)
                SignatureTB.Text = signature;
        }

        /// <summary>
        /// Calculates the height to split the image at to match the SplitAtPercentage
        /// </summary>
        /// <param name="splitAtPercentage">the position relative to the whole page</param>
        /// <returns>the height to split the image at</returns>
        public int CalculateSplitHeight(double splitAtPercentage)
        {
            int splitHeight;
            if(SignatureEnabled)
                splitHeight = (int)Math.Round((splitAtPercentage * DocumentHeight - SIGNATURE_HEIGHT - Padding.Top) / Scaling);
            else
                splitHeight = (int)Math.Round((splitAtPercentage * DocumentHeight - Padding.Top) / Scaling);
            if (splitHeight > MaxCropHeight) return MaxCropHeight;
            else if (splitHeight < 0) return 0;
            else return splitHeight;
        }

        public void AddUIElement(UIElement element)
        {
            Grid.Children.Add(element);
        }

        public void RemoveUIElement(UIElement element)
        {
            if (Grid.Children.Contains(element))
                Grid.Children.Remove(element);
        }
    }
}