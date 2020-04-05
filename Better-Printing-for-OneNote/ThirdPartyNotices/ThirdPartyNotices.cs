using System.Collections.ObjectModel;
using System.IO;
using Better_Printing_for_OneNote.Models;

namespace Better_Printing_for_OneNote
{
    class ThirdPartyNotices
    {
        public static ObservableCollection<ThirdPartyNoticeModel> Notices = new ObservableCollection<ThirdPartyNoticeModel>()
        {
            new ThirdPartyNoticeModel
            {
                SoftwareName = "App Icon",
                LicenseText = "Copyright (c) 2020 Anton Wittig"
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Wpf Cropable Image Control",
                LicenseText = File.ReadAllText("ThirdPartyNotices/WPF Cropable Image Control - License.txt")
            },
            new ThirdPartyNoticeModel()
            {
                SoftwareName = "Xpdf",
                LicenseText = "Copyright 1996-2013 Glyph & Cog, LLC"
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Json.NET",
                LicenseText = File.ReadAllText("ThirdPartyNotices/Json.NET - License.txt")
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Wix# (WixSharp)",
                LicenseText = File.ReadAllText("ThirdPartyNotices/WixSharp - License.txt")
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "WiX Toolset",
                LicenseText = File.ReadAllText("ThirdPartyNotices/WiX Toolset - License.txt")
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Merge Pages Icon",
                LicenseText = "Icon by Those Icons (https://www.flaticon.com/de/autoren/those-icons) from Flaticon (https://www.flaticon.com/de/)"
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Trash Can Icon",
                LicenseText = "Icon by Cole Bemis (https://twitter.com/colebemis) from Feathericons (https://feathericons.com/)"
            },
        };
    }
}
