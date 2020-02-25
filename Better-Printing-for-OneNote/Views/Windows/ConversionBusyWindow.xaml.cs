using Better_Printing_for_OneNote.Models;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace Better_Printing_for_OneNote.Views.Windows
{
    public partial class ConversionBusyWindow : Window
    {
        private Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 40, 40));
        private CancellationTokenSource CTS;
        public bool Completed = false;
        private ProgressReporter _reporter;
        public ProgressReporter Reporter
        {
            get
            {
                return _reporter;
            }
            set
            {
                if (_reporter != value)
                {
                    _reporter = value;
                    OnPropertyChanged("Reporter");
                }
            }
        }

        public ConversionBusyWindow(CancellationTokenSource cts, ProgressReporter reporter)
        {
            CTS = cts;
            Reporter = reporter;
            InitializeComponent();
            DataContext = this;
        }

        public void SetError(string errorMesagge)
        {
            Reporter.ReportProgress(100, errorMesagge);
            ProgessBar.Foreground = ErrorBrush;
            Completed = true;
            CancelBtn.IsEnabled = true;
            CancelBtn.Content = "OK";
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Completed)
                Close();
            else
            {

                CTS.Cancel();
                CancelBtn.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!Completed)
            {
                e.Cancel = true;
                CTS.Cancel();
            }
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
