using Better_Printing_for_OneNote.AdditionalClasses;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for BusyIndicatorWindow.xaml
    /// </summary>
    public partial class BusyIndicatorWindow : Window
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

        public BusyIndicatorWindow(CancellationTokenSource cts, ProgressReporter reporter)
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
            CancelBtn.IsEnabled = false;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CTS.Cancel();
            CancelBtn.IsEnabled = false;
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
