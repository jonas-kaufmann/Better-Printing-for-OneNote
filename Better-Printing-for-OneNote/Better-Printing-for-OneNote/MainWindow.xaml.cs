using Better_Printing_for_OneNote.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                        Environment.SpecialFolder.LocalApplicationData) + "\\Better-Printing-for-One-Note";
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
