namespace Dat2DTR
{
    /// <summary>
    /// Something a page can be drawn on: the screen, a printer or a PDF page.
    /// All positions are in "points" (1 point = 1/72 inch) from the top-left corner.
    /// </summary>
    public interface IDtrCanvas
    {
        void DrawLine(double x1, double y1, double x2, double y2, double thickness);

        /// <summary>Fills a box with a colour (0xRRGGBB). Draw it BEFORE the lines and text on top of it.</summary>
        void FillRectangle(double x, double y, double width, double height, int rgb);

        /// <param name="baseline">Vertical position of the line the letters sit on.</param>
        /// <param name="rgb">Colour as 0xRRGGBB.</param>
        void DrawText(string text, double x, double baseline, double size, bool bold, int rgb);
    }

    /// <summary>
    /// Draws one Daily Time Record (Civil Service Form No. 48) on a canvas.
    ///
    /// This is the ONLY place that decides how the form looks. The preview on screen,
    /// the printout and the PDF all call this same code, so they always match.
    /// </summary>
    public static class DtrPageRenderer
    {
        private const int Black = 0x000000;
        private const int White = 0xFFFFFF;
        private const int SundayRed = 0xC00000;

        private const int BodyRows = 31;                 // the form always has room for 31 days
        private const int TotalRows = 2 + BodyRows + 1;  // 2 heading rows + days + TOTAL

        private const string Certification =
            "I CERTIFY on my honor that the above is a true and correct report of the hours of work performed, " +
            "record of which was made daily at the time of arrival at and departure from office.";

        // Faint colours for the screen only (see screenMarks): never printed.
        private const int MarkCheck = 0xFFD6D6;      // something to check (red)
        private const int MarkTyped = 0xFFF2B8;      // times typed in by hand (yellow)
        private const int MarkAdjusted = 0xFFDDB8;   // Late / Undertime typed in by hand (orange)
        private const int MarkLegendText = 0x707070;

        /// <param name="screenMarks">
        /// true only for the preview on screen: days that need checking, were typed in by hand or had
        /// their Late / Undertime typed in get a faint colour, with a small legend above the form.
        /// Printing, PDF and Excel never pass true, so the colours are never printed.
        /// </param>
        public static void Draw(IDtrCanvas canvas, DtrSheet sheet, double pageWidth, double pageHeight, bool screenMarks = false)
        {
            const double marginX = 34, marginTop = 34, marginBottom = 30;
            const double footerHeight = 150; // certification + signatures below the table

            double left = marginX;
            double right = pageWidth - marginX;
            double width = right - left;

            // ------------------------------------------------------------ heading
            double y = marginTop;
            canvas.DrawText("DAILY TIME RECORD", left, y + 11, 11.5, true, Black);
            DrawRight(canvas, "CIVIL SERVICE FORM No. 48", right, y + 9, 7.5, true);

            y += 26;
            canvas.DrawText("Name:", left, y, 9.5, true, Black);
            double nameX = left + FontMetrics.TextWidth("Name: ", 9.5, true);
            double nameSize = FitSize(sheet.NameLine, right - nameX, 10, true);
            canvas.DrawText(sheet.NameLine, nameX, y, nameSize, true, Black);

            y += 14;
            canvas.DrawText("For the month of", left, y, 8.5, false, Black);
            double monthLineStart = left + FontMetrics.TextWidth("For the month of", 8.5, false) + 8;
            double monthLineEnd = monthLineStart + 150;
            DrawCentered(canvas, sheet.MonthText, monthLineStart, monthLineEnd, y, 9.5, true, Black);
            canvas.DrawLine(monthLineStart, y + 2.5, monthLineEnd, y + 2.5, 0.6);

            y += 13;
            double hoursX = left + width * 0.66;          // where "( Regular days" starts
            double hoursValueX = hoursX + 58;             // where the underlined value starts
            canvas.DrawText("Official hours for arrival", left, y, 8, false, Black);
            canvas.DrawText("( Regular days", hoursX, y, 8, false, Black);
            DrawCentered(canvas, sheet.OfficialHours, hoursValueX, right, y, 8, true, Black);
            canvas.DrawLine(hoursValueX, y + 2.5, right, y + 2.5, 0.6);

            y += 10;
            canvas.DrawText("and departure", left + 14, y, 8, false, Black);
            canvas.DrawText("( Saturdays", hoursX, y, 8, false, Black);
            DrawCentered(canvas, "Service as required", hoursValueX, right, y, 8, true, Black);
            canvas.DrawLine(hoursValueX, y + 2.5, right, y + 2.5, 0.6);

            // ------------------------------------------------------------ table grid
            double tableTop = y + 9;
            double rowHeight = (pageHeight - marginBottom - footerHeight - tableTop) / TotalRows;
            rowHeight = Math.Max(10, Math.Min(18, rowHeight));

            double tableBottom = tableTop + rowHeight * TotalRows;
            double bodyTop = tableTop + rowHeight * 2;
            double totalTop = tableBottom - rowHeight;

            // Column edges: DAY | AM arrival | AM departure | PM arrival | PM departure
            //               | late hours | late minutes | undertime hours | undertime minutes
            const double dayWidth = 26;
            double timeWidth = (width - dayWidth) * 0.158;
            double smallWidth = (width - dayWidth - timeWidth * 4) / 4;

            var x = new double[10];
            x[0] = left;
            x[1] = left + dayWidth;
            for (int i = 2; i <= 5; i++) x[i] = x[i - 1] + timeWidth;
            for (int i = 6; i <= 8; i++) x[i] = x[i - 1] + smallWidth;
            x[9] = right;

            // ------------------------------------------------------------ screen-only colours (before the lines)
            if (screenMarks)
                DrawScreenMarks(canvas, sheet, left, right, bodyTop, rowHeight);

            // outside border
            canvas.DrawLine(left, tableTop, right, tableTop, 1.0);
            canvas.DrawLine(left, tableBottom, right, tableBottom, 1.0);
            canvas.DrawLine(left, tableTop, left, tableBottom, 1.0);
            canvas.DrawLine(right, tableTop, right, tableBottom, 1.0);

            // horizontal lines
            canvas.DrawLine(left, tableTop + rowHeight, right, tableTop + rowHeight, 0.4);
            canvas.DrawLine(left, bodyTop, right, bodyTop, 0.8);
            for (int i = 1; i < BodyRows; i++)
                canvas.DrawLine(left, bodyTop + rowHeight * i, right, bodyTop + rowHeight * i, 0.4);
            canvas.DrawLine(left, totalTop, right, totalTop, 0.8);

            // vertical lines between the groups (A.M. | P.M. | LATE | UNDERTIME)
            canvas.DrawLine(x[1], tableTop, x[1], totalTop, 0.8);
            canvas.DrawLine(x[3], tableTop, x[3], totalTop, 0.8);
            canvas.DrawLine(x[5], tableTop, x[5], tableBottom, 0.8);
            canvas.DrawLine(x[7], tableTop, x[7], tableBottom, 0.8);

            // vertical lines inside each group
            canvas.DrawLine(x[2], tableTop + rowHeight, x[2], totalTop, 0.4);
            canvas.DrawLine(x[4], tableTop + rowHeight, x[4], totalTop, 0.4);
            canvas.DrawLine(x[6], tableTop + rowHeight, x[6], tableBottom, 0.4);
            canvas.DrawLine(x[8], tableTop + rowHeight, x[8], tableBottom, 0.4);

            // ------------------------------------------------------------ table headings
            double head1 = Baseline(tableTop, rowHeight, 9);
            DrawCentered(canvas, "A. M.", x[1], x[3], head1, 9, true, Black);
            DrawCentered(canvas, "P. M.", x[3], x[5], head1, 9, true, Black);
            DrawCentered(canvas, "L A T E", x[5], x[7], head1, 8.5, true, Black);
            DrawCentered(canvas, "U N D E R T I M E", x[7], x[9], head1, 8.5, true, Black);

            double head2 = Baseline(tableTop + rowHeight, rowHeight, 7.5);
            string[] headings = { "DAY", "Arrival", "Departure", "Arrival", "Departure", "Hours", "Minutes", "Hours", "Minutes" };
            for (int i = 0; i < headings.Length; i++)
                DrawCentered(canvas, headings[i], x[i], x[i + 1], head2, i == 0 ? 7 : 7.5, true, Black);

            // ------------------------------------------------------------ one row per day
            const double cellSize = 8.5;
            const double pad = 4;

            for (int i = 0; i < sheet.Days.Count && i < BodyRows; i++)
            {
                DtrDay day = sheet.Days[i];
                double rowTop = bodyTop + rowHeight * i;
                double baseline = Baseline(rowTop, rowHeight, cellSize);

                canvas.DrawText(day.Day.ToString(), x[0] + 3, baseline, 8, true, Black);

                string label = day.RowLabel; // "A B S E N T", a leave, or a remark such as HOLIDAY
                if (label.Length > 0 && day.HasLeave)
                {
                    // A leave (VACATION LEAVE...) is centred across all four time columns.
                    DrawAcross(canvas, label, x[1], x[5], rowTop, rowHeight, baseline, cellSize);
                }
                else if (label.Length > 0)
                {
                    // Centred in the A.M. Departure column. A remark that is too long for one
                    // column is centred across the three columns up to P.M. Departure instead.
                    bool fitsOneColumn = FontMetrics.TextWidth(label, cellSize, true) <= x[3] - x[2] - pad;
                    if (fitsOneColumn)
                        DrawCentered(canvas, label, x[2], x[3], baseline, cellSize, true, Black);
                    else
                        DrawAcross(canvas, label, x[2], x[5], rowTop, rowHeight, baseline, cellSize);
                }
                else
                {
                    // Each time is centred in its own column. With a half-day leave, the half
                    // that is on leave shows the leave across its two columns instead.
                    string morningLeave = day.HalfLabel(true), afternoonLeave = day.HalfLabel(false);

                    if (morningLeave.Length > 0)
                    {
                        DrawAcross(canvas, morningLeave, x[1], x[3], rowTop, rowHeight, baseline, cellSize);
                    }
                    else
                    {
                        DrawCentered(canvas, DtrSheet.FormatFormTime(day.AmArrival), x[1], x[2], baseline, cellSize, true, Black);
                        DrawCentered(canvas, DtrSheet.FormatFormTime(day.AmDeparture), x[2], x[3], baseline, cellSize, true, Black);
                    }

                    if (afternoonLeave.Length > 0)
                    {
                        DrawAcross(canvas, afternoonLeave, x[3], x[5], rowTop, rowHeight, baseline, cellSize);
                    }
                    else
                    {
                        DrawCentered(canvas, DtrSheet.FormatFormTime(day.PmArrival), x[3], x[4], baseline, cellSize, true, Black);
                        DrawCentered(canvas, DtrSheet.FormatFormTime(day.PmDeparture), x[4], x[5], baseline, cellSize, true, Black);
                    }
                }

                DrawHoursMinutes(canvas, day.LateMinutes, x[5], x[6], x[7], baseline, cellSize, false);
                DrawHoursMinutes(canvas, day.UndertimeMinutes, x[7], x[8], x[9], baseline, cellSize, false);

                if (day.Remark.Length > 0)
                {
                    int colour = day.Remark == "Sun" ? SundayRed : Black;
                    DrawCentered(canvas, day.Remark, x[8], x[9], baseline, cellSize, true, colour);
                }
            }

            // ------------------------------------------------------------ TOTAL row
            double totalBaseline = Baseline(totalTop, rowHeight, 9.5);
            canvas.DrawText("TOTAL", left + 18, totalBaseline, 9.5, true, Black);
            DrawHoursMinutes(canvas, sheet.TotalLateMinutes, x[5], x[6], x[7], totalBaseline, 9, true);
            DrawHoursMinutes(canvas, sheet.TotalUndertimeMinutes, x[7], x[8], x[9], totalBaseline, 9, true);

            // ------------------------------------------------------------ certification
            y = tableBottom + 15;
            const double paragraphSize = 9, lineHeight = 11.5, indent = 20;
            List<string> lines = Wrap(Certification, width, indent, paragraphSize, true);
            for (int i = 0; i < lines.Count; i++)
            {
                canvas.DrawText(lines[i], left + (i == 0 ? indent : 0), y, paragraphSize, true, Black);
                y += lineHeight;
            }

            y += 6;
            canvas.DrawLine(left, y, right, y, 1.2);
            double rule = y;

            canvas.DrawText("Verified as to the prescribed office hours", left, rule + 11, 7.5, true, Black);

            // Two places to sign: at the bottom right, and in the middle below it.
            const double signatureWidth = 170;
            DrawSignature(canvas, right - signatureWidth, right, rule + 30, null);
            double middle = left + width / 2;
            DrawSignature(canvas, middle - signatureWidth / 2, middle + signatureWidth / 2, rule + 72, null);

            canvas.DrawText("(See instruction on back)", left, rule + 72, 7.5, true, Black);
        }

        /// <summary>A line to sign on, with "Signature over Printed Name" (and a title) centred under it.</summary>
        private static void DrawSignature(IDtrCanvas canvas, double x1, double x2, double lineY, string? title)
        {
            canvas.DrawLine(x1, lineY, x2, lineY, 0.6);
            DrawCentered(canvas, "Signature over Printed Name", x1, x2, lineY + 9.5, 7.5, true, Black);
            if (title != null)
                DrawCentered(canvas, title, x1, x2, lineY + 19, 7.5, true, Black);
        }

        /// <summary>Writes minutes as two numbers: hours in one column, minutes in the next.</summary>
        private static void DrawHoursMinutes(IDtrCanvas canvas, int totalMinutes,
            double x1, double x2, double x3, double baseline, double size, bool showZero)
        {
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours > 0 || showZero)
                DrawCentered(canvas, hours.ToString(), x1, x2, baseline, size, true, Black);
            if (minutes > 0 || showZero)
                DrawCentered(canvas, minutes.ToString(), x2, x3, baseline, size, true, Black);
        }

        /// <summary>
        /// Centres bold words across several columns of one row, and blanks out the
        /// column lines behind the words so they stay easy to read.
        /// </summary>
        private static void DrawAcross(IDtrCanvas canvas, string text, double x1, double x2,
            double rowTop, double rowHeight, double baseline, double size)
        {
            if (text.Length == 0) return;
            size = FitSize(text, x2 - x1 - 8, size, true);
            double textWidth = FontMetrics.TextWidth(text, size, true);
            double textX = x1 + (x2 - x1 - textWidth) / 2;

            canvas.FillRectangle(textX - 3, rowTop + 0.45, textWidth + 6, rowHeight - 0.9, White);
            canvas.DrawText(text, textX, baseline, size, true, Black);
        }

        // ---------------------------------------------------------------- text helpers
        // (also used by LeavePageRenderer, so both pages are drawn the same way)

        /// <summary>Baseline that places text of this size in the vertical middle of a row.</summary>
        /// <summary>The faint colours behind the days to look at, and a legend at the top of the page.</summary>
        private static void DrawScreenMarks(IDtrCanvas canvas, DtrSheet sheet, double left, double right, double bodyTop, double rowHeight)
        {
            bool check = false, typed = false, adjusted = false;
            for (int i = 0; i < sheet.Days.Count && i < BodyRows; i++)
            {
                DtrDay day = sheet.Days[i];
                int colour;
                if (day.Warning.Length > 0) { colour = MarkCheck; check = true; }
                else if (day.IsAdjusted) { colour = MarkAdjusted; adjusted = true; }
                else if (day.IsEdited) { colour = MarkTyped; typed = true; }
                else continue;
                canvas.FillRectangle(left, bodyTop + rowHeight * i, right - left, rowHeight, colour);
            }
            if (!check && !typed && !adjusted)
                return;

            // Legend:  On screen only (not printed):  [ ] to check   [ ] typed in by hand   [ ] Late / Undertime typed
            double y = 12, x = left;
            const double box = 7, size = 6.5;
            canvas.DrawText("On screen only, not printed:", x, y + box - 0.5, size, false, MarkLegendText);
            x += FontMetrics.TextWidth("On screen only, not printed:", size, false) + 8;
            foreach (var mark in new[]
            {
                new KeyValuePair<int, string>(check ? MarkCheck : -1, "to check"),
                new KeyValuePair<int, string>(typed ? MarkTyped : -1, "times typed in by hand"),
                new KeyValuePair<int, string>(adjusted ? MarkAdjusted : -1, "Late / Undertime typed in"),
            })
            {
                if (mark.Key < 0) continue;
                canvas.FillRectangle(x, y, box, box, mark.Key);
                canvas.DrawText(mark.Value, x + box + 3, y + box - 0.5, size, false, MarkLegendText);
                x += box + 3 + FontMetrics.TextWidth(mark.Value, size, false) + 10;
            }
        }

        internal static double Baseline(double rowTop, double rowHeight, double size)
        {
            return rowTop + rowHeight / 2 + size * 0.36;
        }

        internal static void DrawCentered(IDtrCanvas canvas, string text, double x1, double x2,
            double baseline, double size, bool bold, int rgb)
        {
            if (text.Length == 0) return;
            size = FitSize(text, x2 - x1 - 2, size, bold);
            double textWidth = FontMetrics.TextWidth(text, size, bold);
            canvas.DrawText(text, x1 + (x2 - x1 - textWidth) / 2, baseline, size, bold, rgb);
        }

        internal static void DrawRight(IDtrCanvas canvas, string text, double right, double baseline, double size, bool bold)
        {
            canvas.DrawText(text, right - FontMetrics.TextWidth(text, size, bold), baseline, size, bold, Black);
        }

        /// <summary>Makes the font smaller when the text would not fit in the space.</summary>
        internal static double FitSize(string text, double available, double size, bool bold)
        {
            double textWidth = FontMetrics.TextWidth(text, size, bold);
            if (textWidth <= available || textWidth <= 0)
                return size;
            return Math.Max(5, size * available / textWidth);
        }

        /// <summary>Breaks a paragraph into lines that fit the width.</summary>
        internal static List<string> Wrap(string text, double width, double firstLineIndent, double size, bool bold)
        {
            var lines = new List<string>();
            string current = "";

            foreach (string word in text.Split(' '))
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                double available = width - (lines.Count == 0 ? firstLineIndent : 0);

                if (current.Length > 0 && FontMetrics.TextWidth(candidate, size, bold) > available)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }

            if (current.Length > 0)
                lines.Add(current);
            return lines;
        }
    }
}
