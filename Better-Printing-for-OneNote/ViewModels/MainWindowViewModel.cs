using System;
using System.Windows;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.Threading.Tasks;
using System.Threading;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {

        #region Properties

        public event EventHandler BringWindowToFrontEvent;

        private string TITLE
        {
            get => GeneralHelperClass.FindResource("Title");
        }

        private RelayCommand _testCommand;
        public RelayCommand TestCommand
        {
            get
            {
                return _testCommand ?? (_testCommand = new RelayCommand(c => Test()));
            }
        }

        private string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        #endregion

        public MainWindowViewModel(string argFilePath)
        {
            FilePath = argFilePath;

            //eventuell unnötig
            Task.Run(() => 
            {
                Thread.Sleep(2000);
                BringWindowToFrontEvent?.Invoke(this, EventArgs.Empty);
            });
        }

        private void Test()
        {
            MessageBox.Show(FilePath);
        }
    }
}