using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Dat2DTR
{
    /// <summary>
    /// The big area on the right of the main window. It shows the DTR (or the
    /// Computation of Leaves) exactly as it will be printed, on a white
    /// "sheet of paper" that can be scrolled.
    /// </summary>
    public class DtrPreviewControl : ScrollableControl
    {
        private const int Gap = 24;           // grey space around the paper, in pixels
        private const float MaxZoom = 1.75f;  // never draw the paper bigger than this (pixels per point)

        private DtrSheet? _sheet;
        private LeaveCard? _card;             // the Computation of Leaves of the same employee
        private bool _showLeave;              // true = show the Computation of Leaves instead of the DTR
        private DtrPaper _paper = DtrPaper.A4;
        private float _zoom = 1f;             // pixels per point
        private string _emptyTitle = "No DTR to show yet";
        private string _emptyText = "";
        private Image? _emptyImage;           // logo shown above the message when there is no DTR

        public DtrPreviewControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            AutoScroll = true;
            TabStop = true;
            BackColor = Theme.PreviewBackground;
        }

        /// <summary>Shows a DTR, or the "empty" message when sheet is null.</summary>
        public void ShowSheet(DtrSheet? sheet)
        {
            ShowPages(sheet, null, false);
        }

        /// <summary>
        /// Shows one of the two pages of an employee: the DTR, or (when showLeave is true and
        /// there is a leave card) the Computation of Leaves. sheet = null shows the "empty" message.
        /// </summary>
        public void ShowPages(DtrSheet? sheet, LeaveCard? card, bool showLeave)
        {
            bool isNewEmployee = sheet == null || _sheet == null || sheet.EmployeeId != _sheet.EmployeeId;
            bool isOtherPage = showLeave != _showLeave;
            _sheet = sheet;
            _card = card;
            _showLeave = showLeave;
            UpdatePageSize();
            if (isNewEmployee || isOtherPage)
                AutoScrollPosition = new Point(0, 0);
            Invalidate();
        }

        public void SetPaper(DtrPaper paper)
        {
            _paper = paper;
            UpdatePageSize();
            Invalidate();
        }

        /// <summary>The words shown in the middle when there is no DTR to show.</summary>
        public void SetEmptyMessage(string title, string text)
        {
            _emptyTitle = title;
            _emptyText = text;
            if (_sheet == null)
                Invalidate();
        }

        /// <summary>A logo to show above the message when there is no DTR to show.</summary>
        public void SetEmptyImage(Image? image)
        {
            _emptyImage = image;
            if (_sheet == null)
                Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdatePageSize();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus(); // so the mouse wheel scrolls the page
        }

        /// <summary>Makes the paper as wide as the control allows and sets up the scroll bars.</summary>
        private void UpdatePageSize()
        {
            if (_sheet == null)
            {
                AutoScrollMinSize = Size.Empty;
                return;
            }

            // Always leave room for the vertical scroll bar. Otherwise the paper
            // could flip between two sizes while the window is being resized.
            int available = Width - SystemInformation.VerticalScrollBarWidth - Gap * 2;
            float zoom = (float)(available / PaperInfo.WidthOf(_paper));
            _zoom = Math.Max(0.5f, Math.Min(MaxZoom, zoom));

            AutoScrollMinSize = new Size(
                (int)(PaperInfo.WidthOf(_paper) * _zoom) + Gap * 2,
                (int)(PaperInfo.HeightOf(_paper) * _zoom) + Gap * 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (_sheet == null)
            {
                PaintEmptyMessage(g);
                return;
            }

            float pageWidth = (float)PaperInfo.WidthOf(_paper) * _zoom;
            float pageHeight = (float)PaperInfo.HeightOf(_paper) * _zoom;

            // Centre the paper, and move it by how far the user has scrolled.
            float x = Math.Max(Gap, (ClientSize.Width - pageWidth) / 2f) + AutoScrollPosition.X;
            float y = Gap + AutoScrollPosition.Y;

            using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillRectangle(shadow, x + 3, y + 3, pageWidth, pageHeight);
            g.FillRectangle(Brushes.White, x, y, pageWidth, pageHeight);
            using (var border = new Pen(Theme.Line))
                g.DrawRectangle(border, x, y, pageWidth, pageHeight);

            // From here on, 1 unit = 1 point on the paper.
            GraphicsState saved = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(_zoom, _zoom);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var canvas = new GdiCanvas(g))
            {
                if (_showLeave && _card != null)
                    LeavePageRenderer.Draw(canvas, _card, PaperInfo.WidthOf(_paper), PaperInfo.HeightOf(_paper));
                else
                    DtrPageRenderer.Draw(canvas, _sheet, PaperInfo.WidthOf(_paper), PaperInfo.HeightOf(_paper), true);   // with the screen-only colours
            }

            g.Restore(saved);
        }

        private void PaintEmptyMessage(Graphics g)
        {
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var titleFont = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (var textFont = new Font("Segoe UI", 10.5f))
            using (var titleBrush = new SolidBrush(Theme.TextStrong))
            using (var textBrush = new SolidBrush(Theme.TextMuted))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Far;

                int middle = ClientSize.Height / 2;

                // The logo sits above the words. It is left out when the window is too small for it.
                if (_emptyImage != null)
                {
                    float logoWidth = Math.Min(ClientSize.Width - Gap * 2, titleFont.Height * 15f);
                    float logoHeight = logoWidth * _emptyImage.Height / _emptyImage.Width;
                    float space = titleFont.Height * 0.9f;                  // gap between logo and words
                    float wordsHeight = titleFont.Height + textFont.Height * 3f;

                    if (logoWidth > 120 && logoHeight + space + wordsHeight < ClientSize.Height - Gap * 2)
                    {
                        middle += (int)((logoHeight + space) / 2);
                        float top = middle - 4 - titleFont.Height - space - logoHeight;
                        LogoBox.DrawSmooth(g, _emptyImage,
                            new RectangleF((ClientSize.Width - logoWidth) / 2, top, logoWidth, logoHeight));
                    }
                }

                var titleArea = new RectangleF(Gap, 0, ClientSize.Width - Gap * 2, middle - 4);
                g.DrawString(_emptyTitle, titleFont, titleBrush, titleArea, format);

                format.LineAlignment = StringAlignment.Near;
                var textArea = new RectangleF(Gap, middle + 4, ClientSize.Width - Gap * 2, middle - 4);
                g.DrawString(_emptyText, textFont, textBrush, textArea, format);
            }
        }
    }
}
