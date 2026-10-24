using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Dat2DTR
{
    /// <summary>
    /// This is a very small PDF writer: pages with straight lines and Helvetica text.
    /// That is everything the DTR form needs, so the app does not depend on any
    /// PDF library and works fully offline.
    /// </summary>
    public sealed class PdfDocument
    {
        private readonly List<PdfPage> _pages = new List<PdfPage>();

        public PdfPage AddPage(double width, double height)
        {
            var page = new PdfPage(width, height);
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
            //   then two objects per page: the page itself and its drawing commands.
            int objectCount = 5 + _pages.Count * 2;
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
                      "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + contentObject + " 0 R >>\nendobj\n");

                byte[] content = Compress(Encoding.ASCII.GetBytes(page.Content));
                offsets[contentObject] = position;
                write(contentObject + " 0 obj\n<< /Length " + content.Length + " /Filter /FlateDecode >>\nstream\n");
                output.Write(content, 0, content.Length);
                position += content.Length;
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
    public sealed class PdfPage : IDtrCanvas
    {
        private readonly StringBuilder _content = new StringBuilder();

        internal PdfPage(double width, double height)
        {
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
