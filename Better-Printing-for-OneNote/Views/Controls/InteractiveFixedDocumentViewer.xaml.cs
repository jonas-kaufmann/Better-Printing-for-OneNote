﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Better_Printing_for_OneNote.Views.Controls
{
    public partial class InteractiveFixedDocumentViewer : UserControl
    {
        #region properties
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document), typeof(FixedDocument), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(Document_Changed));

        public FixedDocument Document
        {
            get => (FixedDocument)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }
        private static void Document_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is InteractiveFixedDocumentViewer ifdv && e.OldValue != e.NewValue)
            {
                ifdv.MainScrollViewer.Visibility = Visibility.Hidden; // to prevent visual bugs
                ifdv.UpdateDocument();
            }
        }

        public delegate FixedDocument PageSplitRequestedEventHandler(object sender, int pageNr, double splitAtPercentage);

        #region zoom properties
        public double MinZoom { get; set; } = 0.4;
        public double MaxZoom { get; set; } = 4;


        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public static DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(1.0, Zoom_Changed, Zoom_Coerce));
        private static void Zoom_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is InteractiveFixedDocumentViewer ifdv && e.OldValue != e.NewValue)
                ifdv.UpdateZoom();
        }
        private static object Zoom_Coerce(DependencyObject d, object value)
        {
            double zoom = (double)value;
            InteractiveFixedDocumentViewer ifdv = (InteractiveFixedDocumentViewer)d;

            if (zoom < ifdv.MinZoom)
                zoom = ifdv.MinZoom;
            else if (zoom > ifdv.MaxZoom)
                zoom = ifdv.MaxZoom;

            return zoom;
        }


        public int PageCount
        {
            get => (int)GetValue(PageCountProperty);
            private set => SetValue(PageCountProperty, value);
        }
        public static DependencyProperty PageCountProperty = DependencyProperty.Register(nameof(PageCount), typeof(int), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(0, PageCount_Changed));
        private static void PageCount_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is InteractiveFixedDocumentViewer ifdv && e.OldValue != e.NewValue)
            {
                ifdv.PagesGrid.Visibility = (int)e.NewValue > 0 ? Visibility.Visible : Visibility.Hidden; // prevent a black dot in the center of the control when document page view is empty

                ifdv.PageNumber = ifdv.CorrectPageNumber(ifdv.PageNumber); // make sure currently displayed page number is still within bounds
            }
        }

        public int PageNumber
        {
            get => (int)GetValue(PageNumberProperty);
            set
            {
                SetValue(PageNumberProperty, value);
                EditAreaRect.Visibility = Visibility.Collapsed;
            }
        }
        public static DependencyProperty PageNumberProperty = DependencyProperty.Register(nameof(PageNumber), typeof(int), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(0, null, PageNumber_Coerce));
        private static object PageNumber_Coerce(DependencyObject d, object value)
        {
            int pageNumber = (int)value;
            var ifdv = (InteractiveFixedDocumentViewer)d;
            var correctPageNumber = ifdv.CorrectPageNumber(pageNumber);
            if (correctPageNumber != ifdv.PageNumber)
                ifdv.MainScrollViewer.Visibility = Visibility.Hidden; // to prevent visual bugs
            return correctPageNumber;
        }
        private int CorrectPageNumber(int value)
        {
            if (PageCount == 0)
                value = 0;
            else if (value >= PageCount)
                value = PageCount - 1;
            else if (value < 0)
                value = 0;

            return value;
        }


        public bool RenderPageNumbers
        {
            get => (bool)GetValue(RenderPageNumbersProperty);
            set => SetValue(RenderPageNumbersProperty, value);
        }
        public static DependencyProperty RenderPageNumbersProperty = DependencyProperty.Register(nameof(RenderPageNumbers), typeof(bool), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(false));
        #endregion

        #region page split command
        public PageSplitRequestedHandler PageSplitRequestedCommand
        {
            get => (PageSplitRequestedHandler)GetValue(PageSplitRequestedCommandProperty);
            set => SetValue(PageSplitRequestedCommandProperty, value);
        }

        public delegate void PageSplitRequestedHandler(object sender, int pageIndex, double splitAtPercentage);
        public static readonly DependencyProperty PageSplitRequestedCommandProperty = DependencyProperty.Register(nameof(PageSplitRequestedCommand), typeof(PageSplitRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion

        #region undo command
        public UndoRequestedHandler UndoRequestedCommand
        {
            get => (UndoRequestedHandler)GetValue(UndoRequestedCommandProperty);
            set => SetValue(UndoRequestedCommandProperty, value);
        }

        public delegate void UndoRequestedHandler(object sender);
        public static readonly DependencyProperty UndoRequestedCommandProperty = DependencyProperty.Register(nameof(UndoRequestedCommand), typeof(UndoRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion

        #region redo command
        public RedoRequestedHandler RedoRequestedCommand
        {
            get => (RedoRequestedHandler)GetValue(RedoRequestedCommandProperty);
            set => SetValue(RedoRequestedCommandProperty, value);
        }

        public delegate void RedoRequestedHandler(object sender);
        public static readonly DependencyProperty RedoRequestedCommandProperty = DependencyProperty.Register(nameof(RedoRequestedCommand), typeof(RedoRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion

        #region page delete command
        public PageDeleteRequestedHandler PageDeleteRequestedCommand
        {
            get => (PageDeleteRequestedHandler)GetValue(PageDeleteRequestedCommandProperty);
            set => SetValue(PageDeleteRequestedCommandProperty, value);
        }

        public delegate void PageDeleteRequestedHandler(object sender, int pageIndex);
        public static readonly DependencyProperty PageDeleteRequestedCommandProperty = DependencyProperty.Register(nameof(PageDeleteRequestedCommand), typeof(PageDeleteRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion

        #region delete area command
        public AreaDeleteRequestedHandler AreaDeleteRequestedCommand
        {
            get => (AreaDeleteRequestedHandler)GetValue(AreaDeleteRequestedCommandProperty);
            set => SetValue(AreaDeleteRequestedCommandProperty, value);
        }

        public delegate void AreaDeleteRequestedHandler(object sender, int pageIndex, double percentageDeleteStart, double percentageDeleteEnd);
        public static readonly DependencyProperty AreaDeleteRequestedCommandProperty = DependencyProperty.Register(nameof(AreaDeleteRequestedCommand), typeof(AreaDeleteRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion
        #region add signature to document command
        public AddSignatureRequestedHandler AddSignatureRequestedCommand
        {
            get => (AddSignatureRequestedHandler)GetValue(AddSignatureRequestedCommandProperty);
            set => SetValue(AddSignatureRequestedCommandProperty, value);
        }

        public delegate void AddSignatureRequestedHandler(object sender, double x, double y);
        public static readonly DependencyProperty AddSignatureRequestedCommandProperty = DependencyProperty.Register(nameof(AddSignatureRequestedCommand), typeof(AddSignatureRequestedHandler), typeof(InteractiveFixedDocumentViewer));
        #endregion

        #region page split tool
        public bool IsPageSplitToolSelected
        {
            get => (bool)GetValue(IsPageSplitToolSelectedProperty);
            set => SetValue(IsPageSplitToolSelectedProperty, value);
        }
        public static DependencyProperty IsPageSplitToolSelectedProperty = DependencyProperty.Register(nameof(IsPageSplitToolSelected), typeof(bool), typeof(InteractiveFixedDocumentViewer), new PropertyMetadata(true, IsPageSplitToolSelected_ChangedCallback));
        private static void IsPageSplitToolSelected_ChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((InteractiveFixedDocumentViewer)sender).IsPageSplitToolSelected_Changed();
        private void IsPageSplitToolSelected_Changed()
        {
            PagesGrid.Cursor = IsPageSplitToolSelected ? Cursors.Arrow : Cursors.IBeam;
        }
        #endregion
        #endregion

        public InteractiveFixedDocumentViewer()
        {
            InitializeComponent();
            MainGrid.DataContext = this;

        }

        #region rendering
        public void UpdateDocument()
        {
            MainDPV.DocumentPaginator = Document.DocumentPaginator;
            MainDPV.DocumentPaginator.GetPageCompleted += delegate { this.MainScrollViewer.Visibility = Visibility.Visible; };
            PageCount = Document.Pages.Count;
        }
        public void UpdateZoom()
        {
            var oldMousePos = Mouse.GetPosition(PagesGrid);
            double oldHeight = PagesGrid.ActualHeight;

            PageSplitLine.Visibility = Visibility.Collapsed; // otherwise would block resizing of grid

            MainDPVGrid.LayoutTransform = new ScaleTransform(Zoom, Zoom);
            PagesGrid.UpdateLayout();

            #region keep currently focused point in place
            double multiplier = PagesGrid.ActualHeight / oldHeight;

            double newX = oldMousePos.X * multiplier;
            double newY = oldMousePos.Y * multiplier;

            double offsetX = MainScrollViewer.HorizontalOffset + (newX - oldMousePos.X);
            double offsetY = MainScrollViewer.VerticalOffset + (newY - oldMousePos.Y);

            MainScrollViewer.ScrollToVerticalOffset(offsetY);
            MainScrollViewer.ScrollToHorizontalOffset(offsetX);

            #endregion
        }
        #endregion

        #region pagesplitting line
        public void UpdateSplittingLine()
        {
            if (IsPageSplitToolSelected && MainDPV.IsMouseOver)
            {
                var pos = Mouse.GetPosition(PagesGrid);

                PageSplitLine.X2 = PagesGrid.ActualWidth;
                PageSplitLine.Margin = new Thickness(0, pos.Y, 0, 0);
                PageSplitLine.Visibility = Visibility.Visible;
            }
            else
            {
                PageSplitLine.Visibility = Visibility.Collapsed;
            }
        }

        private void MainDPV_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PageSplitLine.Visibility != Visibility.Collapsed)
                PageSplitLine.Visibility = Visibility.Collapsed;
        }

        private void MainDPV_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsPageSplitToolSelected)
            {
                EditAreaRect.Width = MainDPV.ActualWidth;
                EditAreaRect.Height = 0;
                lastMousePos = e.GetPosition(MainDPV);
                EditAreaRect.Margin = new Thickness(0, lastMousePos.Y, 0, 0);
                EditAreaRect.Visibility = Visibility.Visible;
                PageSplitLine.Visibility = Visibility.Collapsed;
            }
        }

        private void MainDPV_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsPageSplitToolSelected)
            {
                // insert page break
                if ((EditAreaRect.Visibility != Visibility.Visible || (EditAreaRect.Visibility == Visibility.Visible && EditAreaRect.Height <= 1)) && PageSplitRequestedCommand != null && sender is DocumentPageView dPV)
                {
                    // prevent visual glitches
                    PageSplitLine.Visibility = Visibility.Collapsed;
                    EditAreaRect.Visibility = Visibility.Collapsed;

                    int pageIndex = MainDPV.PageNumber;
                    if (pageIndex < 0)
                        return;

                    var pos = e.GetPosition(dPV);
                    double splitAtPercentage = pos.Y / dPV.ActualHeight;
                    e.Handled = true;

                    PageSplitRequestedCommand(this, pageIndex, splitAtPercentage);
                }
            }
            else
            {
                if (!(Keyboard.FocusedElement is TextBox) && AddSignatureRequestedCommand != null)
                {
                    var mousePos = e.GetPosition(MainDPV);
                    AddSignatureRequestedCommand(this, mousePos.X, mousePos.Y);
                    var pageChildren = Document.Pages[PageNumber].Child.Children;
                    pageChildren[pageChildren.Count - 1].Focus();
                }
            }
        }
        #endregion

        #region move view by rightlick & splitting line
        private Point lastMousePos;
        private bool moveViewByMouse = false;
        private void MainDPV_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (moveViewByMouse && e.RightButton == MouseButtonState.Pressed)
            {
                var currentMousePos = e.GetPosition(MainScrollViewer);
                double xOffset = (lastMousePos.X - currentMousePos.X);
                double yOffset = (lastMousePos.Y - currentMousePos.Y);

                MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + xOffset);
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + yOffset);

                lastMousePos = currentMousePos;
            }
            else if (IsPageSplitToolSelected && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                double y = e.GetPosition(MainDPV).Y;
                if (y < lastMousePos.Y)
                {
                    EditAreaRect.Margin = new Thickness(0, y, 0, 0);
                    EditAreaRect.Height = lastMousePos.Y - y;
                }
                else
                    EditAreaRect.Height = y - EditAreaRect.Margin.Top;

                UpdateSplittingLine();

            }
            else
                UpdateSplittingLine();

            e.Handled = true;
        }

        // enable move of view by mouse
        private void PagesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(MainScrollViewer);
            EditAreaRect.Visibility = Visibility.Collapsed;
            moveViewByMouse = true;

            e.Handled = true;
        }
        // disable move of view by mouse
        private void PagesGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            moveViewByMouse = false;
            e.Handled = true;
        }
        #endregion

        // zoom with mousewheel
        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Zoom += e.Delta / 1000d;
                e.Handled = true;
            }
        }

        // keyboard commands
        public void OnApplication_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // prevent wrong misbehavior when typing into textboxes
            if (Keyboard.FocusedElement is TextBox)
                return;

            e.Handled = true;

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                if (e.Key == Key.D0)
                    Zoom = 1;
                else if (e.Key == Key.Add)
                    Zoom += .25;
                else if (e.Key == Key.Subtract)
                    Zoom -= .25;
                else if (e.Key == Key.Z && UndoRequestedCommand != null)
                    UndoRequestedCommand(this);
                else if (e.Key == Key.Y && RedoRequestedCommand != null)
                    RedoRequestedCommand(this);
                else
                    e.Handled = false;
            else if (e.Key == Key.Right)
                PageNumber += 1;
            else if (e.Key == Key.Left)
                PageNumber -= 1;
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (EditAreaRect.Visibility == Visibility.Visible && EditAreaRect.Height != 0)
                {
                    AreaDeleteRequestedCommand?.Invoke(this, MainDPV.PageNumber, EditAreaRect.Margin.Top / MainDPV.ActualHeight, (EditAreaRect.Margin.Top + EditAreaRect.Height) / MainDPV.ActualHeight);
                    EditAreaRect.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EditAreaRect.Visibility = Visibility.Collapsed;
                    PageDeleteRequestedCommand?.Invoke(this, MainDPV.PageNumber);
                }
            }
            else
                e.Handled = false;
        }

        // TextBox for page number: apply on Enter
        private void PageNumberTb_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var bindingExpr = PageNumberTb.GetBindingExpression(TextBox.TextProperty);
                bindingExpr.UpdateSource();
                e.Handled = true;
            }
        }
    }
}
