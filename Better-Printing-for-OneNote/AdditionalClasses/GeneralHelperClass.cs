using System;
using System.IO;
using System.Windows;

namespace Better_Printing_for_OneNote.AdditionalClasses
{
    static class GeneralHelperClass
    {
        /// <summary>
        /// Gibt den Wert der Resource als string zurück
        /// </summary>
        /// <param name="key">x:Key der Resource</param>
        /// <returns>Wert als string</returns>
        public static string FindResource(string key)
        {
            return Application.Current.FindResource(key).ToString();
        }

        /// <summary>
        /// Führt die übergebene Action im Application UI Thread aus
        /// </summary>
        /// <param name="call">Action</param>
        private static void ExecuteInUiThread(Action call)
        {
            Application.Current.Dispatcher.Invoke(call);
        }

        /// <summary>
        /// Erstellt den angegebenen Ordner, falls dieser nicht existiert
        /// </summary>
        /// <param name="path">Pfad zum Ordner</param>
        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
