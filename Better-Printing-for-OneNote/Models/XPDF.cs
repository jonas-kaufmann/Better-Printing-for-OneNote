using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Better_Printing_for_OneNote.Models
{
    public static class XPDF
    {
        private const string PDFINFOPATH = "Xpdf\\bin\\pdfinfo.exe";
        private const string PDFTOPPMPath = "Xpdf\\bin\\pdftoppm.exe";

        public static int GetPageCount(string filePath)
        {
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PDFINFOPATH,
                    Arguments = $"\"{filePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            p.Start();
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception($"pdfinfo.exe failed with ExitCode {p.ExitCode}");

            string text = p.StandardOutput.ReadToEnd();

            //find page count in text
            string searchFor = "Pages:";
            int index = text.IndexOf(searchFor) + searchFor.Length;

            // skip whitespace
            while (char.IsWhiteSpace(text[index]))
                index++;

            string number = "";
            for (int i = index; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                    number += text[i];
                else
                    break;
            }

            return int.Parse(number);
        }

        /// <param name="page">index of the page to convert, starts at 1</param>
        /// <param name="pipename">when running this method multiple times at once, has to be set to a different value for each instance</param>
        public static PPMImage GetPageAsPPM(string filePath, int page, int dpi = 300, bool antiAliasing = false, bool vectorAntiAliasing = false, string pipename = "BPfON")
        {
            byte[] bytes;

            if (page < 1)
                throw new ArgumentException("Value can't be less than one.", nameof(page));

            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipename, PipeDirection.In))
            {
                string absolutePathToPPM = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + PDFTOPPMPath;

                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/C (\"{absolutePathToPPM}\" -f {page} -l {page} -r {dpi} -aa {(antiAliasing ? "yes" : "no")} -aaVector {(vectorAntiAliasing ? "yes" : "no")} \"{filePath}\" -) > \"\\\\.\\pipe\\{pipename}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };

                p.Start();

                List<byte> bytesList = new List<byte>(50000000);
                Span<byte> buffer = stackalloc byte[4096];

                pipeServer.WaitForConnection();

                int readLength = pipeServer.Read(buffer);
                while (readLength > 0)
                {

                    bytesList.AddRange(buffer.Slice(0, readLength).ToArray());

                    readLength = pipeServer.Read(buffer);
                }

                bytes = bytesList.ToArray();
            }            

            #region Parse PPM-Image
            PPMImage image = new PPMImage();
            byte splitByte = (byte)'\n';
            int oneAfterLastCut = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == splitByte)
                {
                    image.FormatSpecifier = Encoding.UTF8.GetString(bytes[oneAfterLastCut..i]);
                    if (image.FormatSpecifier != "P6")
                        throw new Exception("Unsupported PPM format");
                    oneAfterLastCut = i + 1;
                    break;
                }
            }

            for (int i = oneAfterLastCut; i < bytes.Length; i++)
            {
                if (bytes[i] == splitByte)
                {
                    string text = Encoding.UTF8.GetString(bytes[oneAfterLastCut..i]);
                    string[] splitText = text.Split(" ");
                    if (splitText.Length != 2)
                        throw new Exception("Invalid PPM format");
                    image.Width = int.Parse(splitText[0]);
                    image.Height = int.Parse(splitText[1]);
                    oneAfterLastCut = i + 1;
                    break;
                }
            }

            for (int i = oneAfterLastCut; i < bytes.Length; i++)
            {
                if (bytes[i] == splitByte)
                {
                    oneAfterLastCut = i + 1;
                    break;
                }
            }

            int bytesPerPixel = 3;
            int pixelsDataEnd = image.Width * image.Height * bytesPerPixel + oneAfterLastCut;

            image.Pixels = bytes[oneAfterLastCut..pixelsDataEnd];
            #endregion

            return image;
        }
    }
}
