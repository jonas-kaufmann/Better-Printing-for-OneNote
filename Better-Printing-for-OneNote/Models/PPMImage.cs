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

        public PPMImage Clone() => new PPMImage((string) FormatSpecifier.Clone(), Width, Height, (byte[]) Pixels.Clone());
    }
}