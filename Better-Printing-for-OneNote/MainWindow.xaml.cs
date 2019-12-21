using Better_Printing_for_OneNote.AdditionalClasses;
using Better_Printing_for_OneNote.ViewModels;
using System;
using System.IO;
using System.Windows;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool DEBUG_MODE = false;
        private FileStream _loggingFileStream;
        private StreamWriter _loggingWriter;

        public MainWindow(string argFilePath)
        {
            // Logging
#if DEBUG
            DEBUG_MODE = true;
#endif
            if (!DEBUG_MODE)
            {
                var path = Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData) + "\\" + GeneralHelperClass.FindResource("LocalFolderTitle");
                try
                {
                    Directory.CreateDirectory(path);
                    _loggingFileStream = new FileStream(path + "\\log.txt",
                        FileMode.Create, FileAccess.Write);
                    _loggingWriter = new StreamWriter(_loggingFileStream);
                    _loggingWriter.AutoFlush = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Logging konnte nicht aktiviert werden.");
                    Console.WriteLine(e.Message);
                    File.Delete(path + "\\log.txt");
                }
                Console.SetOut(_loggingWriter);
                Console.SetError(_loggingWriter);
                Console.WriteLine("Release-Mode");
            }
            else Console.WriteLine("Debug-Mode");

            //ViewModel
            var viewModel = new MainWindowViewModel(argFilePath);
            DataContext = viewModel;
            viewModel.BringWindowToFrontEvent += new EventHandler(BringWindowToFront);

            InitializeComponent();
        }

        private void BringWindowToFront(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!this.IsVisible)
                {
                    this.Show();
                }
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
                this.Focus();
            });
        }
    }
}
