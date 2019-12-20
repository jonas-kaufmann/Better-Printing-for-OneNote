using System;
using System.Windows;
using Better_Printing_for_OneNote.AdditionalClasses;
using System.Threading.Tasks;
using System.Threading;

namespace Better_Printing_for_OneNote.ViewModels
{
    class MainWindowViewModel : NotifyBase
    {
        #region Konstanten

        private string LOCAL_FOLDER_PATH
        {
            get => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SupremeBot";
        }

        private string TITLE
        {
            get => FindResource("SupremeBot_Title");
        }

        #endregion

        #region Properties

        public event EventHandler BringWindowToFrontEvent;

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

        /// <summary>
        /// Gibt den Wert der Resource als string zurück
        /// </summary>
        /// <param name="key">x:Key der Resource</param>
        /// <returns>Wert als string</returns>
        private string FindResource(string key)
        {
            return Application.Current.FindResource(key).ToString();
        }

        /// <summary>
        /// Führt die übergebene Action im Application UI Thread aus
        /// </summary>
        /// <param name="call">Action</param>
        private void ExecuteInUiThread(Action call)
        {
            Application.Current.Dispatcher.Invoke(call);
        }
    }
}