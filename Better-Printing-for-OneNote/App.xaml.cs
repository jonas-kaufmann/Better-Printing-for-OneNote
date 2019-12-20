using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Better_Printing_for_OneNote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var argFilePath = "";
            if(e.Args.Length > 0)
                argFilePath = e.Args[0];
            (new MainWindow(argFilePath)).Show();
        }
    }
}
