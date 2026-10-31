namespace Dat2DTR
{
    /// <summary>
    /// This is where the Certificate of Leave Credits of one employee is drawn on a canvas: the DepEd logo and
    /// the heading of the office, a table of every leave as of a date (Vacation Leave, Sick Leave and their total,
    /// Forced Leave, Special Privilege Leave, Wellness Leave, CTO and Solo Parent Leave), and two places to sign.
    /// A balance that is not known yet is left as an empty box to fill in by hand. The paragraphs are justified.
    /// The printout and the PDF both call this same code.
    /// </summary>
    public static class CertificateRenderer
    {
        private const int Black = 0x000000;
        private const int HeadingFill = 0xE6E8F2;   // This is light grey-blue behind the table headings
        private const int TotalFill = 0xF3F4F8;     // This is the lighter grey-blue behind the total

        /// <summary>This is the whole certificate. Returns where the two places to sign start ("Prepared by:"), for the live preview.</summary>
        public static double Draw(IDtrCanvas canvas, LeaveCertificate c, double pageWidth, double pageHeight)
        {
            double left = 72, right = pageWidth - 72, width = right - left;
            double middle = (left + right) / 2;

            // This is the heading of the office ------------------------------------------------------------
            // Old English Text MT for "Republic of the Philippines" and "Department of Education", Trajan Pro for
            // "NATIONAL CAPITAL REGION" - when those fonts are found (see CertificateFonts); otherwise Helvetica / Arial.
            // This is the DepEd logo first, centred above "Republic of the Philippines".
            double y = 78;
            JpegImage? logo = CertificateLogo.Get();
            var pictures = canvas as IDtrImageCanvas;
            if (logo != null && pictures != null)
            {
                const double logoSize = 64, logoTop = 34;
                pictures.DrawImage(logo, middle - logoSize / 2, logoTop, logoSize, logoSize);
                y = logoTop + logoSize + 18;
            }
            TrueTypeFont? oldEnglish = CertificateFonts.Find(CertificateFonts.OldEnglish);
            TrueTypeFont? trajan = CertificateFonts.Find(CertificateFonts.Trajan);
            var fonts = canvas as IDtrFontCanvas;
            if (fonts == null) { oldEnglish = null; trajan = null; }

            if (oldEnglish != null)
            {
                Centered(fonts!, "Republic of the Philippines", oldEnglish, 12, middle, y);
                y += 22;
                Centered(fonts!, "Department of Education", oldEnglish, 18, middle, y);
            }
            else
            {
                DtrPageRenderer.DrawCentered(canvas, "Republic of the Philippines", left, right, y, 10, false, Black);
                y += 16;
                DtrPageRenderer.DrawCentered(canvas, "DEPARTMENT OF EDUCATION", left, right, y, 12.5, true, Black);
            }
            y += oldEnglish != null ? 17 : 15;
            if (trajan != null)
                Centered(fonts!, "NATIONAL CAPITAL REGION", trajan, 10.5, middle, y);
            else
                DtrPageRenderer.DrawCentered(canvas, "National Capital Region", left, right, y, 10, false, Black);
            y += 12;
            canvas.DrawLine(left, y, right, y, 1.2);

            // This is the title ------------------------------------------------------------
            y += 52;
            DtrPageRenderer.DrawCentered(canvas, "CERTIFICATE OF LEAVE CREDITS", left, right, y, 16, true, Black);

            // This is the first paragraph: who, and as of which date ------------------------------------------------------------
            string who = (c.EmployeeName.Length > 0 ? c.EmployeeName : "ID " + c.EmployeeId) +
                         (c.EmployeeName.Length > 0 ? " (biometric ID " + c.EmployeeId + ")" : "");
            y += 44;
            y = Paragraph(canvas, "This is to certify that " + who + " has the following leave credits as of " +
                          KeepTogether(LeaveCertificate.LongDate(c.AsOf)) + ", based on the leave records of this office:", left, width, y);

            // This is the table of every leave: one line each, the days left on the right ------------------------------------------------------------
            y += 20;
            double tableWidth = Math.Min(340, width);
            double tx = middle - tableWidth / 2;
            double rowHeight = 19, headHeight = 22;
            double numberColumn = 110, split = tx + tableWidth - numberColumn;

            var rows = new List<KeyValuePair<string, string?>>();
            rows.Add(Row("Vacation Leave", c.Vl.HasValue ? LeaveRules.Format(c.Vl.Value) : null));
            rows.Add(Row("Sick Leave", c.Sl.HasValue ? LeaveRules.Format(c.Sl.Value) : null));
            rows.Add(Row("Total (Vacation + Sick Leave)", c.Total.HasValue ? LeaveRules.Format(c.Total.Value) : null));
            rows.Add(Row("Mandatory / Forced Leave", Days(c.ForcedLeft)));
            rows.Add(Row("Special Privilege Leave", Days(c.SplLeft)));
            rows.Add(Row("Wellness Leave", Days(c.WellnessLeft)));
            rows.Add(Row("Compensatory Time-Off (CTO)", Days(c.Cto)));
            rows.Add(Row("Solo Parent Leave", Days(c.Solo)));
            const int totalRow = 2;   // This is the line of the total, in bold with a light background

            double tableHeight = headHeight + rowHeight * rows.Count;
            canvas.FillRectangle(tx, y, tableWidth, headHeight, HeadingFill);
            canvas.FillRectangle(tx, y + headHeight + rowHeight * totalRow, tableWidth, rowHeight, TotalFill);
            canvas.DrawText("Kind of Leave", tx + 8, DtrPageRenderer.Baseline(y, headHeight, 10), 10, true, Black);
            DtrPageRenderer.DrawCentered(canvas, "Balance (days)", split, tx + tableWidth, DtrPageRenderer.Baseline(y, headHeight, 10), 10, true, Black);
            for (int r = 0; r < rows.Count; r++)
            {
                double top = y + headHeight + rowHeight * r;
                bool bold = r == totalRow;
                canvas.DrawText(rows[r].Key, tx + 8, DtrPageRenderer.Baseline(top, rowHeight, 10), 10, bold, Black);
                if (rows[r].Value != null)
                    DtrPageRenderer.DrawCentered(canvas, rows[r].Value!, split, tx + tableWidth, DtrPageRenderer.Baseline(top, rowHeight, 10.5), 10.5, true, Black);
            }

            // This is the lines of the table: a heavier line around it and under the total.
            canvas.DrawLine(tx, y, tx + tableWidth, y, 1.0);
            canvas.DrawLine(tx, y + headHeight, tx + tableWidth, y + headHeight, 0.8);
            for (int r = 1; r < rows.Count; r++)
            {
                double lineY = y + headHeight + rowHeight * r;
                canvas.DrawLine(tx, lineY, tx + tableWidth, lineY, r == totalRow + 1 ? 0.9 : 0.4);
            }
            canvas.DrawLine(tx, y + tableHeight, tx + tableWidth, y + tableHeight, 1.0);
            canvas.DrawLine(tx, y, tx, y + tableHeight, 1.0);
            canvas.DrawLine(split, y, split, y + tableHeight, 0.6);
            canvas.DrawLine(tx + tableWidth, y, tx + tableWidth, y + tableHeight, 1.0);
            y += tableHeight;

            // This is the second paragraph and the date issued ------------------------------------------------------------
            y += 36;
            y = Paragraph(canvas, "This certification is issued upon the request of the above-named employee for whatever " +
                          "legal purpose it may serve.", left, width, y);
            y += 22;
            y = Paragraph(canvas, "Issued this " + KeepTogether(LeaveCertificate.IssuedText(c.Issued)) + ".", left, width, y);

            // This is the two places to sign ------------------------------------------------------------
            y += 46;
            double signaturesTop = y;
            double signWidth = 190;
            double leftSign = left, rightSign = right - signWidth;
            canvas.DrawText("Prepared by:", leftSign, y, 10, false, Black);
            canvas.DrawText("Certified correct:", rightSign, y, 10, false, Black);
            y += 48;
            DrawSignature(canvas, leftSign, leftSign + signWidth, y, c.PreparedName, c.PreparedPosition);
            DrawSignature(canvas, rightSign, rightSign + signWidth, y, c.CertifiedName, c.CertifiedPosition);
            return signaturesTop;
        }

        /// <summary>This is a line of text in a font from a file, centred on the middle of the page.</summary>
        private static void Centered(IDtrFontCanvas canvas, string text, TrueTypeFont font, double size, double middle, double baseline)
        {
            canvas.DrawText(text, middle - font.TextWidth(text, size) / 2, baseline, size, font, Black);
        }

        /// <summary>
        /// This is a paragraph broken into lines that fit the width, JUSTIFIED: every line but the last reaches both
        /// the left and the right margin (the space between the words is made wider). The first line is indented.
        /// Returns where its last line ended.
        /// </summary>
        private static double Paragraph(IDtrCanvas canvas, string text, double left, double width, double y)
        {
            const double size = 11, lineHeight = 17, indent = 36;
            List<string> lines = DtrPageRenderer.Wrap(text, width, indent, size, false);
            for (int i = 0; i < lines.Count; i++)
            {
                double x = left + (i == 0 ? indent : 0);
                double available = width - (i == 0 ? indent : 0);
                string[] words = lines[i].Split(' ');
                if (i == lines.Count - 1 || words.Length < 2)
                {
                    canvas.DrawText(lines[i].Replace(NoBreak, ' '), x, y, size, false, Black);   // This is the last line: not stretched
                }
                else
                {
                    double wordsWidth = 0;
                    foreach (string word in words)
                        wordsWidth += FontMetrics.TextWidth(word.Replace(NoBreak, ' '), size, false);
                    double gap = (available - wordsWidth) / (words.Length - 1);
                    foreach (string word in words)
                    {
                        string shown = word.Replace(NoBreak, ' ');
                        canvas.DrawText(shown, x, y, size, false, Black);
                        x += FontMetrics.TextWidth(shown, size, false) + gap;
                    }
                }
                if (i < lines.Count - 1) y += lineHeight;
            }
            return y;
        }

        /// <summary>This is one line of the table of leaves: the kind, and the days (null = left empty, to fill in by hand).</summary>
        private static KeyValuePair<string, string?> Row(string kind, string? days)
        {
            return new KeyValuePair<string, string?>(kind, days);
        }

        /// <summary>This is a number of days for the table, e.g. "5", "2.5", "0" (null stays null).</summary>
        private static string? Days(decimal? days)
        {
            return days.HasValue ? LeaveRules.FormatShort(days.Value) : null;
        }

        /// <summary>This is the no-break space: words joined with it are never split over two lines.</summary>
        private const char NoBreak = '\u00A0';

        /// <summary>This is a date (or other words) kept on one line, e.g. "October 31, 2026".</summary>
        private static string KeepTogether(string text)
        {
            return text.Replace(' ', NoBreak);
        }

        /// <summary>
        /// This is a line to sign on with "Signature over Printed Name" under it, and a line with "Position" under it.
        /// The name and the position are printed on the lines ("" = left empty, to fill in by hand).
        /// </summary>
        private static void DrawSignature(IDtrCanvas canvas, double x1, double x2, double lineY, string name, string position)
        {
            if (name.Length > 0)
                DtrPageRenderer.DrawCentered(canvas, name, x1, x2, lineY - 4, DtrPageRenderer.FitSize(name, x2 - x1, 10.5, true), true, Black);
            canvas.DrawLine(x1, lineY, x2, lineY, 0.8);
            DtrPageRenderer.DrawCentered(canvas, "Signature over Printed Name", x1, x2, lineY + 12, 8.5, false, Black);
            if (position.Length > 0)
                DtrPageRenderer.DrawCentered(canvas, position, x1, x2, lineY + 36, DtrPageRenderer.FitSize(position, x2 - x1, 10, false), false, Black);
            canvas.DrawLine(x1, lineY + 40, x2, lineY + 40, 0.8);
            DtrPageRenderer.DrawCentered(canvas, "Position", x1, x2, lineY + 52, 8.5, false, Black);
        }
    }
}
