using System.Drawing.Drawing2D;

namespace Dat2DTR
{
    /// <summary>
    /// Draws the DTR page with Windows graphics. Used for the preview on screen
    /// and for printing. (The PDF uses <see cref="PdfPage"/> instead.)
    ///
    /// The Graphics object must already be set up so that 1 unit = 1 point.
    /// </summary>
    internal sealed class GdiCanvas : IDtrCanvas, IDisposable
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

            // Arial has the same letter widths as the Helvetica font used in the PDF.
            try
            {
                _family = new FontFamily("Arial");
                _ownsFamily = true;
            }
            catch (ArgumentException)
            {
                _family = FontFamily.GenericSansSerif;
            }

            // "Typographic" = no extra padding around the text, so positions are exact.
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

            // DrawString wants the TOP of the text, the page layout gives the BASELINE.
            // "Ascent" is the distance between the two.
            double ascent = size * _family.GetCellAscent(style) / _family.GetEmHeight(style);

            _graphics.DrawString(text, font, GetBrush(rgb), (float)x, (float)(baseline - ascent), _format);
        }

        private Font GetFont(double size, FontStyle style)
        {
            string key = size.ToString("0.00") + (int)style;
            Font? font;
            if (!_fonts.TryGetValue(key, out font))
            {
                // GraphicsUnit.World = the size follows the scaling of the Graphics object,
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
