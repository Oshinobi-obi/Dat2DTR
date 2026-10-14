namespace Dat2DTR
{
    /// <summary>
    /// Draws the "Computation of Leaves" page of one employee on a canvas:
    /// the leave card of the year (one row per month, the same columns as the
    /// leave card kept by Personnel) and, below it, how the open month was computed.
    ///
    /// Like the DTR, this is the ONLY place that decides how the page looks.
    /// The preview on screen, the printout and the PDF all call this same code.
    /// </summary>
    public static class LeavePageRenderer
    {
        private const int Black = 0x000000;
        private const int Grey = 0x505050;
        private const int HeadingFill = 0xE6E8F2;   // light grey-blue behind the table headings
        private const int CurrentFill = 0xFFF2B3;   // pale yellow behind the open month

        // Widths of the 14 columns (they are stretched to the width of the paper):
        // Month | VACATION: Earned, Enjoyed, Net, Leave Date/s, SPL, WL, CTO, Late, Undertime
        //       | SICK: Earned, Enjoyed, Net, Leave Date/s
        private static readonly double[] ColumnWidths = { 46, 34, 36, 38, 62, 24, 24, 26, 32, 40, 34, 36, 38, 50 };

        /// <summary>Column number (in ColumnWidths) of the first SICK column.</summary>
        private const int FirstSickColumn = 10;

        private static readonly string[] ColumnNames =
        {
            "", "Earned", "Enjoyed", "Net", "Leave Date/s", "SPL", "WL", "CTO", "Late", "Undertime",
            "Earned", "Enjoyed", "Net", "Leave Date/s"
        };

        public static void Draw(IDtrCanvas canvas, LeaveCard card, double pageWidth, double pageHeight)
        {
            const double marginX = 34, marginTop = 34;

            double left = marginX;
            double right = pageWidth - marginX;
            double width = right - left;

            // ------------------------------------------------------------ heading
            double y = marginTop;
            canvas.DrawText("COMPUTATION OF LEAVES", left, y + 11, 11.5, true, Black);
            DtrPageRenderer.DrawRight(canvas, "For the month of " + card.MonthText, right, y + 9, 8.5, true);

            // Under "For the month of": the Vacation Leave and Sick Leave left at the end of the month,
            // each written on a line (left empty when the balance is not known yet).
            const double balanceWidth = 150;
            double balanceLeft = right - balanceWidth;
            DrawBalance(canvas, "VL", card.Current.VlNet, balanceLeft, right, y + 26);
            DrawBalance(canvas, "SL", card.Current.SlNet, balanceLeft, right, y + 40);

            y += 26;
            canvas.DrawText("Name:", left, y, 9.5, true, Black);
            double nameX = left + FontMetrics.TextWidth("Name: ", 9.5, true);
            double nameSize = DtrPageRenderer.FitSize(card.NameLine, balanceLeft - 12 - nameX, 10, true);
            canvas.DrawText(card.NameLine, nameX, y, nameSize, true, Black);

            y += 14;
            string scheduleLine = card.ScheduleText + ".   " + card.DayChargeText;
            canvas.DrawText(scheduleLine, left, y, DtrPageRenderer.FitSize(scheduleLine, balanceLeft - 12 - left, 8.5, false), false, Black);

            // ------------------------------------------------------------ the leave card of the year
            y = DrawCardTable(canvas, card, left, width, y + 10);

            // ------------------------------------------------------------ used this year, under the table
            y += 13;
            string months = card.MonthStart.Month == 1 ? "January" : "January to " + card.Current.MonthName;
            string used = "Used this year (" + months + "):     " +
                          "Special Privilege Leave  " + UsedOf(card.SplUsed, LeaveType.SpecialPrivilege) + "          " +
                          "Forced Leave  " + UsedOf(card.ForcedUsed, LeaveType.Forced) + "          " +
                          "Wellness Leave  " + UsedOf(card.WellnessUsed, LeaveType.Wellness) + "          " +
                          "CTO  " + LeaveRules.FormatShort(card.CtoUsed);
            canvas.DrawText(used, left, y, DtrPageRenderer.FitSize(used, width, 8, true), true, Black);

            // ------------------------------------------------------------ how the month was computed
            // Two columns: Vacation and Sick Leave on the left; SPL, Wellness Leave and CTO on the right.
            y += 24;
            canvas.DrawText("HOW " + card.MonthText.ToUpperInvariant() + " WAS COMPUTED", left, y, 9.5, true, Black);
            canvas.DrawLine(left, y + 4, right, y + 4, 0.8);
            y += 6;

            int split = card.Breakdown.FindIndex(l => l.Kind == LeaveLineKind.Heading && l.Text.StartsWith("SPECIAL PRIVILEGE"));
            if (split < 0) split = card.Breakdown.Count;

            const double gap = 24;
            double columnWidth = (width - gap) / 2;
            double endLeft = DrawLines(canvas, card.Breakdown.GetRange(0, split), left, columnWidth, y);
            double endRight = DrawLines(canvas, card.Breakdown.GetRange(split, card.Breakdown.Count - split),
                                        left + columnWidth + gap, columnWidth, y);
            y = Math.Max(endLeft, endRight);

            // ------------------------------------------------------------ small print at the bottom
            // (left out when the page is so full that it would not fit any more)
            double foot = Math.Max(y + 26, pageHeight - 46);
            if (foot + 16 > pageHeight - 12)
                return;

            const string foot1 = "Late and undertime are turned into days with the CSC table of fractions of a day " +
                                 "(8 hours = 1 day,  1 hour = .125,  1 minute = .002) and are deducted from Vacation Leave.";
            const string foot2 = "Enjoyed = what was taken from the credits.   Net = balance at the end of the month.   " +
                                 "SPL, WL and CTO are in days.   Prepared with Dat2DTR - compare with the employee's leave card.";

            canvas.DrawLine(left, foot - 10, right, foot - 10, 0.4);
            canvas.DrawText(foot1, left, foot, DtrPageRenderer.FitSize(foot1, width, 7, false), false, Grey);
            canvas.DrawText(foot2, left, foot + 9.5, DtrPageRenderer.FitSize(foot2, width, 7, false), false, Grey);
        }

        /// <summary>Writes lines of the computation in one column. Returns where the last line ended.</summary>
        private static double DrawLines(IDtrCanvas canvas, List<LeaveLine> lines, double x, double columnWidth, double y)
        {
            const double lineHeight = 11.5, textSize = 8, indent = 10, valueWidth = 64;
            double valueRight = x + columnWidth;
            double textRoom = columnWidth - indent - valueWidth - 4;

            foreach (LeaveLine line in lines)
            {
                if (line.Kind == LeaveLineKind.Heading)
                {
                    y += lineHeight + 5;
                    canvas.DrawText(line.Text, x, y, DtrPageRenderer.FitSize(line.Text, columnWidth, textSize, true), true, Black);
                    continue;
                }

                y += lineHeight;
                bool total = line.Kind == LeaveLineKind.Total;
                if (total)
                    canvas.DrawLine(valueRight - 54, y - 9, valueRight, y - 9, 0.6);

                double room = line.Value.Length == 0 ? columnWidth - indent : textRoom;
                canvas.DrawText(line.Text, x + indent, y, DtrPageRenderer.FitSize(line.Text, room, textSize, total), total, Black);
                if (line.Value.Length > 0)
                    canvas.DrawText(line.Value, valueRight - FontMetrics.TextWidth(line.Value,
                        DtrPageRenderer.FitSize(line.Value, valueWidth, textSize, total), total),
                        y, DtrPageRenderer.FitSize(line.Value, valueWidth, textSize, total), total, Black);
            }
            return y;
        }

        /// <summary>
        /// Centres text in a cell of the table. Text that is too long is first made smaller, and
        /// if that is not enough it is cut short with "..." (the full list of days is in the
        /// computation below the table), so it never runs into the next column.
        /// </summary>
        private static void DrawCell(IDtrCanvas canvas, string text, double x1, double x2, double baseline, double size, bool bold)
        {
            if (text.Length == 0) return;

            const double smallest = 5.5;
            double room = x2 - x1 - 3;
            double textWidth = FontMetrics.TextWidth(text, size, bold);
            if (textWidth > room)
                size = Math.Max(smallest, size * room / textWidth);

            while (text.Length > 4 && FontMetrics.TextWidth(text, size, bold) > room)
            {
                // drop the last entry of the list (or, if there is no comma, a few letters)
                int cut = text.LastIndexOf(',', text.Length - 5);
                text = (cut > 0 ? text.Substring(0, cut) : text.Substring(0, text.Length - 5)) + " ...";
            }

            textWidth = FontMetrics.TextWidth(text, size, bold);
            canvas.DrawText(text, x1 + (x2 - x1 - textWidth) / 2, baseline, size, bold, Black);
        }

        /// <summary>Draws the 12-month table and returns the position of its bottom edge.</summary>
        private static double DrawCardTable(IDtrCanvas canvas, LeaveCard card, double left, double width, double top)
        {
            const double rowHeight = 15;
            const int headRows = 2, monthRows = 12;

            // column edges
            double total = ColumnWidths.Sum();
            var x = new double[ColumnWidths.Length + 1];
            x[0] = left;
            for (int i = 0; i < ColumnWidths.Length; i++)
                x[i + 1] = x[i] + ColumnWidths[i] * width / total;
            double right = x[ColumnWidths.Length];

            double bodyTop = top + rowHeight * headRows;
            double bottom = bodyTop + rowHeight * monthRows;

            // ---- coloured backgrounds first, so the lines and the text go on top
            canvas.FillRectangle(left, top, width, rowHeight * headRows, HeadingFill);
            int current = card.MonthStart.Month - 1;
            canvas.FillRectangle(left, bodyTop + rowHeight * current, width, rowHeight, CurrentFill);

            // ---- lines
            canvas.DrawLine(left, top, right, top, 1.0);
            canvas.DrawLine(left, bottom, right, bottom, 1.0);
            canvas.DrawLine(left, top, left, bottom, 1.0);
            canvas.DrawLine(right, top, right, bottom, 1.0);

            canvas.DrawLine(x[1], top + rowHeight, right, top + rowHeight, 0.4);
            canvas.DrawLine(left, bodyTop, right, bodyTop, 0.8);
            for (int i = 1; i < monthRows; i++)
                canvas.DrawLine(left, bodyTop + rowHeight * i, right, bodyTop + rowHeight * i, 0.4);

            for (int i = 1; i < ColumnWidths.Length; i++)
            {
                bool groupEdge = i == 1 || i == FirstSickColumn;   // Month | VACATION | SICK
                canvas.DrawLine(x[i], groupEdge ? top : top + rowHeight, x[i], bottom, groupEdge ? 0.8 : 0.4);
            }

            // ---- headings
            DtrPageRenderer.DrawCentered(canvas, card.Year.ToString(), x[0], x[1],
                DtrPageRenderer.Baseline(top, rowHeight * 2, 9.5), 9.5, true, Black);
            double head1 = DtrPageRenderer.Baseline(top, rowHeight, 8.5);
            DtrPageRenderer.DrawCentered(canvas, "VACATION", x[1], x[FirstSickColumn], head1, 8.5, true, Black);
            DtrPageRenderer.DrawCentered(canvas, "SICK", x[FirstSickColumn], x[ColumnWidths.Length], head1, 8.5, true, Black);

            double head2 = DtrPageRenderer.Baseline(top + rowHeight, rowHeight, 7);
            for (int i = 1; i < ColumnNames.Length; i++)
                DtrPageRenderer.DrawCentered(canvas, ColumnNames[i], x[i], x[i + 1], head2, 7, true, Black);

            // ---- one row per month
            const double size = 7.5;
            for (int i = 0; i < monthRows; i++)
            {
                double baseline = DtrPageRenderer.Baseline(bodyTop + rowHeight * i, rowHeight, size);
                bool bold = i == current;

                string name = new DateTime(card.Year, i + 1, 1).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
                canvas.DrawText(name, x[0] + 4, baseline, DtrPageRenderer.FitSize(name, x[1] - x[0] - 6, size, bold), bold, Black);

                LeaveMonth? m = card.Months[i];
                if (m == null)
                    continue; // the program knows nothing about this month

                string[] cells =
                {
                    LeaveRules.Format(m.VlEarned),
                    Credits(m.VlEnjoyed),
                    m.VlNet.HasValue ? LeaveRules.Format(m.VlNet.Value) : "",
                    m.VlDatesText,
                    DaysOrBlank(m.SplDays),
                    DaysOrBlank(m.WellnessDays),
                    DaysOrBlank(m.CtoDays),
                    Credits(m.LateDays),
                    Credits(m.UndertimeDays),
                    LeaveRules.Format(m.SlEarned),
                    Credits(m.SlEnjoyed),
                    m.SlNet.HasValue ? LeaveRules.Format(m.SlNet.Value) : "",
                    m.SickDates
                };

                for (int c = 0; c < cells.Length; c++)
                    DrawCell(canvas, cells[c], x[c + 1], x[c + 2], baseline, size, bold);
            }

            return bottom;
        }

        /// <summary>"1 of 3" - days used of the yearly limit.</summary>
        private static string UsedOf(decimal used, LeaveType type)
        {
            return LeaveCalculator.UsedText(used, type).Replace(" this year", "");
        }

        /// <summary>"VL ______20.027______" - a balance written on a line.</summary>
        private static void DrawBalance(IDtrCanvas canvas, string label, decimal? value, double x1, double x2, double baseline)
        {
            canvas.DrawText(label, x1, baseline, 9, true, Black);
            double lineStart = x1 + 24;
            canvas.DrawLine(lineStart, baseline + 2.5, x2, baseline + 2.5, 0.6);
            if (value.HasValue)
                DtrPageRenderer.DrawCentered(canvas, LeaveRules.Format(value.Value), lineStart, x2, baseline, 9.5, true, Black);
        }

        /// <summary>Days (SPL, WL, CTO), with zero left empty.</summary>
        private static string DaysOrBlank(decimal days)
        {
            return days == 0m ? "" : LeaveRules.FormatShort(days);
        }

        /// <summary>Leave credits, with zero left empty the way it is on the leave card.</summary>
        private static string Credits(decimal value)
        {
            return value == 0m ? "" : LeaveRules.Format(value);
        }
    }
}
