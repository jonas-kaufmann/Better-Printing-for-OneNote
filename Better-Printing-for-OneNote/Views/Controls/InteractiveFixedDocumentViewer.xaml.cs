using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
                ifdv.UpdateDocument();
        }

        public delegate FixedDocument PageSplitRequestedEventHandler(int pageNr, double splitAtPercentage);

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
        #endregion

        #region attributes
        private readonly List<DocumentPageView> documentPageViews = new List<DocumentPageView>();
        #endregion

        public InteractiveFixedDocumentViewer() => InitializeComponent();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainScrollViewer.Focus(); // Needs focus in order to receive keyboard events
        }

        #region rendering
        public void UpdateDocument()
        {
            // unregister attached event handlers
            foreach (DocumentPageView dPV in documentPageViews)
            {
                dPV.MouseLeftButtonUp -= DocumentPageView_PreviewMouseLeftButtonUp;
                dPV.MouseLeave -= DPV_MouseLeave;
            }

            PagesSP.Children.Clear();
            documentPageViews.Clear();

            for (int i = 0; i < Document.Pages.Count; i++)
            {
                Border border = new Border() { BorderThickness = new Thickness(1), BorderBrush = Brushes.Black };
                if (PagesSP.Children.Count != 0)
                    border.Margin = new Thickness(0, 24, 0, 0);

                DocumentPageView dPV = new DocumentPageView()
                {
                    DocumentPaginator = Document.DocumentPaginator,
                    PageNumber = i
                };
                border.Child = dPV;


                PagesSP.Children.Add(border);
                documentPageViews.Add(dPV);

                //attach event handlers
                dPV.PreviewMouseLeftButtonUp += DocumentPageView_PreviewMouseLeftButtonUp;
                dPV.MouseLeave += DPV_MouseLeave;
            }
        }
        public void UpdateZoom()
        {
            var oldMousePos = Mouse.GetPosition(PagesGrid);
            double oldHeight = PagesGrid.ActualHeight;

            PageSplitLine.Visibility = Visibility.Collapsed; // otherwise would block resizing of grid

            PagesSP.LayoutTransform = new ScaleTransform(Zoom, Zoom);
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

            UpdateSplittingLine();
        }
        #endregion

        #region pagesplitting line
        public void UpdateSplittingLine()
        {
            if (PagesGrid.IsMouseOver)
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

        private void DPV_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PageSplitLine.Visibility != Visibility.Collapsed)
                PageSplitLine.Visibility = Visibility.Collapsed;
        }

        // trigger pagesplit request at current mouse position
        private void DocumentPageView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (PageSplitRequestedCommand != null && sender is DocumentPageView dPV)
            {
                PageSplitLine.Visibility = Visibility.Collapsed; // prevent visual glitches

                int pageIndex = documentPageViews.IndexOf(dPV);
                if (pageIndex < 0)
                    return;

                var pos = e.GetPosition(dPV);
                double splitAtPercentage = pos.Y / dPV.ActualHeight;
                e.Handled = true;

                PageSplitRequestedCommand(this, pageIndex, splitAtPercentage);
            }
        }
        #endregion

        #region move view by rightlick & splitting line
        private Point lastMousePos;
        private bool moveViewByMouse = false;
        private void PagesGrid_PreviewMouseMove(object sender, MouseEventArgs e)
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
            else
                UpdateSplittingLine();

            e.Handled = true;
        }

        // enable move of view by mouse
        private void PagesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(MainScrollViewer);
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

        // zoom & undo with keyboard
        private void MainScrollViewer_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Key == Key.D0)
                {
                    Zoom = 1;
                    e.Handled = true;
                }
                else if (e.Key == Key.Add)
                {
                    Zoom += .25;
                    e.Handled = true;
                }
                else if (e.Key == Key.Subtract)
                {
                    Zoom -= .25;
                    e.Handled = true;
                }
                else if (UndoRequestedCommand != null && e.Key == Key.Z)
                {
                    UndoRequestedCommand(this);
                    e.Handled = true;
                }
            }
        }
    }
}
