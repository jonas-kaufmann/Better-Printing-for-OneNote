using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Setup
{
    public class DotNetCoreProject
    {
        public readonly string PathToProject;

        public DotNetCoreProject(string pathToProject)
        {
            PathToProject = pathToProject;

            if (!Directory.Exists(PathToProject))
                throw new Exception($"{nameof(pathToProject)} is not valid");

            var files = Directory.GetFiles(PathToProject, "*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                throw new Exception("No valid project was found at the specified path");
        }

        /// <summary>
        /// Reads the version node from .csproj file
        /// </summary>
        /// <returns>null if version node was not found in .csproj file, else the inner text of the node</returns>
        public string ReadVersion()
        {
            var files = Directory.GetFiles(PathToProject, "*.csproj", SearchOption.TopDirectoryOnly);

            if (files.Length != 1)
                throw new Exception("Either more than one .csproj file or none exist in the project directory");

            XmlDocument doc = new XmlDocument();
            doc.Load(files[0]);

            if (doc.SelectSingleNode("/Project/PropertyGroup/Version") is XmlNode versionNode)
            {
                return versionNode.InnerText;
            }

            return null;
        }


        public void Publish(string folderProfileName)
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"publish /p:PublishProfile={folderProfileName}";
            startInfo.WorkingDirectory = PathToProject;
            p.StartInfo = startInfo;

            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception("Publish on project failed");
        }
    }
}
