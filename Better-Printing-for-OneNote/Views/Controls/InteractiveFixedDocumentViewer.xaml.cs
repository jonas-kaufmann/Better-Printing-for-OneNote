using System.Collections.Generic;
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
        private FixedDocument document;
        public FixedDocument Document
        {
            get => document;
            set
            {
                document = value;
                RerenderDocument();
            }
        }

        public delegate FixedDocument PageSplitRequestedEventHandler(int pageNr, int splitAt);
        public event PageSplitRequestedEventHandler PageSplitRequested;

        #region zoom properties
        private double minZoom = 0.4;
        public double MinZoom
        {
            get => minZoom;
            set
            {
                minZoom = value;
            }
        }
        private double maxZoom = 4;
        public double MaxZoom
        {
            get => maxZoom;
            set
            {
                maxZoom = value;
            }
        }
        private double zoom = 1;
        public double Zoom
        {
            get => zoom;
            set
            {
                double oldVal = zoom;
                zoom = value;
                // insure zoom inside bounds
                if (zoom < minZoom)
                    zoom = minZoom;
                else if (zoom > maxZoom)
                    zoom = maxZoom;

                if (zoom != oldVal)
                {
                    var mousePos = Mouse.GetPosition(PagesGrid);

                    PageSplitLine.Visibility = Visibility.Collapsed; // otherwise would block resizing of grid
                    

                    PagesSP.LayoutTransform = new ScaleTransform(zoom, zoom, mousePos.X, mousePos.Y);
                    PagesGrid.UpdateLayout();

                    UpdateSplittingLine();
                }
            }
        }
        #endregion

        private List<DocumentPageView> documentPageViews = new List<DocumentPageView>();

        public InteractiveFixedDocumentViewer()
        {
            InitializeComponent();
        }

        private void RerenderDocument()
        {
            PagesSP.Children.Clear();
            documentPageViews.Clear();

            for (int i = 0; i < Document.Pages.Count; i++)
            {
                DocumentPageView dPV = new DocumentPageView()
                {
                    DocumentPaginator = document.DocumentPaginator,
                    PageNumber = i
                };


                PagesSP.Children.Add(dPV);
                documentPageViews.Add(dPV);
                dPV.MouseLeftButtonUp += DocumentPageView_MouseLeftButtonUp;
                dPV.MouseLeave += DPV_MouseLeave;
            }
        }

        private void DPV_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (PageSplitLine.Visibility != Visibility.Collapsed)
                PageSplitLine.Visibility = Visibility.Collapsed;
        }

        private void DocumentPageView_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DocumentPageView dPV)
            {
                int pageNr = documentPageViews.IndexOf(dPV);
                if (pageNr < 0)
                    return;

                var pos = e.GetPosition(dPV);
                int splitAt = (int)pos.Y;
                PageSplitRequested?.Invoke(pageNr, splitAt);
            }
        }


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

        private void PagesGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(MainScrollViewer);
            moveViewByMouse = true;

            e.Handled = true;
        }

        private void PagesGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            moveViewByMouse = false;
            e.Handled = true;
        }

        private void UpdateSplittingLine()
        {
            if (PagesGrid.IsMouseOver)
            {
                var pos = Mouse.GetPosition(PagesGrid);

                PageSplitLine.X2 = PagesGrid.ActualWidth;
                PageSplitLine.Margin = new Thickness(0, pos.Y, 0, 0);
                PageSplitLine.Visibility = Visibility.Visible;
            } else
            {
                PageSplitLine.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        // zoom with mousewheel
        private void MainGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Zoom += e.Delta / 1000d;
                e.Handled = true;
            }
        }

        #region zoom with keyboard
        private void MainScrollViewer_PreviewKeyUp(object sender, KeyEventArgs e) => HandleKeyUp(e);
        private void MainGrid_PreviewKeyUp(object sender, KeyEventArgs e) => HandleKeyUp(e);

        private void HandleKeyUp(KeyEventArgs e)
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
            }
        }
        #endregion
    }
}
