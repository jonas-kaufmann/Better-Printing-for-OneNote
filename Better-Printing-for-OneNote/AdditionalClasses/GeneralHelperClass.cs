using System;
using System.Collections.Generic;
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
        public static void ExecuteInUiThread(Action call)
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

        /// <summary>
        /// Compares two list with an EqualityComparer
        /// </summary>
        /// <typeparam name="T">the type of the list elements (has to work with the EC)</typeparam>
        /// <param name="list1">first list</param>
        /// <param name="list2">second list</param>
        /// <returns>true: equal, false: not equal</returns>
        public static bool CompareList<T>(List<T> list1, List<T> list2)
        {
            if (list1.Count == list2.Count)
            {
                for (int i = 0; i < list1.Count; i++)
                    if (!EqualityComparer<T>.Default.Equals(list1[i], list2[i])) return false;
            } else return false;
            return true;
        }
    }
}
