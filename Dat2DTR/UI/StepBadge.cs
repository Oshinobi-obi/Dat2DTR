using System.Drawing.Drawing2D;

namespace Dat2DTR
{
    /// <summary>The round navy "1", "2", "3" markers beside each step.</summary>
    public class StepBadge : Control
    {
        public StepBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(30, 30);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int diameter = Math.Min(Width, Height) - 1;
            var circle = new Rectangle(0, 0, diameter, diameter);

            using (var fill = new SolidBrush(Theme.Accent))
                g.FillEllipse(fill, circle);

            using (var font = new Font("Segoe UI", diameter * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                using (var number = new SolidBrush(Theme.Yellow))
                    g.DrawString(Text, font, number, new RectangleF(0, 0, diameter + 1, diameter + 1), format);
            }
        }
    }
}
