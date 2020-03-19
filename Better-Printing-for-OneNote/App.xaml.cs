﻿using Better_Printing_for_OneNote.AdditionalClasses;
using Better_Printing_for_OneNote.Views.Windows;
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
        private bool LOGGING_INITIALIZED = false;
        private string LOCAL_FOLDER_PATH;

        public App()
        {
            #region Logging

#if DEBUG
            DEBUG_MODE = true;
#endif

            if (!DEBUG_MODE)
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            #endregion

            // initialize Resources
            LOCAL_FOLDER_PATH = $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Better_Printing_for_OneNote.Properties.Resources.LocalFolderTitle)}\\";
            Application.Current.Resources["LocalFolderPath"] = LOCAL_FOLDER_PATH;

            #region Logging

            if (!DEBUG_MODE)
            {
                try
                {
                    GeneralHelperClass.CreateDirectoryIfNotExists(LOCAL_FOLDER_PATH);
                    File.WriteAllText($"{LOCAL_FOLDER_PATH}logs.txt", ""); // clear logfile
                    Trace.Listeners.Add(new TextWriterTraceListener($"{LOCAL_FOLDER_PATH}logs.txt"));
                    Trace.AutoFlush = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Logging konnte nicht aktiviert werden:\n{ex.ToString()}\n");
                    try
                    {
                        File.Delete($"{LOCAL_FOLDER_PATH}logs.txt");
                    }
                    catch (Exception)
                    {
                    }
                }
                Trace.WriteLine("Release-Mode");
            }
            else Trace.WriteLine("Debug-Mode");
            LOGGING_INITIALIZED = true;

            #endregion
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var output = $"UnhandledException:\n{e.ToString()}";
            Trace.WriteLine(output);
            if (!LOGGING_INITIALIZED)
            {
                var localFolderPath = GeneralHelperClass.FindResource("LocalFolderPath");
                GeneralHelperClass.CreateDirectoryIfNotExists(localFolderPath);
                File.WriteAllText($"{localFolderPath}logs.txt", output);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // initialize Resources
            Resources["TempFolderPath"] = Path.GetTempPath();
            Resources["LocalFolderPath"] = LOCAL_FOLDER_PATH;

            var argFilePath = "";
            if (e.Args.Length > 0)
            {
                argFilePath = e.Args[0];
                Trace.WriteLine($"Anwendung mit folgendem Startparameter gestartet {argFilePath}");
            }
            else
                Trace.WriteLine("Anwendung ohne Startparameter gestartet");

            (new MainWindow(argFilePath) { WindowState = WindowState.Maximized }).Show();
        }
    }
}
