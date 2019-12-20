using System;

namespace Better_Printing_for_OneNote.AdditionalClasses
{
    static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
