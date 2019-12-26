using System;
using WixSharp;
using WixSharp.UI.Forms;

namespace Setup
{
    class Program
    {
        public const string ProductName = "BetterPrinting";
        public const string ProgramExe = "Better-Printing-for-OneNote.exe";


        // TODO before building the installer for an update:
        // - run publish for all projects that are included in the setup
        // - edit project.version
        // - insert newly generated GUID for project.UpgradeCode
        // Build setup by running build

        static int Main()
        {
            try
            {
                var project = new ManagedProject(ProductName,
                    new InstallDir($@"%ProgramFiles%\{ProductName}",
                        new Files(@"..\Better-Printing-for-OneNote\bin\Release\netcoreapp3.1\publish\*")),
                    new Dir("%ProgramMenu%",
                        new ExeFileShortcut(ProductName, $"[INSTALLDIR]{ProgramExe}", ""))
                    )
                {
                    GUID = new Guid("1DB76552-1EEE-41DB-8628-68B25106C5B8"),
                    Platform = Platform.x64,
                    OutDir = @"publish\",
                    Version = new Version("1.0.0"),
                    UpgradeCode = new Guid("31577167-F5B1-4C08-A8E3-A896F11ABB62")
                };

                project.ManagedUI = new ManagedUI();
                project.ManagedUI.InstallDialogs.Add<InstallDirDialog>()
                    .Add<ProgressDialog>()
                    .Add<ExitDialog>();

                project.BuildMsi();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }

            return 0;
        }
    }
}