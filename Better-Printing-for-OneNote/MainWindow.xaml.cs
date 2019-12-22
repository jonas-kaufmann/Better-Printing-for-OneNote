using Better_Printing_for_OneNote.ViewModels;
using System;
using System.Windows;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel vm;

        public MainWindow(string argFilePath)
        {
            var viewModel = new MainWindowViewModel(argFilePath);
            DataContext = viewModel;
            viewModel.BringWindowToFrontEvent += new EventHandler(BringWindowToFront);

            InitializeComponent();


            vm = viewModel;
        }


        private void BringWindowToFront(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsVisible)
                {
                    Show();
                }
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Activate();
                Topmost = true;
                Topmost = false;
                Focus();
            });
        }

        private System.Windows.Documents.FixedDocument MainIFDV_PageSplitRequested(int pageNr, int splitAt)
        {
            return vm.Test(pageNr, splitAt);
        }
    }
}
