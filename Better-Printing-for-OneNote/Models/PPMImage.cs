using System;

namespace Better_Printing_for_OneNote.Models
{
    public class PPMImage
    {
        public string FormatSpecifier { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Pixels { get; set; }
        public int BytesPerPixel { get; set; } = 3;


        public PPMImage() { }

        public PPMImage(string formatSpecifier, int width, int height, byte[] pixels, int bytesPerPixel = 3)
        {
            FormatSpecifier = formatSpecifier;
            Width = width;
            Height = height;
            Pixels = pixels;
            BytesPerPixel = bytesPerPixel;
        }

        //public void Crop(int x1, int y1, int x2, int y2)
        //{
        //    if (x1 < 0 || y1 < 0 || x2 < 0 || y2 < 0 || x1 >= Width || y1 >= Height || x2 >= Width || y2 >= Height || x2 <= x1 || y2 <= y1)
        //        throw new ArgumentException();

        //    int cropStart = (Width * y1 + x1) * BytesPerPixel;
        //    int cropEnd = (Width * y2 + x2 + 1) * BytesPerPixel;

        //    Pixels = Pixels[cropStart..cropEnd];

        //    Width = x2 - x1;
        //    Height = y2 - y1;
        //}

        public PPMImage Clone() => new PPMImage((string) FormatSpecifier.Clone(), Width, Height, (byte[]) Pixels.Clone());
    }
}