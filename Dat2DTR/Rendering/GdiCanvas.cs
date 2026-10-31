using System.Drawing.Drawing2D;

namespace Dat2DTR
{
    /// <summary>
    /// This is where the DTR page is drawn with Windows graphics. Used for the preview on screen
    /// and for printing. (The PDF uses <see cref="PdfPage"/> instead.)
    ///
    /// The Graphics object must already be set up so that 1 unit = 1 point.
    /// </summary>
    internal sealed class GdiCanvas : IDtrCanvas, IDtrFontCanvas, IDtrImageCanvas, IDisposable
    {
        private readonly Graphics _graphics;
        private readonly FontFamily _family;
        private readonly bool _ownsFamily;
        private readonly StringFormat _format;
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<int, SolidBrush> _brushes = new Dictionary<int, SolidBrush>();

        public GdiCanvas(Graphics graphics)
        {
            _graphics = graphics;

            // This is Arial: it has the same letter widths as the Helvetica font used in the PDF.
            try
            {
                _family = new FontFamily("Arial");
                _ownsFamily = true;
            }
            catch (ArgumentException)
            {
                _family = FontFamily.GenericSansSerif;
            }

            // This is "Typographic" = no extra padding around the text, so positions are exact.
            _format = new StringFormat(StringFormat.GenericTypographic);
            _format.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.NoClip |
                                   StringFormatFlags.MeasureTrailingSpaces;
        }

        public void DrawLine(double x1, double y1, double x2, double y2, double thickness)
        {
            using (var pen = new Pen(Color.Black, (float)thickness))
            {
                pen.StartCap = LineCap.Square;
                pen.EndCap = LineCap.Square;
                _graphics.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
            }
        }

        public void FillRectangle(double x, double y, double width, double height, int rgb)
        {
            _graphics.FillRectangle(GetBrush(rgb), (float)x, (float)y, (float)width, (float)height);
        }

        public void DrawText(string text, double x, double baseline, double size, bool bold, int rgb)
        {
            if (text.Length == 0)
                return;

            FontStyle style = bold ? FontStyle.Bold : FontStyle.Regular;
            Font font = GetFont(size, style);

            // This is because DrawString wants the TOP of the text, and the page layout gives the BASELINE.
            // "Ascent" is the distance between the two.
            double ascent = size * _family.GetCellAscent(style) / _family.GetEmHeight(style);

            _graphics.DrawString(text, font, GetBrush(rgb), (float)x, (float)(baseline - ascent), _format);
        }

        /// <summary>
        /// This is the font family of each font file, kept for as long as the program runs (finding one takes time).
        /// The font installed in Windows is used when there is one (for example Old English Text MT comes with
        /// Microsoft Office); otherwise the file itself is loaded. The collections are kept so their fonts stay usable.
        /// </summary>
        private static readonly Dictionary<string, FontFamily?> FileFamilies = new Dictionary<string, FontFamily?>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<System.Drawing.Text.PrivateFontCollection> Collections = new List<System.Drawing.Text.PrivateFontCollection>();

        /// <summary>This is text in a TrueType font read from a file (the certificate heading).</summary>
        public void DrawText(string text, double x, double baseline, double size, TrueTypeFont trueType, int rgb)
        {
            if (text.Length == 0)
                return;

            FontFamily? family = FamilyOf(trueType);
            if (family == null)
            {
                DrawText(text, x, baseline, size, false, rgb);   // This is the fallback when Windows cannot read the file
                return;
            }

            // This is the same as for Arial: GDI+ draws from the TOP of the letters, the page gives the baseline.
            // The height above the baseline is taken from the font file (the same number Windows uses).
            try
            {
                using (var font = new Font(family, (float)size, FontStyle.Regular, GraphicsUnit.World))
                {
                    double ascent = size * trueType.WinAscent / 1000.0;
                    _graphics.DrawString(text, font, GetBrush(rgb), (float)x, (float)(baseline - ascent), _format);
                }
            }
            catch (ArgumentException)
            {
                DrawText(text, x, baseline, size, false, rgb);   // This is the fallback when Windows refuses the font
            }
        }

        /// <summary>This is the pictures already read, kept for as long as the program runs (with the bytes they came from).</summary>
        private static readonly Dictionary<JpegImage, Image?> Pictures = new Dictionary<JpegImage, Image?>();

        /// <summary>This is a JPEG picture in a box (top-left corner x, y), drawn smoothly.</summary>
        public void DrawImage(JpegImage image, double x, double y, double width, double height)
        {
            Image? picture;
            lock (Pictures)
            {
                if (!Pictures.TryGetValue(image, out picture))
                {
                    try
                    {
                        // This is a stream that stays open: GDI+ needs it for as long as the picture is used.
                        picture = Image.FromStream(new MemoryStream(image.Data));
                    }
                    catch (ArgumentException)
                    {
                        picture = null;
                    }
                    Pictures[image] = picture;
                }
            }
            if (picture == null)
                return;

            InterpolationMode before = _graphics.InterpolationMode;
            _graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _graphics.DrawImage(picture, (float)x, (float)y, (float)width, (float)height);
            _graphics.InterpolationMode = before;
        }

        private static FontFamily? FamilyOf(TrueTypeFont trueType)
        {
            lock (FileFamilies)
            {
                FontFamily? family;
                if (FileFamilies.TryGetValue(trueType.Path, out family))
                    return family;

                family = null;
                try
                {
                    // This is the font installed in Windows, when it is there.
                    using (var installed = new System.Drawing.Text.InstalledFontCollection())
                        if (installed.Families.Any(f => string.Equals(f.Name, trueType.FamilyName, StringComparison.OrdinalIgnoreCase)))
                            family = new FontFamily(trueType.FamilyName);
                }
                catch (ArgumentException) { family = null; }

                if (family == null)
                {
                    // This is the font file itself (from the Fonts folder).
                    try
                    {
                        var collection = new System.Drawing.Text.PrivateFontCollection();
                        collection.AddFontFile(trueType.Path);
                        Collections.Add(collection);
                        if (collection.Families.Length > 0)
                            family = collection.Families[0];
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is FileNotFoundException || ex is System.Runtime.InteropServices.ExternalException)
                    {
                        family = null;
                    }
                }

                if (family != null && !family.IsStyleAvailable(FontStyle.Regular))
                    family = null;
                FileFamilies[trueType.Path] = family;
                return family;
            }
        }

        private Font GetFont(double size, FontStyle style)
        {
            string key = size.ToString("0.00") + (int)style;
            Font? font;
            if (!_fonts.TryGetValue(key, out font))
            {
                // This is GraphicsUnit.World = the size follows the scaling of the Graphics object,
                // so the same code works for the screen and for any printer resolution.
                font = new Font(_family, (float)size, style, GraphicsUnit.World);
                _fonts[key] = font;
            }
            return font;
        }

        private SolidBrush GetBrush(int rgb)
        {
            SolidBrush? brush;
            if (!_brushes.TryGetValue(rgb, out brush))
            {
                brush = new SolidBrush(Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF));
                _brushes[rgb] = brush;
            }
            return brush;
        }

        public void Dispose()
        {
            foreach (Font font in _fonts.Values) font.Dispose();
            foreach (SolidBrush brush in _brushes.Values) brush.Dispose();
            _format.Dispose();
            if (_ownsFamily) _family.Dispose();
        }
    }
}
