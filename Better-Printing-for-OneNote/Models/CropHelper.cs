using Better_Printing_for_OneNote.AdditionalClasses;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        private List<Crop> CurrentCropHeights;
        private List<SignatureAdded> CurrentSignatures = new List<SignatureAdded>();
        private ObservableCollection<PageModel> Pages = new ObservableCollection<PageModel>();
        private List<DocumentChange> UndoChangeList = new List<DocumentChange>();
        private List<DocumentChange> RedoChangeList = new List<DocumentChange>();

        private int MaxCropHeight;
        private double DocumentHeight;
        private double DocumentWidth;
        private double ContentHeight;
        private double ContentWidth;
        private Thickness Padding;
        private BitmapSource[] Images;

        public CropHelper(BitmapSource[] images)
        {
            Images = images;
        }

        public void InitializePages()
        {
            Pages.Clear();
            RedoChangeList.Clear();
            UndoChangeList.Clear();


            var bigImageHeight = 0;
            foreach (var b in Images)
                bigImageHeight += b.PixelHeight;


            CurrentCropHeights = new List<Crop>();
            var positionY = 0;
            var first = true;
            while (positionY + MaxCropHeight < bigImageHeight)
            {
                var page = CreateNewPage(null);
                page.CropShift = positionY;
                positionY += page.CropHeight;
                CurrentCropHeights.Add(new Crop() { Value = page.CropHeight });
                Pages.Add(page);
                if (first)
                {
                    MaxCropHeight = page.MaxCropHeight;
                    first = false;
                }
            }

            // add last crop
            var lastPage = CreateNewPage(null);
            lastPage.CropShift = positionY;
            lastPage.CropHeight = bigImageHeight - positionY;
            CurrentCropHeights.Add(new Crop() { Value = bigImageHeight - positionY });
            Pages.Add(lastPage);


            var document = new FixedDocument();
            foreach (var p in Pages)
                document.Pages.Add(p.Page);
            Document = document;
        }

        private PageModel CreateNewPage(ArrayList skips)
        {
            var page = new PageModel(Images, skips, DocumentHeight, DocumentWidth, ContentHeight, ContentWidth, Padding);
            return page;
        }

        public void UndoChange()
        {
            if (UndoChangeList.Count > 0)
            {
                if (UndoChangeList[UndoChangeList.Count - 1] is SignatureAdded signatureAdded)
                {
                    CurrentSignatures.Remove(signatureAdded);
                    RedoChangeList.Add(signatureAdded);
                    UpdatePages();
                }
                else if (UndoChangeList[UndoChangeList.Count - 1] is CropsAndSkips cropsAndSkips)
                {
                    RedoChangeList.Add(new CropsAndSkips(CurrentCropHeights));
                    CurrentCropHeights = cropsAndSkips.Crops;
                    UpdatePages();
                }

                UndoChangeList.RemoveAt(UndoChangeList.Count - 1);
            }
        }

        public void RedoChange()
        {
            if (RedoChangeList.Count > 0)
            {
                if (RedoChangeList[RedoChangeList.Count - 1] is SignatureAdded signatureAdded)
                {
                    CurrentSignatures.Add(signatureAdded);
                    UndoChangeList.Add(signatureAdded);
                    UpdatePages();
                }
                else if (RedoChangeList[RedoChangeList.Count - 1] is CropsAndSkips cropsAndSkips)
                {
                    UndoChangeList.Add(new CropsAndSkips(CurrentCropHeights));
                    CurrentCropHeights = cropsAndSkips.Crops;
                    UpdatePages();
                }

                RedoChangeList.RemoveAt(RedoChangeList.Count - 1);
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
                            newCropHeights.Add(new Skip() { Value = CurrentCropHeights[cropIndex].Value });
                        else
                            newCropHeights.Add(CurrentCropHeights[cropIndex]);
                        page++;
                    }
                    cropIndex++;
                }

                UndoChangeList.Add(new CropsAndSkips(CurrentCropHeights));
                CurrentCropHeights = newCropHeights;
                UpdatePages();
            }
        }

        /// <summary>
        /// Splits the desired page at the desired crop height
        /// </summary>
        /// <param name="pageToEdit">the page to edit</param>
        /// <param name="splitAtPercentage">the position of the split relative to the whole Page (FixedContent) in percent</param>
        public void SplitPageAt_Reset(int pageToEdit, double splitAtPercentage)
        {
            var cropHeight = Pages[pageToEdit].CalculateSplitHeight(splitAtPercentage);
            if (cropHeight != Pages[pageToEdit].CropHeight)
            {
                // addopt all Siteheights before the page to edit, reset all pages after the edited page (maintains Skips)
                var imageHeight = Pages[0].BigImageHeight;
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

                UndoChangeList.Add(new CropsAndSkips(CurrentCropHeights));
                CurrentCropHeights = newCropHeights;
                UpdatePages();
            }
        }

        public void SplitPageAt(int pageToEdit, double splitAtPercentage)
        {
            var cropHeight = Pages[pageToEdit].CalculateSplitHeight(splitAtPercentage);

            if (cropHeight != Pages[pageToEdit].CropHeight)
            {
                var newCropHeights = new List<Crop>();
                var pageIndex = 0;
                var overflowHeight = 0;
                for (int cropIndex = 0; cropIndex < CurrentCropHeights.Count; cropIndex++)
                {
                    var crop = CurrentCropHeights[cropIndex];
                    if (crop.GetType() == typeof(Skip))
                        newCropHeights.Add(crop);
                    else
                    {
                        if (pageIndex == pageToEdit)
                        {
                            overflowHeight = crop.Value - cropHeight;

                            newCropHeights.Add(new Crop() { Value = cropHeight });
                            if (Pages.Count < pageToEdit + 2)
                                // no page after the page to edit
                                newCropHeights.Add(new Crop() { Value = overflowHeight });
                        }
                        else
                        {
                            if (pageIndex == pageToEdit + 1)
                                // the page after the page to edit
                                newCropHeights.Add(new Crop() { Value = crop.Value + overflowHeight });
                            else
                                newCropHeights.Add(crop);
                        }
                        pageIndex++;
                    }
                }

                UndoChangeList.Add(new CropsAndSkips(CurrentCropHeights));
                CurrentCropHeights = newCropHeights;
                UpdatePages();
            }
        }

        /// <summary>
        /// Updates the pages with the "CurrentCropHeights"
        /// </summary>
        private void UpdatePages()
        {
            // remove all UIElements from all pages
            foreach (var pageModel in Pages)
            {
                FixedPage fp = pageModel.Page.Child;
                fp.Children.Clear();
            }

            // remove all pages
            Pages.Clear();

            // divide real crops and skips
            var skips = new List<WpfCropableImageControl.Skip>();
            var crops = new List<Crop>();
            var positionY = 0;
            foreach (var crop in CurrentCropHeights)
            {
                if (crop.GetType() == typeof(Skip))
                    skips.Add(new WpfCropableImageControl.Skip() { SkipType = WpfCropableImageControl.SkipType.Y, SkipStart = positionY, SkipEnd = positionY + crop.Value });
                else
                    crops.Add(crop);

                positionY += crop.Value;
            }
            var skipsArrayList = new ArrayList(skips);

            // process pages/real crops
            var shift = 0;
            foreach (var crop in crops)
            {
                var page = CreateNewPage(skipsArrayList);
                page.CropShift = shift;
                page.CropHeight = crop.Value;
                shift += crop.Value;
                Pages.Add(page);
            }

            // create document
            var document = new FixedDocument();
            foreach (var page in Pages)
                document.Pages.Add(page.Page);
            Document = document;

            // readd signatures
            for (int i = 0; i < CurrentSignatures.Count; i++)
            {
                var signature = CurrentSignatures[i];

                // remove signatures with empty or only whitespace text
                if (string.IsNullOrWhiteSpace(signature.Text.Text))
                {
                    CurrentSignatures.RemoveAt(i);
                    UndoChangeList.Remove(signature);
                    i--;
                    continue;
                }

                foreach (var page in Pages)
                {
                    TextBox tb = CreateSignatureTextBox(signature.Text, new Thickness(signature.X, signature.Y, 0, 0));
                    page.AddUIElement(tb);
                    page.Page.Child.UpdateLayout();
                }
            }
        }


        /// <summary>
        /// Add an editable TextBox to all pages of the document
        /// </summary>
        public void AddSignatureTb(double x, double y)
        {
            // approximately center textbox on coordinates
            x -= 2;
            y -= 8;

            BindableText bindableText = new BindableText();
            SignatureAdded signatureAdded = new SignatureAdded(bindableText, x, y);
            UndoChangeList.Add(signatureAdded);
            CurrentSignatures.Add(signatureAdded);

            foreach (var page in Pages)
            {
                TextBox tb = CreateSignatureTextBox(bindableText, new Thickness(x, y, 0, 0));
                page.AddUIElement(tb);
            }
        }

        private TextBox CreateSignatureTextBox(BindableText text, Thickness margin)
        {
            TextBox tb = new TextBox { AcceptsReturn = true, Background = Brushes.White, BorderThickness = new Thickness(0), Margin = margin };
            tb.Background = Brushes.Transparent;
            Binding binding = new Binding(nameof(text.Text));
            binding.Mode = BindingMode.TwoWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = text;
            tb.SetBinding(TextBox.TextProperty, binding);

            return tb;
        }

        /// <summary>
        /// Updates the format of all pages
        /// </summary>
        /// <param name="documentHeight">the height of the whole page</param>
        /// <param name="documentWidth">the width of the whole page</param>
        /// <param name="contentHeight">the height of the content on the page (without the printers padding)</param>
        /// <param name="contentWidth">the width of the content on the page (without the printers padding)</param>
        /// <param name="padding">the printers padding</param>
        public void UpdateFormat(double documentHeight, double documentWidth, double contentHeight, double contentWidth, Thickness padding)
        {
            if (DocumentHeight != documentHeight || DocumentWidth != documentWidth || contentHeight != ContentHeight || contentWidth != ContentWidth || Padding != padding)
            {
                DocumentHeight = documentHeight;
                DocumentWidth = documentWidth;
                ContentHeight = contentHeight;
                ContentWidth = contentWidth;
                Padding = padding;
                InitializePages();
            }
        }
    }

    class DocumentChange { }

    class CropsAndSkips : DocumentChange
    {
        public List<Crop> Crops { get; set; }

        public CropsAndSkips(List<Crop> crops)
        {
            Crops = crops;
        }
    }

    class Crop
    {
        public int Value { get; set; }
    }

    class Skip : Crop { }

    class SignatureAdded : DocumentChange
    {
        public BindableText Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public SignatureAdded(BindableText text, double x, double y)
        {
            Text = text;
            X = x;
            Y = y;
        }
    }

    class BindableText : NotifyBase
    {
        private string text = string.Empty;
        public string Text
        {
            get => text; set
            {
                if (value != text)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }
    }
}