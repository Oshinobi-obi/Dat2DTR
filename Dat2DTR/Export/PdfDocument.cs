using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Dat2DTR
{
    /// <summary>
    /// This is a very small PDF writer: pages with straight lines and Helvetica text.
    /// That is everything the DTR form needs, so the app does not depend on any
    /// PDF library and works fully offline. A TrueType font from a file can also be used
    /// (the certificate heading); it is then put inside the PDF, so it looks the same everywhere.
    /// </summary>
    public sealed class PdfDocument
    {
        private readonly List<PdfPage> _pages = new List<PdfPage>();

        /// <summary>This is the TrueType fonts used on the pages, in the order they were first used (F3, F4, ...).</summary>
        private readonly List<TrueTypeFont> _fonts = new List<TrueTypeFont>();

        /// <summary>This is the JPEG pictures used on the pages (Im1, Im2, ...).</summary>
        private readonly List<JpegImage> _images = new List<JpegImage>();

        /// <summary>This is the name the pages use for that picture ("Im1", "Im2", ...).</summary>
        internal string ImageName(JpegImage image)
        {
            int index = _images.IndexOf(image);
            if (index < 0)
            {
                _images.Add(image);
                index = _images.Count - 1;
            }
            return "Im" + (index + 1);
        }

        /// <summary>This is the name the pages use for that font ("F3", "F4", ...).</summary>
        internal string FontName(TrueTypeFont font)
        {
            int index = _fonts.IndexOf(font);
            if (index < 0)
            {
                _fonts.Add(font);
                index = _fonts.Count - 1;
            }
            return "F" + (index + 3);
        }

        public PdfPage AddPage(double width, double height)
        {
            var page = new PdfPage(this, width, height);
            _pages.Add(page);
            return page;
        }

        public void Save(string path, string title)
        {
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                Save(file, title);
        }

        public void Save(Stream output, string title)
        {
            // This is how a PDF is built: a list of numbered "objects" followed by a table that says
            // at which byte each object starts.
            //   1 = catalog, 2 = page list, 3 = regular font, 4 = bold font, 5 = document info,
            //   then two objects per page: the page itself and its drawing commands,
            //   then three objects per TrueType font: the font, its description and the font file,
            //   then one object per JPEG picture.
            int fontStart = 6 + _pages.Count * 2;
            int imageStart = fontStart + _fonts.Count * 3;
            int objectCount = 5 + _pages.Count * 2 + _fonts.Count * 3 + _images.Count;
            var offsets = new long[objectCount + 1];
            long position = 0;

            Action<string> write = text =>
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                output.Write(bytes, 0, bytes.Length);
                position += bytes.Length;
            };

            write("%PDF-1.4\n");

            offsets[1] = position;
            write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            var kids = new StringBuilder();
            for (int i = 0; i < _pages.Count; i++)
                kids.Append(6 + i * 2).Append(" 0 R ");
            offsets[2] = position;
            write("2 0 obj\n<< /Type /Pages /Kids [ " + kids + "] /Count " + _pages.Count + " >>\nendobj\n");

            offsets[3] = position;
            write("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
            offsets[4] = position;
            write("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            offsets[5] = position;
            write("5 0 obj\n<< /Title " + PdfPage.Literal(title) + " /Producer (Dat2DTR) /CreationDate (D:" +
                  DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ") >>\nendobj\n");

            for (int i = 0; i < _pages.Count; i++)
            {
                PdfPage page = _pages[i];
                int pageObject = 6 + i * 2;
                int contentObject = pageObject + 1;

                offsets[pageObject] = position;
                write(pageObject + " 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                      PdfPage.Number(page.Width) + " " + PdfPage.Number(page.Height) + "] " +
                      "/Resources << /Font << /F1 3 0 R /F2 4 0 R" + FontResources(fontStart) + " >>" + ImageResources(imageStart) + " >> /Contents " + contentObject + " 0 R >>\nendobj\n");

                byte[] content = Compress(Encoding.ASCII.GetBytes(page.Content));
                offsets[contentObject] = position;
                write(contentObject + " 0 obj\n<< /Length " + content.Length + " /Filter /FlateDecode >>\nstream\n");
                output.Write(content, 0, content.Length);
                position += content.Length;
                write("\nendstream\nendobj\n");
            }

            // This is each TrueType font: the font, its description, and the whole font file (compressed).
            for (int f = 0; f < _fonts.Count; f++)
            {
                TrueTypeFont font = _fonts[f];
                int fontObject = fontStart + f * 3, descriptorObject = fontObject + 1, fileObject = fontObject + 2;
                string name = font.PostScriptName.Length > 0 ? font.PostScriptName : "Font" + (f + 3);

                var widths = new StringBuilder();
                for (int code = 32; code <= 255; code++)
                    widths.Append(PdfPage.Number(font.Widths[code])).Append(' ');
                offsets[fontObject] = position;
                write(fontObject + " 0 obj\n<< /Type /Font /Subtype /TrueType /BaseFont /" + name +
                      " /FirstChar 32 /LastChar 255 /Widths [ " + widths + "] /Encoding /WinAnsiEncoding /FontDescriptor " +
                      descriptorObject + " 0 R >>\nendobj\n");

                offsets[descriptorObject] = position;
                write(descriptorObject + " 0 obj\n<< /Type /FontDescriptor /FontName /" + name + " /Flags 32 /FontBBox [" +
                      string.Join(" ", font.BoundingBox.Select(PdfPage.Number).ToArray()) + "] /ItalicAngle " + PdfPage.Number(font.ItalicAngle) +
                      " /Ascent " + PdfPage.Number(font.Ascent) + " /Descent " + PdfPage.Number(font.Descent) +
                      " /CapHeight " + PdfPage.Number(font.CapHeight) + " /StemV 80" +
                      (font.CanEmbed ? " /FontFile2 " + fileObject + " 0 R" : "") + " >>\nendobj\n");

                // This is the font file. A font that does not allow it is not put inside (the PDF reader then uses a similar font).
                byte[] file = font.CanEmbed ? Compress(font.Data) : new byte[0];
                offsets[fileObject] = position;
                write(fileObject + " 0 obj\n<< /Length " + file.Length + " /Length1 " + (font.CanEmbed ? font.Data.Length : 0) +
                      (font.CanEmbed ? " /Filter /FlateDecode" : "") + " >>\nstream\n");
                output.Write(file, 0, file.Length);
                position += file.Length;
                write("\nendstream\nendobj\n");
            }

            // This is each JPEG picture, carried as it is (a PDF reader can show JPEG directly).
            for (int m = 0; m < _images.Count; m++)
            {
                JpegImage image = _images[m];
                offsets[imageStart + m] = position;
                write((imageStart + m) + " 0 obj\n<< /Type /XObject /Subtype /Image /Width " + image.Width + " /Height " + image.Height +
                      " /ColorSpace " + (image.Components == 1 ? "/DeviceGray" : "/DeviceRGB") +
                      " /BitsPerComponent 8 /Filter /DCTDecode /Length " + image.Data.Length + " >>\nstream\n");
                output.Write(image.Data, 0, image.Data.Length);
                position += image.Data.Length;
                write("\nendstream\nendobj\n");
            }

            long tableStart = position;
            write("xref\n0 " + (objectCount + 1) + "\n");
            write("0000000000 65535 f \n");
            for (int i = 1; i <= objectCount; i++)
                write(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");

            write("trailer\n<< /Size " + (objectCount + 1) + " /Root 1 0 R /Info 5 0 R >>\n");
            write("startxref\n" + tableStart + "\n%%EOF\n");
        }

        /// <summary>This is " /XObject << /Im1 15 0 R >>" - the pictures every page may use ("" when there are none).</summary>
        private string ImageResources(int imageStart)
        {
            if (_images.Count == 0)
                return "";
            var sb = new StringBuilder(" /XObject <<");
            for (int m = 0; m < _images.Count; m++)
                sb.Append(" /Im").Append(m + 1).Append(' ').Append(imageStart + m).Append(" 0 R");
            return sb.Append(" >>").ToString();
        }

        /// <summary>This is " /F3 9 0 R /F4 12 0 R" - the TrueType fonts every page may use.</summary>
        private string FontResources(int fontStart)
        {
            var sb = new StringBuilder();
            for (int f = 0; f < _fonts.Count; f++)
                sb.Append(" /F").Append(f + 3).Append(' ').Append(fontStart + f * 3).Append(" 0 R");
            return sb.ToString();
        }

        /// <summary>This is where data is compressed the way PDF expects it ("zlib" format).</summary>
        private static byte[] Compress(byte[] data)
        {
            using (var result = new MemoryStream())
            {
                result.WriteByte(0x78);
                result.WriteByte(0x9C);

                using (var deflate = new DeflateStream(result, CompressionMode.Compress, true))
                    deflate.Write(data, 0, data.Length);

                // This is the checksum zlib ends with: a checksum of the original data (Adler-32).
                uint a = 1, b = 0;
                foreach (byte value in data)
                {
                    a = (a + value) % 65521;
                    b = (b + a) % 65521;
                }
                uint checksum = (b << 16) | a;
                result.WriteByte((byte)(checksum >> 24));
                result.WriteByte((byte)(checksum >> 16));
                result.WriteByte((byte)(checksum >> 8));
                result.WriteByte((byte)checksum);

                return result.ToArray();
            }
        }
    }

    /// <summary>This is one PDF page. The DTR is drawn on it through <see cref="IDtrCanvas"/>.</summary>
    public sealed class PdfPage : IDtrCanvas, IDtrFontCanvas, IDtrImageCanvas
    {
        private readonly StringBuilder _content = new StringBuilder();
        private readonly PdfDocument _document;

        internal PdfPage(PdfDocument document, double width, double height)
        {
            _document = document;
            Width = width;
            Height = height;
            _content.Append("2 J\n"); // This is square line ends, so table corners meet cleanly
        }

        public double Width { get; private set; }
        public double Height { get; private set; }

        internal string Content
        {
            get { return _content.ToString(); }
        }

        // This is a note: PDF measures "y" from the BOTTOM of the page, so every y is flipped.

        public void DrawLine(double x1, double y1, double x2, double y2, double thickness)
        {
            _content.Append(Number(thickness)).Append(" w ")
                    .Append(Number(x1)).Append(' ').Append(Number(Height - y1)).Append(" m ")
                    .Append(Number(x2)).Append(' ').Append(Number(Height - y2)).Append(" l S\n");
        }

        public void FillRectangle(double x, double y, double width, double height, int rgb)
        {
            double red = ((rgb >> 16) & 0xFF) / 255.0;
            double green = ((rgb >> 8) & 0xFF) / 255.0;
            double blue = (rgb & 0xFF) / 255.0;

            _content.Append(Number(red)).Append(' ').Append(Number(green)).Append(' ').Append(Number(blue)).Append(" rg ")
                    .Append(Number(x)).Append(' ').Append(Number(Height - y - height)).Append(' ')
                    .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re f\n");
        }

        public void DrawText(string text, double x, double baseline, double size, bool bold, int rgb)
        {
            if (text.Length == 0)
                return;

            double r = ((rgb >> 16) & 0xFF) / 255.0;
            double g = ((rgb >> 8) & 0xFF) / 255.0;
            double b = (rgb & 0xFF) / 255.0;

            _content.Append("BT ").Append(bold ? "/F2 " : "/F1 ").Append(Number(size)).Append(" Tf ")
                    .Append(Number(r)).Append(' ').Append(Number(g)).Append(' ').Append(Number(b)).Append(" rg ")
                    .Append(Number(x)).Append(' ').Append(Number(Height - baseline)).Append(" Td ")
                    .Append(Literal(text)).Append(" Tj ET\n");
        }

        /// <summary>This is a JPEG picture in a box (top-left corner x, y; it is put inside the PDF, see PdfDocument.Save).</summary>
        public void DrawImage(JpegImage image, double x, double y, double width, double height)
        {
            _content.Append("q ").Append(Number(width)).Append(" 0 0 ").Append(Number(height)).Append(' ')
                    .Append(Number(x)).Append(' ').Append(Number(Height - y - height)).Append(" cm /")
                    .Append(_document.ImageName(image)).Append(" Do Q\n");
        }

        /// <summary>This is text in a TrueType font (it is put inside the PDF, see PdfDocument.Save).</summary>
        public void DrawText(string text, double x, double baseline, double size, TrueTypeFont font, int rgb)
        {
            if (text.Length == 0)
                return;

            double r = ((rgb >> 16) & 0xFF) / 255.0;
            double g = ((rgb >> 8) & 0xFF) / 255.0;
            double b = (rgb & 0xFF) / 255.0;

            _content.Append("BT /").Append(_document.FontName(font)).Append(' ').Append(Number(size)).Append(" Tf ")
                    .Append(Number(r)).Append(' ').Append(Number(g)).Append(' ').Append(Number(b)).Append(" rg ")
                    .Append(Number(x)).Append(' ').Append(Number(Height - baseline)).Append(" Td ")
                    .Append(Literal(text)).Append(" Tj ET\n");
        }

        internal static string Number(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>This is where text is written the way PDF wants it: (inside brackets, with special characters escaped).</summary>
        internal static string Literal(string text)
        {
            var sb = new StringBuilder("(");
            foreach (char c in text)
            {
                byte code = FontMetrics.ToWinAnsi(c);
                if (code == '(' || code == ')' || code == '\\')
                    sb.Append('\\').Append((char)code);
                else if (code < 32 || code > 126)
                    sb.Append('\\').Append(Convert.ToString(code, 8).PadLeft(3, '0'));
                else
                    sb.Append((char)code);
            }
            return sb.Append(')').ToString();
        }
    }
}
