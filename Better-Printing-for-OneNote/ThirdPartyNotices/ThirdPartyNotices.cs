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
                LicenseText = "Copyright 2020 Anton Wittig"
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Wpf Cropable Image Control",
                LicenseText = File.ReadAllText("ThirdPartyNotices/WPF Cropable Image Control - License.txt")
            },
            new ThirdPartyNoticeModel
            {
                SoftwareName = "Extended WPF Toolkit",
                LicenseText = File.ReadAllText("ThirdPartyNotices/Extended WPF Toolkit - License.txt")
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
            }
        };
    }
}
