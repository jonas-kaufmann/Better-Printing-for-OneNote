using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfCropableImageControl;

namespace Better_Printing_for_OneNote.Models
{
    public class PageModel
    {
        #region Properties

        public PageContent Page { get; private set; }
        private CropableImage CropableImage;
        public int MaxCropHeight { get; private set; }
        public int OptimalCropHeight { get; private set; } // relative to page

        private int _bigImageHeight;
        public int BigImageHeight
        {
            get
            {
                if (CropableImage != null && CropableImage.BigImageHeight.HasValue)
                    return CropableImage.BigImageHeight.Value;
                else return _bigImageHeight;
            }
            private set
            {
                if (_bigImageHeight != value)
                    _bigImageHeight = value;
            }
        }

        private int _bigImageWidth;
        public int BigImageWidth
        {
            get
            {
                if (CropableImage != null && CropableImage.BigImageWidth.HasValue)
                    return CropableImage.BigImageWidth.Value;
                else return _bigImageWidth;
            }
            private set
            {
                if (_bigImageWidth != value)
                    _bigImageWidth = value;
            }
        }

        public ArrayList Skips
        {
            get => CropableImage.Skips;
            set
            {
                if (value != CropableImage.Skips)
                    CropableImage.Skips = value;
            }
        }

        public double PageHeight
        {
            get => Page.Child.Height;
            set
            {
                if (value != Page.Child.Height)
                    Page.Child.Height = value;
            }
        }

        public double PageWidth
        {
            get => Page.Child.Width;
            set
            {
                if (value != Page.Child.Width)
                    Page.Child.Width = value;
            }
        }

        public Thickness ContentPadding
        {
            get => CropableImage.Margin;
            set
            {
                if (value != CropableImage.Margin)
                    CropableImage.Margin = value;
            }
        }

        public double ContentHeight
        {
            get => CropableImage.Height;
            set
            {
                if (value != CropableImage.Height)
                    CropableImage.Height = value;
            }
        }

        public double ContentWidth
        {
            get => CropableImage.Width;
            set
            {
                if (value != CropableImage.Width)
                    CropableImage.Width = value;
            }
        }

        public BitmapSource[] Images
        {
            get => CropableImage.Images as BitmapSource[];
            set
            {
                if (value != CropableImage.Images)
                    CropableImage.Images = value;
            }
        }

        public int CropHeight
        {
            get => CropableImage.CropHeight.Value;
            set
            {
                if (value != CropableImage.CropHeight)
                    CropableImage.CropHeight = value;
            }
        }

        public int CropShift
        {
            get => CropableImage.ShiftY;
            set
            {
                if (value != CropableImage.ShiftY)
                    CropableImage.ShiftY = value;
            }
        }

        #endregion

        public PageModel(BitmapSource[] images, ArrayList skips, double pageHeight, double pageWidth, double contentHeight, double contentWidth, Thickness contentPadding)
        {
            // initialize the page
            Page = new PageContent();
            var fixedPage = new FixedPage();
            CropableImage = new CropableImage();
            RenderOptions.SetBitmapScalingMode(CropableImage, BitmapScalingMode.HighQuality);
            fixedPage.Children.Add(CropableImage);
            Page.Child = fixedPage;

            Images = images;
            Skips = skips;
            PageHeight = pageHeight;
            PageWidth = pageWidth;
            ContentHeight = contentHeight;
            ContentWidth = contentWidth;
            ContentPadding = contentPadding;

            BigImageHeight = 0;
            BigImageWidth = 0;
            foreach (var b in images)
            {
                BigImageHeight += b.PixelHeight;
                if (BigImageWidth < b.PixelWidth)
                    BigImageWidth = b.PixelWidth;
            }
            MaxCropHeight = (int)Math.Round((BigImageWidth * ContentHeight) / ContentWidth);
            CropHeight = MaxCropHeight;
        }

        public void AddUIElement(UIElement uielement) => Page.Child.Children.Add(uielement);
        public void RemoveUIElement(UIElement uielement) => Page.Child.Children.Remove(uielement);

        /// <summary>
        /// Calculates the vertical pixel position relative to the page
        /// </summary>
        /// <param name="percentage">the vertical position percentage relative to the page (with margin)</param>
        public int CalculatePixelPosY(double percentage)
        {
            double scalingY = CropableImage.ActualCropHeight / ContentHeight;
            double scalingX = CropableImage.ActualCropWidth / ContentWidth;
            var pageY = (int)Math.Round((percentage * PageHeight - ContentPadding.Top) * Math.Max(scalingX, scalingY));

            if (pageY > CropHeight) return CropHeight;
            else if (pageY < 0) return 0;
            else return pageY;
        }

        /// <summary>
        /// Calculates the optimal crop height (with the MaxCropHeight) relative to the page
        /// </summary>
        public double CalculateOptimalCropHeight()
        {
            double scalingY = ContentHeight / CropableImage.ActualCropHeight;
            double scalingX = ContentWidth / CropableImage.ActualCropWidth;
            return MaxCropHeight * Math.Min(scalingX, scalingY);
        }
    }
}
