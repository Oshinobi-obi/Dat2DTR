using System.Drawing.Drawing2D;

namespace Dat2DTR
{
    /// <summary>
    /// This is a box that shows a logo, shrunk smoothly to fit the box without stretching it.
    /// (A normal PictureBox shrinks large pictures roughly, which makes small logos look jagged.)
    /// </summary>
    public class LogoBox : Control
    {
        private Image? _image;

        public LogoBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }

        /// <summary>This is where the picture to show is set (null shows nothing).</summary>
        public void SetImage(Image? image)
        {
            _image = image;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_image != null && Width > 1 && Height > 1)
                DrawSmooth(e.Graphics, _image, FitInside(_image.Size, ClientRectangle));
        }

        /// <summary>This is the biggest rectangle with the picture's shape that fits in the area, centred.</summary>
        public static RectangleF FitInside(Size picture, RectangleF area)
        {
            float scale = Math.Min(area.Width / picture.Width, area.Height / picture.Height);
            float width = picture.Width * scale;
            float height = picture.Height * scale;
            return new RectangleF(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
        }

        /// <summary>This is where a picture is drawn into a rectangle with the smoothest scaling Windows offers.</summary>
        public static void DrawSmooth(Graphics g, Image image, RectangleF target)
        {
            GraphicsState saved = g.Save();
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, target);
            g.Restore(saved);
        }
    }
}
