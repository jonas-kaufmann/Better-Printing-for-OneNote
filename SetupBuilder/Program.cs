using SetupBuilder.GUIDs;
using System;
using WixSharp;
using WixSharp.UI.Forms;

namespace SetupBuilder
{
    class Program
    {
        public const string PathToProject = "../../../Better-Printing-for-OneNote";
        public const string PathToIcon = PathToProject + "/Resources/Icon.ico";
        public const string ProjectPublishFolderProfileName = "FolderProfile";
        public const string ProjectPublishFolder = PathToProject + "/bin/publish/*";
        public const string PathToUpgradeCodes = "../../GUIDs/UpgradeCodes.json";

        public const string ProductName = "Better-Printing-for-OneNote";
        public const string ProgramExe = "Better-Printing-for-OneNote.exe";


        // to build the installer simply run this file
        static int Main()
        {
            try
            {
                var vsProject = new DotNetCoreProject(PathToProject);
                vsProject.Publish(ProjectPublishFolderProfileName); // publish project

                var project = new ManagedProject(ProductName,
                    new InstallDir($@"%ProgramFiles%\{ProductName}",
                        new Files(ProjectPublishFolder)),
                    new IconFile(new Id("icon.ico"), PathToIcon), 
                    new Property("ARPPRODUCTICON", "icon.ico"),
                    new Dir("%ProgramMenu%",
                        new ExeFileShortcut(ProductName, $"[INSTALLDIR]{ProgramExe}", ""))
                    )
                {
                    GUID = new Guid("1DB76552-1EEE-41DB-8628-68B25106C5B8"),
                    Platform = Platform.x64,
                    OutDir = "../publish",
                    Name = ProductName
                };

                // set project version
                project.Version = new Version(vsProject.ReadVersion());

                // set upgrade code
                project.UpgradeCode = new GUIDReaderWriter(PathToUpgradeCodes).GetGUIDForVersion(project.Version);

                project.ManagedUI = new ManagedUI();
                project.ManagedUI.InstallDialogs.Add<InstallDirDialog>()
                    .Add<ProgressDialog>()
                    .Add<ExitDialog>();

                project.BuildMsi();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("\nPress any key to continue ...");
                Console.ReadKey();
                return 1;
            }

            return 0;
        }
    }
}