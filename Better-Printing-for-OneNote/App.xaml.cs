using Better_Printing_for_OneNote.AdditionalClasses;
using System;
using System.Diagnostics;
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // initialize Resources
            var localFolderPath = $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Better_Printing_for_OneNote.Properties.Resources.LocalFolderTitle)}\\";
            Resources["LocalFolderPath"] = localFolderPath;
            Resources["TempFolderPath"] = Path.GetTempPath();

            #region Logging

#if DEBUG
            DEBUG_MODE = true;
#endif
            if (!DEBUG_MODE)
            {
                try
                {
                    GeneralHelperClass.CreateDirectoryIfNotExists(localFolderPath);
                    File.WriteAllText($"{localFolderPath}logs.txt", ""); // clear logfile
                    Trace.Listeners.Add(new TextWriterTraceListener($"{localFolderPath}logs.txt"));
                    Trace.AutoFlush = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Logging konnte nicht aktiviert werden:\n{ex.ToString()}\n");
                    try
                    {
                        File.Delete($"{localFolderPath}logs.txt");
                    }
                    catch (Exception)
                    {
                    }
                }
                Trace.WriteLine("Release-Mode");
            }
            else Trace.WriteLine("Debug-Mode");

            #endregion

            var argFilePath = "";
            if (e.Args.Length > 0)
            {
                argFilePath = e.Args[0];
                Trace.WriteLine($"Anwendung mit folgendem Startparameter gestartet {argFilePath}");
            }
            else
            {
                Trace.WriteLine("Anwendung ohne Startparameter gestartet");
            }
            (new MainWindow(argFilePath) { WindowState = WindowState.Maximized }).Show();
        }
    }
}
