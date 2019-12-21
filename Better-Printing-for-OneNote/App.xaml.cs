using Better_Printing_for_OneNote.AdditionalClasses;
using System;
using System.IO;
using System.Windows;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool DEBUG_MODE = false;
        private FileStream _loggingFileStream;
        private StreamWriter _loggingWriter;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            #region Logging

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
                catch (Exception ex)
                {
                    Console.WriteLine("Logging konnte nicht aktiviert werden.");
                    Console.WriteLine(ex.Message);
                    File.Delete(path + "\\log.txt");
                }
                Console.SetOut(_loggingWriter);
                Console.SetError(_loggingWriter);
                Console.WriteLine("Release-Mode");
            }
            else Console.WriteLine("Debug-Mode");

            #endregion

            var argFilePath = "";
            if(e.Args.Length > 0)
                argFilePath = e.Args[0];
            var window = new MainWindow(argFilePath) { WindowState = WindowState.Maximized };
            window.Show();
        }
    }
}
