using Better_Printing_for_OneNote.AdditionalClasses;

namespace Better_Printing_for_OneNote.Models
{
    public class ProgressReporter : NotifyBase
    {
        private double _percentageCompleted = 0;
        public double PercentageCompleted
        {
            get
            {
                return _percentageCompleted;
            }
            private set
            {
                if (_percentageCompleted != value)
                {
                    _percentageCompleted = value;
                    OnPropertyChanged("PercentageCompleted");
                }
            }
        }

        private string _currentTaskDescription = "";
        public string CurrentTaskDescription
        {
            get
            {
                return _currentTaskDescription;
            }
            private set
            {
                if (_currentTaskDescription != value)
                {
                    _currentTaskDescription = value;
                    OnPropertyChanged("CurrentTaskDescription");
                }
            }
        }

        public ProgressReporter() { }

        public void ReportProgress(double percentageCompleted, string currentTaskDescription)
        {
            GeneralHelperClass.ExecuteInUiThread(() =>
            {
                PercentageCompleted = percentageCompleted;
                CurrentTaskDescription = currentTaskDescription;
            });
        }

        public void ReportProgress(double percentageCompleted)
        {
            GeneralHelperClass.ExecuteInUiThread(() =>
            {
                PercentageCompleted = percentageCompleted;
            });
        }

        public void ReportProgress(string currentTaskDescription)
        {
            GeneralHelperClass.ExecuteInUiThread(() =>
            {
                CurrentTaskDescription = currentTaskDescription;
            });
        }
    }

}
