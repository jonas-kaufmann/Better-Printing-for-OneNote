namespace Better_Printing_for_OneNote.Models
{
    public class ThirdPartyNoticeModel
    {
        public string SoftwareName { get; set; } = string.Empty;
        public string LicenseText { get; set; } = string.Empty;

        public ThirdPartyNoticeModel() { }
        public ThirdPartyNoticeModel(string softwareName, string licenseText)
        {
            SoftwareName = softwareName;
            LicenseText = licenseText;
        }
    }
}
