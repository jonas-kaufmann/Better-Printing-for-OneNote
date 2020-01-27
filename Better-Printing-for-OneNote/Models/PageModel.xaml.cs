using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfCropableImageControl;

namespace Better_Printing_for_OneNote.Models
{
    public class PageModel : INotifyPropertyChanged
    {
        public PageContent Page { get; private set; }
        private CropableImage CropableImage;
        public int MaxCropHeight { get; private set; }

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
                if(_bigImageHeight != value)
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

        #region Properties

        private ArrayList _skips;
        public ArrayList Skips
        {
            get
            {
                return _skips;
            }
            set
            {
                if (value != _skips)
                {
                    _skips = value;
                    OnPropertyChanged("Skips");
                }
            }
        }

        private double _documentHeight;
        public double PageHeight
        {
            get
            {
                return _documentHeight;
            }
            set
            {
                if (value != _documentHeight)
                {
                    _documentHeight = value;
                    OnPropertyChanged("DocumentHeight");
                }
            }
        }

        private double _documentWidth;
        public double PageWidth
        {
            get
            {
                return _documentWidth;
            }
            set
            {
                if (value != _documentWidth)
                {
                    _documentWidth = value;
                    OnPropertyChanged("DocumentWidth");
                }
            }
        }

        private Thickness _contentPadding;
        public Thickness ContentPadding
        {
            get
            {
                return _contentPadding;
            }
            set
            {
                if (value != _contentPadding)
                {
                    _contentPadding = value;
                    OnPropertyChanged("Padding");
                }
            }
        }

        private double _contentHeight;
        public double ContentHeight
        {
            get
            {
                return _contentHeight;
            }
            set
            {
                if (value != _contentHeight)
                {
                    _contentHeight = value;
                    OnPropertyChanged("ContentHeight");
                }
            }
        }

        private double _contentWidth;
        public double ContentWidth
        {
            get
            {
                return _contentWidth;
            }
            set
            {
                if (value != _contentWidth)
                {
                    _contentWidth = value;
                    OnPropertyChanged("ContentWidth");
                }
            }
        }

        private BitmapSource[] _images;
        public BitmapSource[] Images
        {
            get
            {
                return _images;
            }
            set
            {
                if (value != _images)
                {
                    _images = value;
                    OnPropertyChanged("Images");
                }
            }
        }

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
                    OnPropertyChanged("CropHeight");
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
                    OnPropertyChanged("CropShift");
                }
            }
        }

        #endregion

        public PageModel(BitmapSource[] images, ArrayList skips, double pageHeight, double pageWidth, double contentHeight, double contentWidth, Thickness contentPadding)
        {
            Images = images;
            Skips = skips;
            PageHeight = pageHeight;
            PageWidth = pageWidth;
            ContentHeight = contentHeight;
            ContentWidth = contentWidth;
            ContentPadding = contentPadding;

            // initialize component
            Page = Application.LoadComponent(new Uri("models/PageModel.xaml", UriKind.Relative)) as PageContent;
            (Page.Child as FixedPage).DataContext = this;
            CropableImage = (CropableImage)Page.Child.Children[0];


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

        /// <summary>
        /// Calculates the height to split the image at to match the SplitAtPercentage
        /// </summary>
        /// <param name="splitAtPercentage">the position relative to the whole page</param>
        /// <returns>the height to split the image at</returns>
        public int CalculateSplitHeight(double splitAtPercentage)
        {
            double scalingY = CropableImage.ActualCropHeight / ContentHeight;
            double scalingX = CropableImage.ActualCropWidth / ContentWidth;
            double scaling = Math.Max(scalingX, scalingY);

            var splitHeight = (int)Math.Round((splitAtPercentage * PageHeight - ContentPadding.Top) * scaling);
            if (splitHeight > CropHeight) return CropHeight;
            else if (splitHeight < 0) return 0;
            else return splitHeight;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }
    }
}
