using System.Drawing.Drawing2D;

namespace Dat2DTR
{
    /// <summary>
    /// This is the "Edit Certificate Details" window: the names and positions printed on the two places to sign of the
    /// Certificate of Leave Credits - "Prepared by" and "Certified correct". A box left empty is left blank on the
    /// certificate, to fill in by hand. Only these four things can be changed here.
    /// On the right is a live preview of the certificate: it changes with every letter typed.
    /// </summary>
    internal sealed class CertificateDetailsForm : Form
    {
        private readonly TextBox _preparedName = new TextBox(), _preparedPosition = new TextBox();
        private readonly TextBox _certifiedName = new TextBox(), _certifiedPosition = new TextBox();

        // This is the live preview: the certificate of the employee on screen (or a sample), drawn with what is typed.
        private readonly PreviewBox _preview = new PreviewBox();
        private readonly RadioButton _signaturesOnly = new RadioButton(), _wholePage = new RadioButton();
        private readonly LeaveCertificate _sample;

        /// <param name="sample">This is the certificate shown in the preview (the employee on screen), or null for a sample one.</param>
        public CertificateDetailsForm(string preparedName, string preparedPosition, string certifiedName, string certifiedPosition,
                                      LeaveCertificate? sample = null)
        {
            _sample = sample ?? SampleCertificate();
            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Edit Certificate Details";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;

            const int margin = 20, width = 440, labelWidth = 80;
            int y = margin;

            var title = new Label();
            title.Text = "Certificate of Leave Credits: who signs";
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.TextStrong;
            title.SetBounds(margin, y, width, 28);
            Controls.Add(title);
            y += 30;

            var how = new Label();
            how.Text = "The name is printed above \"Signature over Printed Name\", the position above \"Position\". " +
                       "Leave a box empty to have it blank on the certificate, to fill in by hand.";
            how.ForeColor = Theme.TextMuted;
            int howHeight = TextRenderer.MeasureText(how.Text, Font, new Size(width, 0), TextFormatFlags.WordBreak).Height + 4;
            how.SetBounds(margin, y, width, howHeight);
            Controls.Add(how);
            y += howHeight + 10;

            y = AddGroup("Prepared by", _preparedName, preparedName, _preparedPosition, preparedPosition, y, margin, width, labelWidth);
            y = AddGroup("Certified correct", _certifiedName, certifiedName, _certifiedPosition, certifiedPosition, y, margin, width, labelWidth);

            var original = new Button();
            original.Text = "Back to the original";
            original.SetBounds(margin, y + 8, 150, 32);
            original.Click += (sender, e) =>
            {
                _preparedName.Text = LeaveCertificate.DefaultPreparedByName;
                _preparedPosition.Text = LeaveCertificate.DefaultPreparedByPosition;
                _certifiedName.Text = "";
                _certifiedPosition.Text = "";
            };
            Controls.Add(original);

            var save = new Button();
            save.Text = "Save";
            save.SetBounds(margin + width - 96 - 8 - 96, y + 8, 96, 32);
            save.Click += (sender, e) => DialogResult = DialogResult.OK;
            Theme.StylePrimary(save);
            Controls.Add(save);
            AcceptButton = save;

            var cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(margin + width - 96, y + 8, 96, 32);
            Controls.Add(cancel);
            CancelButton = cancel;

            // This is the live preview on the right: the signatures (big enough to read) or the whole page.
            int previewX = margin + width + margin, previewWidth = 520;
            var previewTitle = new Label();
            previewTitle.Text = "Live preview";
            previewTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            previewTitle.ForeColor = Theme.Navy;
            previewTitle.SetBounds(previewX, margin + 2, 120, 24);
            Controls.Add(previewTitle);

            _signaturesOnly.Text = "Signatures";
            _signaturesOnly.Checked = true;
            _signaturesOnly.SetBounds(previewX + 130, margin + 2, 100, 24);
            _signaturesOnly.CheckedChanged += (sender, e) => _preview.Invalidate();
            Controls.Add(_signaturesOnly);
            _wholePage.Text = "Whole page";
            _wholePage.SetBounds(previewX + 236, margin + 2, 110, 24);
            _wholePage.CheckedChanged += (sender, e) => _preview.Invalidate();
            Controls.Add(_wholePage);

            int previewTop = margin + 30, previewHeight = Math.Max(y + 8 + 32 - previewTop, 520);
            _preview.SetBounds(previewX, previewTop, previewWidth, previewHeight);
            _preview.Paint += Preview_Paint;
            Controls.Add(_preview);

            foreach (TextBox box in new[] { _preparedName, _preparedPosition, _certifiedName, _certifiedPosition })
                box.TextChanged += (sender, e) => _preview.Invalidate();   // This is the preview following every letter typed

            ClientSize = new Size(previewX + previewWidth + margin, previewTop + previewHeight + margin);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>This is a made-up certificate for the preview when no employee is on screen.</summary>
        private static LeaveCertificate SampleCertificate()
        {
            var c = new LeaveCertificate();
            c.EmployeeId = "0";
            c.EmployeeName = "DELA CRUZ, JUAN (sample)";
            c.Issued = DateTime.Today;
            c.AsOf = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
            c.Vl = 15.000m;
            c.Sl = 10.000m;
            c.ForcedLeft = 5;
            c.SplLeft = 3;
            c.WellnessLeft = 5;
            return c;
        }

        /// <summary>
        /// This is where the preview is drawn: the certificate as it will be printed, with the names and positions typed so
        /// far. "Signatures" shows only the part with the two places to sign, zoomed in; "Whole page" shows the whole page.
        /// </summary>
        private void Preview_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Panel);
            string[] typed = GetValues();
            _sample.PreparedName = typed[0];
            _sample.PreparedPosition = typed[1];
            _sample.CertifiedName = typed[2];
            _sample.CertifiedPosition = typed[3];

            const double pageWidth = 595.28, pageHeight = 841.89;   // This is A4, in points
            double signaturesTop = CertificateRenderer.Draw(new NoCanvas(), _sample, pageWidth, pageHeight);
            RectangleF area = _wholePage.Checked
                ? new RectangleF(0, 0, (float)pageWidth, (float)pageHeight)
                : new RectangleF(60, (float)signaturesTop - 22, (float)pageWidth - 120, 140);

            // This is the part of the page fitted in the box, with a small gap all around.
            const float gap = 10;
            float scale = Math.Min((_preview.ClientSize.Width - gap * 2) / area.Width, (_preview.ClientSize.Height - gap * 2) / area.Height);
            if (scale <= 0) return;
            float left = (_preview.ClientSize.Width - area.Width * scale) / 2, top = (_preview.ClientSize.Height - area.Height * scale) / 2;
            using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillRectangle(shadow, left + 3, top + 3, area.Width * scale, area.Height * scale);
            g.FillRectangle(Brushes.White, left, top, area.Width * scale, area.Height * scale);

            GraphicsState state = g.Save();
            g.SetClip(new RectangleF(left, top, area.Width * scale, area.Height * scale));
            g.TranslateTransform(left, top);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-area.X, -area.Y);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            using (var canvas = new GdiCanvas(g))
                CertificateRenderer.Draw(canvas, _sample, pageWidth, pageHeight);
            g.Restore(state);
        }

        /// <summary>This is a box that draws without flicker (the preview is drawn again with every letter typed).</summary>
        private sealed class PreviewBox : Panel
        {
            public PreviewBox()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                BorderStyle = BorderStyle.FixedSingle;
            }
        }

        /// <summary>
        /// This is a canvas that draws nothing: used once to find where the signatures are, with the same fonts and logo
        /// as the real drawing (so the positions are the same).
        /// </summary>
        private sealed class NoCanvas : IDtrCanvas, IDtrFontCanvas, IDtrImageCanvas
        {
            public void DrawLine(double x1, double y1, double x2, double y2, double thickness) { }
            public void FillRectangle(double x, double y, double width, double height, int rgb) { }
            public void DrawText(string text, double x, double baseline, double size, bool bold, int rgb) { }
            public void DrawText(string text, double x, double baseline, double size, TrueTypeFont font, int rgb) { }
            public void DrawImage(JpegImage image, double x, double y, double width, double height) { }
        }

        /// <summary>This is what was typed, after Save: prepared-by name, prepared-by position, certified-by name, certified-by position.</summary>
        public string[] GetValues()
        {
            return new[] { _preparedName.Text.Trim(), _preparedPosition.Text.Trim(), _certifiedName.Text.Trim(), _certifiedPosition.Text.Trim() };
        }

        /// <summary>This is one place to sign: a heading, then the Name and Position boxes. Returns where it ends.</summary>
        private int AddGroup(string heading, TextBox name, string nameText, TextBox position, string positionText,
                             int y, int x, int width, int labelWidth)
        {
            var head = new Label();
            head.Text = heading;
            head.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            head.ForeColor = Theme.Navy;
            head.SetBounds(x, y, width, 22);
            Controls.Add(head);
            y += 26;

            AddBox("Name", name, nameText, y, x, width, labelWidth);
            y += 30;
            AddBox("Position", position, positionText, y, x, width, labelWidth);
            return y + 34;
        }

        private void AddBox(string label, TextBox box, string text, int y, int x, int width, int labelWidth)
        {
            var caption = new Label();
            caption.Text = label;
            caption.TextAlign = ContentAlignment.MiddleLeft;
            caption.SetBounds(x, y, labelWidth, 23);
            Controls.Add(caption);

            box.Text = text;
            box.MaxLength = 80;
            box.SetBounds(x + labelWidth, y, width - labelWidth, 23);
            Controls.Add(box);
        }
    }
}
