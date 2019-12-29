using Better_Printing_for_OneNote.AdditionalClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Better_Printing_for_OneNote.Models
{
    class CropHelper : NotifyBase
    {
        private FixedDocument _document;
        public FixedDocument Document
        {
            get
            {
                return _document;
            }
            set
            {
                _document = value;
                OnPropertyChanged("Document");
            }
        }

        #region PageNumbers

        private bool _pageNumbersEnabled;
        public bool PageNumbersEnabled
        {
            get
            {
                return _pageNumbersEnabled;
            }
            set
            {
                if (value != _pageNumbersEnabled)
                {
                    foreach (var page in Pages)
                        page.PageNumbersEnabled = value;
                    _pageNumbersEnabled = value;
                    OnPropertyChanged("PageNumbersEnabled");
                }
            }
        }

        #endregion

        #region Signature

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
                    foreach (var page in Pages)
                        page.SignatureEnabled = value;
                    _signatureEnabled = value;
                    OnPropertyChanged("SignatureEnabled");
                }
            }
        }

        private string _signature;
        public string Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                if (value != _signature)
                {
                    foreach (var page in Pages)
                        page.Signature = value;
                    _signature = value;
                    OnPropertyChanged("Signature");
                }
            }
        }

        #endregion

        private List<Crop> _currentCropHeights;
        private List<Crop> CurrentCropHeights
        {
            get
            {
                return _currentCropHeights;
            }
            set
            {
                if (value != null)
                {
                    _currentCropHeights = value;
                    UpdatePages();
                }
                else
                {
                    _currentCropHeights = new List<Crop>();
                    var positionY = 0;
                    while (positionY < Height)
                    {
                        var page = CreateNewPage();
                        page.CropShift = positionY;
                        positionY += page.CropHeight;
                        _currentCropHeights.Add(new Crop() { Value = page.CropHeight });
                        Pages.Add(page);
                        MaxCropHeight = page.MaxCropHeight;
                    }

                    // set the document
                    var document = new FixedDocument();
                    foreach (var page in Pages)
                        document.Pages.Add(page.Page);
                    Document = document;
                }
            }
        }

        private ObservableCollection<PageModel> Pages = new ObservableCollection<PageModel>();
        private List<List<Crop>> UndoChangeList = new List<List<Crop>>();
        private List<List<Crop>> RedoChangeList = new List<List<Crop>>();
        private int Height;
        private WriteableBitmap Image;
        private int MaxCropHeight;

        /// <summary>
        /// Initializes the first crops
        /// </summary>
        /// <param name="image"></param>
        public CropHelper(WriteableBitmap image)
        {
            Image = image;
            Height = Image.PixelHeight;

            // register CollectionChanged EventHandler to update Sitenumbers
            Pages.CollectionChanged += (sender, e) => UpdatePageNumbers();

            // set first crops (they are initialized in the property)
            CurrentCropHeights = null;
        }
        
        /// <summary>
        /// Creates new PageModel and copies all needed values (Signature, PageNumbers)
        /// </summary>
        /// <returns>the page</returns>
        private PageModel CreateNewPage()
        {
            var page = new PageModel(Image);
            page.SignatureEnabled = SignatureEnabled;
            page.Signature = Signature;
            page.PageNumbersEnabled = PageNumbersEnabled;
            return page;
        }

        /// <summary>
        /// Undos the last change and adds it to the redo list
        /// </summary>
        public void UndoChange()
        {
            if (UndoChangeList.Count > 0)
            {
                var lastChange = UndoChangeList[UndoChangeList.Count - 1];
                RedoChangeList.Add(CurrentCropHeights);
                UndoChangeList.RemoveAt(UndoChangeList.Count - 1);
                CurrentCropHeights = lastChange;
            }
        }

        /// <summary>
        /// Redos the last undo and adds it to the undo list
        /// </summary>
        public void RedoChange()
        {
            if (RedoChangeList.Count > 0)
            {
                var lastChange = RedoChangeList[RedoChangeList.Count - 1];
                UndoChangeList.Add(CurrentCropHeights);
                RedoChangeList.RemoveAt(RedoChangeList.Count - 1);
                CurrentCropHeights = lastChange;
            }
        }

        /// <summary>
        /// Skips the desired page (all other pages remain the same) (if there is more than one page left)
        /// </summary>
        /// <param name="pageToSkip">the page to skip</param>
        public void SkipPage(int pageToSkip)
        {
            if (Pages.Count > 1)
            {
                var newCropHeights = new List<Crop>();
                var page = 0;
                var cropIndex = 0;

                while (cropIndex < CurrentCropHeights.Count)
                {
                    if (CurrentCropHeights[cropIndex].GetType() == typeof(Skip))
                        newCropHeights.Add(CurrentCropHeights[cropIndex]);
                    else
                    {
                        if (page == pageToSkip)
                            // add skip
                            newCropHeights.Add(new Skip() { Value = CurrentCropHeights[cropIndex].Value });
                        else
                            // copy all values before and after
                            newCropHeights.Add(CurrentCropHeights[cropIndex]);
                        page++;
                    }
                    cropIndex++;
                }

                UndoChangeList.Add(CurrentCropHeights);
                CurrentCropHeights = newCropHeights;
            }
        }

        /// <summary>
        /// Splits the desired page at the desired crop height
        /// </summary>
        /// <param name="pageToEdit">the page to edit</param>
        /// <param name="splitAtPercentage">the position of the split relative to the whole Page (FixedContent) in percent</param>
        public void SplitPageAt(int pageToEdit, double splitAtPercentage)
        {
            var cropHeight = Pages[pageToEdit].CalculateSplitHeight(splitAtPercentage);
            if (cropHeight > 0)
            {
                // addopt all Siteheights before the page to edit, reset all pages after the edited page (maintains Skips)
                var imageHeight = Height;
                var cropIndex = 0;
                var pageIndex = 0;
                var newCropHeights = new List<Crop>();
                while (imageHeight > 0)
                {
                    if (cropIndex < CurrentCropHeights.Count && CurrentCropHeights[cropIndex].GetType() == typeof(Skip))
                    {
                        newCropHeights.Add(CurrentCropHeights[cropIndex]);
                        imageHeight -= CurrentCropHeights[cropIndex].Value;
                    }
                    else
                    {
                        if (pageIndex < pageToEdit)
                        {
                            newCropHeights.Add(CurrentCropHeights[cropIndex]);
                            imageHeight -= CurrentCropHeights[cropIndex].Value;
                        }
                        else if (pageIndex > pageToEdit)
                        {
                            if (imageHeight < MaxCropHeight)
                            {
                                newCropHeights.Add(new Crop() { Value = imageHeight });
                                imageHeight = 0;
                            }
                            else
                            {
                                newCropHeights.Add(new Crop() { Value = MaxCropHeight });
                                imageHeight -= MaxCropHeight;
                            }
                        }
                        else
                        {
                            newCropHeights.Add(new Crop() { Value = cropHeight });
                            imageHeight -= cropHeight;
                        }
                        pageIndex++;
                    }
                    cropIndex++;
                }

                UndoChangeList.Add(CurrentCropHeights);
                CurrentCropHeights = newCropHeights;
            }
        }

        /// <summary>
        /// Updates the pages with the "CurrentCropHeights"
        /// </summary>
        private void UpdatePages()
        {
            // remove all pages
            Pages.Clear();

            // add Pages
            var shift = 0;
            for (int i = 0; i < CurrentCropHeights.Count; i++)
            {
                if (CurrentCropHeights[i].GetType() == typeof(Skip))
                    shift += CurrentCropHeights[i].Value;
                else
                {
                    var page = CreateNewPage();
                    page.CropShift = shift;
                    page.CropHeight = CurrentCropHeights[i].Value;
                    shift += page.CropHeight;
                    Pages.Add(page);
                }
            }

            // create new FixedDocument
            var document = new FixedDocument();
            foreach (var page in Pages)
                document.Pages.Add(page.Page);
            Document = document;
        }

        /// <summary>
        /// Goes through all pages and updates the pagenumbers
        /// </summary>
        private void UpdatePageNumbers()
        {
            var pageCount = Pages.Count;
            for (int i = 0; i < pageCount; i++)
                Pages[i].PageNumber = $"{i + 1}/{pageCount}";
        }
    }

    class Crop
    {
        public int Value;
    }
    class Skip : Crop { }
}
