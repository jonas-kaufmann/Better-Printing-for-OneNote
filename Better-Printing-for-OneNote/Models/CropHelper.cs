using Better_Printing_for_OneNote.AdditionalClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfCropableImageControl;
using static Better_Printing_for_OneNote.Models.SignatureChange;

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
                OnPropertyChanged(nameof(Document));
            }
        }

        private ObservableCollection<PageModel> Pages = new ObservableCollection<PageModel>();
        private CropsAndSkips CurrentCropsAndSkips;
        public readonly List<SignatureChange> CurrentSignatures = new List<SignatureChange>();
        private readonly List<DocumentChange> UndoChangeList = new List<DocumentChange>();
        private readonly List<DocumentChange> RedoChangeList = new List<DocumentChange>();

        private int MaxCropHeight;
        private double PageHeight;
        private double PageWidth;
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
            RedoChangeList.Clear();
            UndoChangeList.Clear();
            CurrentSignatures.Clear();


            var bigImageHeight = 0;
            foreach (var b in Images)
                bigImageHeight += b.PixelHeight;


            CurrentCropsAndSkips = new CropsAndSkips(new List<int>(), new List<Skip>());
            var positionY = 0;
            var first = true;
            while (positionY + MaxCropHeight < bigImageHeight)
            {
                var page = CreateNewPage(null);
                page.CropShift = positionY;
                positionY += page.CropHeight;
                CurrentCropsAndSkips.Crops.Add(page.CropHeight);
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
            CurrentCropsAndSkips.Crops.Add(bigImageHeight - positionY);
            Pages.Add(lastPage);

            var document = new FixedDocument();
            foreach (var p in Pages)
                document.Pages.Add(p.Page);
            Document = document;
        }

        public void UndoChange()
        {
            if (UndoChangeList.Count > 0)
            {
                if (UndoChangeList[UndoChangeList.Count - 1] is CropsAndSkips cropsAndSkips)
                {
                    CurrentCropsAndSkips = cropsAndSkips;
                    UpdatePages();
                }
                else if (UndoChangeList[UndoChangeList.Count - 1] is SignatureChanges signatureChanges)
                {
                    ProcessSignatureChanges(signatureChanges, true);
                }

                RedoChangeList.Add(UndoChangeList[UndoChangeList.Count - 1]);
                UndoChangeList.RemoveAt(UndoChangeList.Count - 1);
            }
        }

        public void RedoChange()
        {
            if (RedoChangeList.Count > 0)
            {
                if (RedoChangeList[RedoChangeList.Count - 1] is CropsAndSkips cropsAndSkips)
                {
                    UndoChangeList.Add(CurrentCropsAndSkips);
                    CurrentCropsAndSkips = cropsAndSkips;
                    UpdatePages();
                }
                else if (RedoChangeList[RedoChangeList.Count - 1] is SignatureChanges signatureChanges)
                {
                    ProcessSignatureChanges(signatureChanges, false);
                }

                UndoChangeList.Add(RedoChangeList[RedoChangeList.Count - 1]);
                RedoChangeList.RemoveAt(RedoChangeList.Count - 1);
            }
        }

        private void ProcessSignatureChanges(SignatureChanges signatureChanges, bool inverted)
        {
            List<SignatureChange> added = signatureChanges.Added;
            List<SignatureChange> removed = signatureChanges.Removed;
            if (inverted)
            {
                added = signatureChanges.Removed;
                removed = signatureChanges.Added;
            }

            foreach (var signatureChange in added)
            {
                CurrentSignatures.Add(signatureChange);

                foreach (var uiElement in signatureChange.UIElements)
                    Pages[uiElement.Item1].AddUIElement(uiElement.Item2);
            }

            foreach (var signatureChange in removed)
            {
                CurrentSignatures.Remove(signatureChange);

                foreach (var uiElement in signatureChange.UIElements)
                    Pages[uiElement.Item1].RemoveUIElement(uiElement.Item2);
            }
        }

        public void SkipPage(int pageToSkip)
        {
            if (Pages.Count > 1)
            {
                var newCropsAndSkips = CurrentCropsAndSkips.Copy();

                newCropsAndSkips.Crops.RemoveAt(pageToSkip);
                var skipStart = CalculatePixelPosY_WholeImage(0, pageToSkip);
                var skipEnd = CalculatePixelPosY_WholeImage(CurrentCropsAndSkips.Crops[pageToSkip], pageToSkip);
                newCropsAndSkips.InsertSkip(new Skip() { SkipType = SkipType.Y, SkipStart = skipStart, SkipEnd = skipEnd });

                UndoChangeList.Add(CurrentCropsAndSkips);
                CurrentCropsAndSkips = newCropsAndSkips;
                UpdatePages();
            }
        }

        public void MergePages(int from, int to)
        {
            var newCropsAndSkips = CurrentCropsAndSkips.Copy();

            var newCropHeight = CurrentCropsAndSkips.Crops[from];
            for (int i = to; i > from; i--)
            {
                newCropHeight += CurrentCropsAndSkips.Crops[i];
                newCropsAndSkips.Crops.RemoveAt(i);
            }
            newCropsAndSkips.Crops[from] = newCropHeight;

            UndoChangeList.Add(CurrentCropsAndSkips);
            CurrentCropsAndSkips = newCropsAndSkips;
            UpdatePages();
        }

        /// <param name="percentage">the vertical position percentage relative to the page (with margin)</param>
        public void SplitPageAt(int pageToEdit, double percentage)
        {
            var splitAt = Pages[pageToEdit].CalculatePixelPosY(percentage);

            if (splitAt != Pages[pageToEdit].CropHeight)
            {
                var newCropsAndSkips = CurrentCropsAndSkips.Copy();

                if (newCropsAndSkips.Crops.Count - 1 > pageToEdit)
                    newCropsAndSkips.Crops[pageToEdit + 1] = CurrentCropsAndSkips.Crops[pageToEdit + 1] + (CurrentCropsAndSkips.Crops[pageToEdit] - splitAt);
                else
                    newCropsAndSkips.Crops.Add(CurrentCropsAndSkips.Crops[pageToEdit] - splitAt);
                newCropsAndSkips.Crops[pageToEdit] = splitAt;

                UndoChangeList.Add(CurrentCropsAndSkips);
                CurrentCropsAndSkips = newCropsAndSkips;
                UpdatePages();
            }
        }

        public void DeleteArea(int pageToEdit, double _start, double _end)
        {
            var start = CalculatePixelPosY_WholeImage(_start, pageToEdit);
            var end = CalculatePixelPosY_WholeImage(_end, pageToEdit);

            var newCropsAndSkips = CurrentCropsAndSkips.Copy();
            newCropsAndSkips.Crops[pageToEdit] = newCropsAndSkips.Crops[pageToEdit] - (Pages[pageToEdit].CalculatePixelPosY(_end) - Pages[pageToEdit].CalculatePixelPosY(_start));
            newCropsAndSkips.InsertSkip(new Skip() { SkipType = SkipType.Y, SkipStart = start, SkipEnd = end });

            UndoChangeList.Add(CurrentCropsAndSkips);
            CurrentCropsAndSkips = newCropsAndSkips;
            UpdatePages();
        }

        /// <summary>
        /// calculates the vertical pixel position relative to the whole image (with skips)
        /// </summary>
        /// <param name="percentage">the vertical position percentage relative to the page (with margin)</param>
        public int CalculatePixelPosY_WholeImage(double percentage, int page)
        {
            // add the page heights before the page
            var posY = 0;
            for (int i = 0; i < page; i++)
                posY += Pages[i].CropHeight;

            posY += Pages[page].CalculatePixelPosY(percentage);

            // add the skipped area before the page to get the actual height
            if (CurrentCropsAndSkips.Skips != null)
            {
                var relevantSkipHeight = 0;
                foreach (Skip s in CurrentCropsAndSkips.Skips)
                    if (s.SkipStart <= posY + relevantSkipHeight)
                        relevantSkipHeight += s.SkipHeight;
                posY += relevantSkipHeight;
            }
            return posY;
        }

        public double GetOptimalHeight(int pageIndex)
        {
            return Pages[pageIndex].CalculateOptimalCropHeight();
        }

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

            // process crops n skips
            var skipsArrayList = new ArrayList(CurrentCropsAndSkips.Skips);
            var shiftY = 0;
            foreach (var cropHeight in CurrentCropsAndSkips.Crops)
            {
                var page = CreateNewPage(skipsArrayList);
                page.CropShift = shiftY;
                page.CropHeight = cropHeight;
                shiftY += cropHeight;
                Pages.Add(page);
            }

            // create document
            var document = new FixedDocument();
            foreach (var page in Pages)
                document.Pages.Add(page.Page);
            Document = document;


            // add signatures
            for (int i = 0; i < CurrentSignatures.Count; i++)
            {
                var signature = CurrentSignatures[i];
                // remove empty signatures
                if (string.IsNullOrWhiteSpace(signature.Text.Text))
                {
                    CurrentSignatures.RemoveAt(i);
                    i--;
                    continue;
                }

                signature.UIElements.Clear();

                AddSignatureToPages(signature);
            }
        }

        /// <summary>
        /// Add an editable TextBox to all pages of the document (returns specified textbox, for focusing)
        /// </summary>
        public TextBox InitialAddSignatureTb(double x, double y, int textboxToReturnPageIndex)
        {
            // match x and y coordinates to height/width of the content of the page
            x = x - Padding.Left;
            y = y - Padding.Top;

            if (x < 0 || x > ContentWidth)
                return null;
            if (y < 0 || y > ContentHeight)
                return null;

            // approximately center textbox on coordinates
            x -= 2;
            y -= 8;

            var bindableText = new BindableText();
            var signatureChange = new SignatureChange(bindableText, x, y);

            return AddSignatureNoCopy(signatureChange, textboxToReturnPageIndex);
        }

        private TextBox AddSignatureToPages(SignatureChange signatureChange, int textboxToReturnPageIndex = -1)
        {
            TextBox textbox = null;
            for (int i = 0; i < Pages.Count; i++)
            {
                TextBox tb = CreateSignatureTextBox(signatureChange.Text, new Thickness(signatureChange.X, signatureChange.Y, 0, 0), i + 1, Pages.Count);
                Pages[i].AddUIElement(tb);
                signatureChange.UIElements.Add((i, tb));
                if (i == textboxToReturnPageIndex)
                    textbox = tb;
            }

            return textbox;
        }

        private TextBox AddSignatureNoCopy(SignatureChange signatureChange, int textboxToReturnPageIndex = -1)
        {
            CurrentSignatures.Add(signatureChange);

            var changes = new SignatureChanges();
            changes.Added.Add(signatureChange);
            UndoChangeList.Add(changes);

            return AddSignatureToPages(signatureChange, textboxToReturnPageIndex);
        }

        public void AddSignaturesAndCopy(List<SignatureChange> signatures)
        {
            var changes = new SignatureChanges();
            foreach (var signature in signatures)
                if (!CurrentSignatures.Exists(s => s is SignatureChange sc && sc.Equals(signature)))
                {
                    var copy = signature.Copy();

                    CurrentSignatures.Add(copy);
                    AddSignatureToPages(copy);

                    changes.Added.Add(copy);
                }

            UndoChangeList.Add(changes);
        }

        private TextBox CreateSignatureTextBox(BindableText text, Thickness margin, int page, int of)
        {
            TextBox tb = new TextBox { AcceptsReturn = true, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Margin = margin };

            Binding CreateBinding()
            {
                var binding = new Binding(nameof(text.Text));
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                binding.Source = text;
                return binding;
            }

            var rawTextBinding = CreateBinding();

            var pageCountBinding = CreateBinding();
            pageCountBinding.Converter = new PageCountConverter(page, of);

            tb.GotKeyboardFocus += (sender, e) =>
                tb.SetBinding(TextBox.TextProperty, rawTextBinding);
            tb.LostKeyboardFocus += (sender, e) =>
                tb.SetBinding(TextBox.TextProperty, pageCountBinding);

            tb.SetBinding(TextBox.TextProperty, pageCountBinding);

            return tb;
        }

        public void ClearSignatures()
        {
            var changes = new SignatureChanges();

            while (CurrentSignatures.Count > 0)
            {
                var index = CurrentSignatures.Count - 1;
                var currentSignature = CurrentSignatures[index];

                changes.Removed.Add(currentSignature);

                CurrentSignatures.RemoveAt(index);

                for (var i = 0; i < Pages.Count; i++)
                {
                    foreach (var uiElement in currentSignature.UIElements)
                        Pages[i].RemoveUIElement(uiElement.Item2);
                }
            }

            UndoChangeList.Add(changes);
        }

        public void UpdateFormat(double pageHeight, double pageWidth, double contentHeight, double contentWidth, Thickness padding)
        {
            if (PageHeight != pageHeight || PageWidth != pageWidth || contentHeight != ContentHeight || contentWidth != ContentWidth || Padding != padding)
            {
                PageHeight = pageHeight;
                PageWidth = pageWidth;
                ContentHeight = contentHeight;
                ContentWidth = contentWidth;
                Padding = padding;
                if (Pages.Count > 0)
                    UpdatePages();
                else
                    InitializePages();
            }
        }

        private PageModel CreateNewPage(ArrayList skips)
        {
            return new PageModel(Images, skips, PageHeight, PageWidth, ContentHeight, ContentWidth, Padding);
        }
    }

    public class PageCountConverter : IValueConverter
    {
        public const string PAGE_COUNT_TOKEN = "${page}";
        public const string OF_PAGES_TOKEN = "${of}";
        private int Page;
        private int Of;

        public PageCountConverter(int page, int of)
        {
            Page = page;
            Of = of;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var builder = new StringBuilder("" + value);
            builder.Replace(PAGE_COUNT_TOKEN, "" + Page);
            builder.Replace(OF_PAGES_TOKEN, "" + Of);
            return builder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class DocumentChange { }

    class CropsAndSkips : DocumentChange
    {
        public List<int> Crops { get; set; }
        private List<Skip> _skips;
        public ReadOnlyCollection<Skip> Skips
        {
            get
            {
                return new ReadOnlyCollection<Skip>(_skips);
            }
        }

        public CropsAndSkips(List<int> crops, List<Skip> skips)
        {
            Crops = crops;
            _skips = skips;
        }

        public void InsertSkip(Skip skip)
        {
            for (int i = 0; i < Skips.Count; i++)
            {
                if (skip.SkipEnd < Skips[i].SkipStart)
                {
                    // right position for the skip
                    _skips.Insert(i, skip);
                    return;
                }
                else
                {
                    if ((skip.SkipStart <= Skips[i].SkipStart && skip.SkipEnd >= Skips[i].SkipEnd)
                        || (skip.SkipStart >= Skips[i].SkipStart && skip.SkipEnd >= Skips[i].SkipEnd && skip.SkipStart <= Skips[i].SkipEnd)
                        || (skip.SkipEnd <= Skips[i].SkipEnd))
                    {
                        // skips are overlapping
                        skip.SkipStart = Math.Min(skip.SkipStart, Skips[i].SkipStart);
                        skip.SkipEnd = Math.Max(skip.SkipEnd, Skips[i].SkipEnd);
                        _skips.RemoveAt(i);
                        i--;
                    }
                }
            }

            _skips.Add(skip);
        }

        public CropsAndSkips Copy()
        {
            var crops = new List<int>();
            foreach (var c in Crops)
                crops.Add(c);

            var skips = new List<Skip>();
            foreach (var s in Skips)
                skips.Add(s);

            return new CropsAndSkips(crops, skips);
        }
    }

    public class SignatureChanges : DocumentChange
    {
        public List<SignatureChange> Added { get; set; } = new List<SignatureChange>();
        public List<SignatureChange> Removed { get; set; } = new List<SignatureChange>();
    }

    public class SignatureChange
    {
        public List<(int, UIElement)> UIElements { get; set; } = new List<(int, UIElement)>();
        public BindableText Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public SignatureChange(BindableText text, double x, double y)
        {
            Text = text;
            X = x;
            Y = y;
        }

        public SignatureChange() { }

        internal SignatureChange Copy()
        {
            return new SignatureChange(this.Text.Copy(), this.X, this.Y);
        }

        internal bool Equals(SignatureChange sa)
        {
            return this.X == sa.X && this.Y == sa.Y && this.Text.Text == sa.Text.Text;
        }
    }

    public class BindableText : NotifyBase
    {
        private string text = string.Empty;
        public string Text
        {
            get => text;
            set
            {
                if (value != text)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        internal BindableText Copy()
        {
            return new BindableText() { Text = this.Text };
        }
    }
}