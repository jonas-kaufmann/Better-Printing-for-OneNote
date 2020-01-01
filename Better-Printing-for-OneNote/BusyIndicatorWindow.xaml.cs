using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for BusyIndicatorWindow.xaml
    /// </summary>
    public partial class BusyIndicatorWindow : Window
    {
        private CancellationTokenSource CTS;

        public BusyIndicatorWindow(CancellationTokenSource cts)
        {
            CTS = cts;
            InitializeComponent();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CTS.Cancel();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!CTS.IsCancellationRequested)
            {
                e.Cancel = true;
                CTS.Cancel();
            }
        }
    }
}
