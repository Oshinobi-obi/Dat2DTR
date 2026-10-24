namespace Dat2DTR
{
    /// <summary>
    /// This is where the "Computation of Leaves" page of one employee is drawn on a canvas:
    /// the leave card of the year (one row per month, the same columns as the
    /// leave card kept by Personnel), below it how the open month was computed, and
    /// below a line across the page the History of Availment of Leave (January to December).
    ///
    /// Like the DTR, this is the ONLY place that decides how the page looks.
    /// The preview on screen, the printout and the PDF all call this same code.
    /// </summary>
    public static class LeavePageRenderer
    {
        private const int Black = 0x000000;
        private const int Grey = 0x505050;
        private const int HeadingFill = 0xE6E8F2;   // This is light grey-blue behind the table headings
        private const int CurrentFill = 0xFFF2B3;   // This is pale yellow behind the open month

        // This is the widths of the 15 columns (they are stretched to the width of the paper):
        // Month | VACATION: Earned, Enjoyed, Net, Leave Date/s, SPL, WL, CTO, SOLO, Late, Undertime
        //       | SICK: Earned, Enjoyed, Net, Leave Date/s
        private static readonly double[] ColumnWidths = { 46, 34, 36, 38, 58, 22, 22, 24, 26, 32, 40, 34, 36, 38, 48 };

        /// <summary>This is the column number (in ColumnWidths) of the first SICK column.</summary>
        private const int FirstSickColumn = 11;

        private static readonly string[] ColumnNames =
        {
            "", "Earned", "Enjoyed", "Net", "Leave Date/s", "SPL", "WL", "CTO", "SOLO", "Late", "Undertime",
            "Earned", "Enjoyed", "Net", "Leave Date/s"
        };

        // This is the History of Availment of Leave: Month | Vacation | Forced | Sick | SPL | Wellness | CTO | Solo Parent | Total days
        private static readonly double[] HistoryWidths = { 52, 76, 52, 70, 46, 46, 46, 46, 40 };

        private static readonly string[] HistoryNames =
        {
            "Month", "Vacation Leave", "Forced Leave", "Sick Leave", "SPL", "Wellness", "CTO", "Solo Parent", "Days"
        };

        public static void Draw(IDtrCanvas canvas, LeaveCard card, double pageWidth, double pageHeight)
        {
            const double marginX = 34, marginTop = 34;

            double left = marginX;
            double right = pageWidth - marginX;
            double width = right - left;

            // This is the heading ------------------------------------------------------------
            double y = marginTop;
            canvas.DrawText("COMPUTATION OF LEAVES", left, y + 11, 11.5, true, Black);
            DtrPageRenderer.DrawRight(canvas, "For the month of " + card.MonthText, right, y + 9, 8.5, true);

            // This is under "For the month of": the Vacation Leave and Sick Leave left at the end of the month,
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

            // This is the leave card of the year ------------------------------------------------------------
            y = DrawCardTable(canvas, card, left, width, y + 10);

            // This is used this year, under the table ------------------------------------------------------------
            y += 13;
            string months = card.MonthStart.Month == 1 ? "January" : "January to " + card.Current.MonthName;
            string used = "Used this year (" + months + "):     " +
                          "Special Privilege Leave  " + UsedOf(card.SplUsed, LeaveType.SpecialPrivilege) + "          " +
                          "Forced Leave  " + UsedOf(card.ForcedUsed, LeaveType.Forced) + "          " +
                          "Wellness Leave  " + UsedOf(card.WellnessUsed, LeaveType.Wellness) + "          " +
                          GivenText(card);
            canvas.DrawText(used, left, y, DtrPageRenderer.FitSize(used, width, 8, true), true, Black);

            // This is how the month was computed ------------------------------------------------------------
            // This is two columns: Vacation and Sick Leave on the left; SPL, Wellness Leave and CTO on the right,
            // and Solo Parent Leave in the column that is shorter.
            y += 24;
            canvas.DrawText("HOW " + card.MonthText.ToUpperInvariant() + " WAS COMPUTED", left, y, 9.5, true, Black);
            canvas.DrawLine(left, y + 4, right, y + 4, 0.8);
            y += 6;

            List<LeaveLine> leftLines, rightLines;
            SplitColumns(card.Breakdown, out leftLines, out rightLines);

            const double gap = 24;
            double columnWidth = (width - gap) / 2;
            double endLeft = DrawLines(canvas, leftLines, left, columnWidth, y);
            double endRight = DrawLines(canvas, rightLines, left + columnWidth + gap, columnWidth, y);
            y = Math.Max(endLeft, endRight);

            // This is the History of Availment of Leave ------------------------------------------------------------
            // This is a line across the page, the heading in the middle, and the table January to December.
            y += 18;
            canvas.DrawLine(left, y, right, y, 1.0);
            y += 15;
            string history = "HISTORY OF AVAILMENT OF LEAVE  -  JANUARY TO DECEMBER " + card.Year;
            DtrPageRenderer.DrawCentered(canvas, history, left, right, y, DtrPageRenderer.FitSize(history, width, 9.5, true), true, Black);
            y += 7;

            // This is 14 rows (heading, 12 months, total); smaller rows when the page is full
            const int historyRows = 14;
            double room = pageHeight - 62 - y;
            double historyRow = Math.Max(10, Math.Min(14, room / historyRows));
            y = DrawHistoryTable(canvas, card, left, width, y, historyRow);

            // This is the small print at the bottom ------------------------------------------------------------
            // This is left out when the page is so full that it would not fit any more.
            double foot = Math.Max(y + 26, pageHeight - 46);
            if (foot + 16 > pageHeight - 12)
                return;

            const string foot1 = "Late and undertime are turned into days with the CSC table of fractions of a day " +
                                 "(8 hours = 1 day,  1 hour = .125,  1 minute = .002) and are deducted from Vacation Leave.";
            const string foot2 = "Enjoyed = what was taken from the credits.   Net = balance at the end of the month.   " +
                                 "SPL, WL, CTO and SOLO are in days.   Prepared with Dat2DTR - compare with the employee's leave card.";

            canvas.DrawLine(left, foot - 10, right, foot - 10, 0.4);
            canvas.DrawText(foot1, left, foot, DtrPageRenderer.FitSize(foot1, width, 7, false), false, Grey);
            canvas.DrawText(foot2, left, foot + 9.5, DtrPageRenderer.FitSize(foot2, width, 7, false), false, Grey);
        }

        /// <summary>
        /// This is where the computation is split into the two columns of the page: Vacation and Sick Leave on the left,
        /// then every other part (SPL, Wellness Leave, CTO, Solo Parent Leave), in order, in whichever
        /// column is shorter at that point - so the two columns end about level.
        /// (The Excel worksheet uses the same split.)
        /// </summary>
        internal static void SplitColumns(List<LeaveLine> lines, out List<LeaveLine> leftLines, out List<LeaveLine> rightLines)
        {
            var parts = new List<List<LeaveLine>>();
            foreach (LeaveLine line in lines)
            {
                if (line.Kind == LeaveLineKind.Heading || parts.Count == 0)
                    parts.Add(new List<LeaveLine>());
                parts[parts.Count - 1].Add(line);
            }

            leftLines = new List<LeaveLine>();
            rightLines = new List<LeaveLine>();
            double leftHeight = 0, rightHeight = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                double height = parts[i].Count + 0.5;   // This is because a heading has a gap above it
                bool toLeft = i < 2 || (i > 2 && leftHeight < rightHeight);   // This is Vacation, Sick | Special Privilege first on the right
                if (toLeft) { leftLines.AddRange(parts[i]); leftHeight += height; }
                else { rightLines.AddRange(parts[i]); rightHeight += height; }
            }
        }

        /// <summary>This is "CTO  1   (balance 2.5)          SOLO  0   (balance 7)" - used this year, and what is left.</summary>
        internal static string GivenText(LeaveCard card)
        {
            return "CTO  " + LeaveRules.FormatShort(card.CtoUsed) + "   (balance " + LeaveRules.FormatShort(card.Cto.End) + ")" +
                   "          " +
                   "SOLO  " + LeaveRules.FormatShort(card.SoloUsed) + "   (balance " + LeaveRules.FormatShort(card.Solo.End) + ")";
        }

        /// <summary>
        /// This is the History of Availment of Leave: one row per month, January to December, with the days of
        /// each kind of leave used, and the total. Returns the position of its bottom edge.
        /// </summary>
        private static double DrawHistoryTable(IDtrCanvas canvas, LeaveCard card, double left, double width, double top,
                                               double rowHeight)
        {
            const int monthRows = 12;
            double total = HistoryWidths.Sum();
            var x = new double[HistoryWidths.Length + 1];
            x[0] = left;
            for (int i = 0; i < HistoryWidths.Length; i++)
                x[i + 1] = x[i] + HistoryWidths[i] * width / total;
            double right = x[HistoryWidths.Length];

            double bodyTop = top + rowHeight;
            double totalTop = bodyTop + rowHeight * monthRows;
            double bottom = totalTop + rowHeight;

            // This is the backgrounds ----
            canvas.FillRectangle(left, top, width, rowHeight, HeadingFill);
            canvas.FillRectangle(left, totalTop, width, rowHeight, HeadingFill);
            int current = card.MonthStart.Month - 1;
            canvas.FillRectangle(left, bodyTop + rowHeight * current, width, rowHeight, CurrentFill);

            // This is the lines ----
            canvas.DrawLine(left, top, right, top, 1.0);
            canvas.DrawLine(left, bottom, right, bottom, 1.0);
            canvas.DrawLine(left, bodyTop, right, bodyTop, 0.8);
            canvas.DrawLine(left, totalTop, right, totalTop, 0.8);
            for (int i = 1; i < monthRows; i++)
                canvas.DrawLine(left, bodyTop + rowHeight * i, right, bodyTop + rowHeight * i, 0.4);
            canvas.DrawLine(left, top, left, bottom, 1.0);
            canvas.DrawLine(right, top, right, bottom, 1.0);
            for (int i = 1; i < HistoryWidths.Length; i++)
                canvas.DrawLine(x[i], top, x[i], bottom, i == 1 || i == HistoryWidths.Length - 1 ? 0.8 : 0.4);

            // This is the headings ----
            double size = Math.Min(7.5, rowHeight * 0.64);
            double headBaseline = DtrPageRenderer.Baseline(top, rowHeight, 7);
            for (int i = 0; i < HistoryNames.Length; i++)
                DrawCell(canvas, HistoryNames[i], x[i], x[i + 1], headBaseline, 7, true);

            // This is one row per month: the days of each kind ----
            var sums = new decimal[8];
            for (int i = 0; i < monthRows; i++)
            {
                double baseline = DtrPageRenderer.Baseline(bodyTop + rowHeight * i, rowHeight, size);
                bool bold = i == current;
                string name = new DateTime(card.Year, i + 1, 1).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
                canvas.DrawText(name, x[0] + 4, baseline, DtrPageRenderer.FitSize(name, x[1] - x[0] - 6, size, bold), bold, Black);

                LeaveMonth h = card.History[i];
                if (h == null) continue;
                string[] cells = { h.VacationDates, h.ForcedDates, h.SickDates, h.SplDates, h.WellnessDates, h.CtoDates, h.SoloDates,
                                   DaysOrBlank(h.AllLeaveDays) };
                decimal[] days = { h.VacationDays, h.ForcedDays, h.SickDays, h.SplDays, h.WellnessDays, h.CtoDays, h.SoloDays,
                                   h.AllLeaveDays };
                for (int c = 0; c < cells.Length; c++)
                {
                    DrawCell(canvas, cells[c], x[c + 1], x[c + 2], baseline, size, bold || c == cells.Length - 1);
                    sums[c] += days[c];
                }
            }

            // This is the total of the year (in days) ----
            double totalBaseline = DtrPageRenderer.Baseline(totalTop, rowHeight, size);
            canvas.DrawText("Total days", x[0] + 4, totalBaseline, DtrPageRenderer.FitSize("Total days", x[1] - x[0] - 6, size, true), true, Black);
            for (int c = 0; c < sums.Length; c++)
                DrawCell(canvas, LeaveRules.FormatShort(sums[c]), x[c + 1], x[c + 2], totalBaseline, size, true);

            return bottom;
        }

        /// <summary>This is where lines of the computation are written in one column. Returns where the last line ended.</summary>
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
        /// This is where text is centred in a cell of the table. Text that is too long is first made smaller, and
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
                // This is where the last entry of the list is dropped (or, if there is no comma, a few letters)
                int cut = text.LastIndexOf(',', text.Length - 5);
                text = (cut > 0 ? text.Substring(0, cut) : text.Substring(0, text.Length - 5)) + " ...";
            }

            textWidth = FontMetrics.TextWidth(text, size, bold);
            canvas.DrawText(text, x1 + (x2 - x1 - textWidth) / 2, baseline, size, bold, Black);
        }

        /// <summary>This is where the 12-month table is drawn; returns the position of its bottom edge.</summary>
        private static double DrawCardTable(IDtrCanvas canvas, LeaveCard card, double left, double width, double top)
        {
            const double rowHeight = 15;
            const int headRows = 2, monthRows = 12;

            // This is the column edges
            double total = ColumnWidths.Sum();
            var x = new double[ColumnWidths.Length + 1];
            x[0] = left;
            for (int i = 0; i < ColumnWidths.Length; i++)
                x[i + 1] = x[i] + ColumnWidths[i] * width / total;
            double right = x[ColumnWidths.Length];

            double bodyTop = top + rowHeight * headRows;
            double bottom = bodyTop + rowHeight * monthRows;

            // This is the coloured backgrounds first, so the lines and the text go on top ----
            canvas.FillRectangle(left, top, width, rowHeight * headRows, HeadingFill);
            int current = card.MonthStart.Month - 1;
            canvas.FillRectangle(left, bodyTop + rowHeight * current, width, rowHeight, CurrentFill);

            // This is the lines ----
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
                bool groupEdge = i == 1 || i == FirstSickColumn;   // This is Month | VACATION | SICK
                canvas.DrawLine(x[i], groupEdge ? top : top + rowHeight, x[i], bottom, groupEdge ? 0.8 : 0.4);
            }

            // This is the headings ----
            DtrPageRenderer.DrawCentered(canvas, card.Year.ToString(), x[0], x[1],
                DtrPageRenderer.Baseline(top, rowHeight * 2, 9.5), 9.5, true, Black);
            double head1 = DtrPageRenderer.Baseline(top, rowHeight, 8.5);
            DtrPageRenderer.DrawCentered(canvas, "VACATION", x[1], x[FirstSickColumn], head1, 8.5, true, Black);
            DtrPageRenderer.DrawCentered(canvas, "SICK", x[FirstSickColumn], x[ColumnWidths.Length], head1, 8.5, true, Black);

            double head2 = DtrPageRenderer.Baseline(top + rowHeight, rowHeight, 7);
            for (int i = 1; i < ColumnNames.Length; i++)
                DtrPageRenderer.DrawCentered(canvas, ColumnNames[i], x[i], x[i + 1], head2, 7, true, Black);

            // This is one row per month ----
            const double size = 7.5;
            for (int i = 0; i < monthRows; i++)
            {
                double baseline = DtrPageRenderer.Baseline(bodyTop + rowHeight * i, rowHeight, size);
                bool bold = i == current;

                string name = new DateTime(card.Year, i + 1, 1).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
                canvas.DrawText(name, x[0] + 4, baseline, DtrPageRenderer.FitSize(name, x[1] - x[0] - 6, size, bold), bold, Black);

                LeaveMonth? m = card.Months[i];
                if (m == null)
                    continue; // This is because the program knows nothing about this month

                string[] cells =
                {
                    LeaveRules.Format(m.VlEarned),
                    Credits(m.VlEnjoyed),
                    m.VlNet.HasValue ? LeaveRules.Format(m.VlNet.Value) : "",
                    m.VlDatesText,
                    DaysOrBlank(m.SplDays),
                    DaysOrBlank(m.WellnessDays),
                    DaysOrBlank(m.CtoDays),
                    DaysOrBlank(m.SoloDays),
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

        /// <summary>This is "1 of 3" - days used of the yearly limit.</summary>
        private static string UsedOf(decimal used, LeaveType type)
        {
            return LeaveCalculator.UsedText(used, type).Replace(" this year", "");
        }

        /// <summary>This is "VL ______20.027______" - a balance written on a line.</summary>
        private static void DrawBalance(IDtrCanvas canvas, string label, decimal? value, double x1, double x2, double baseline)
        {
            canvas.DrawText(label, x1, baseline, 9, true, Black);
            double lineStart = x1 + 24;
            canvas.DrawLine(lineStart, baseline + 2.5, x2, baseline + 2.5, 0.6);
            if (value.HasValue)
                DtrPageRenderer.DrawCentered(canvas, LeaveRules.Format(value.Value), lineStart, x2, baseline, 9.5, true, Black);
        }

        /// <summary>This is days (SPL, WL, CTO, SOLO), with zero left empty.</summary>
        private static string DaysOrBlank(decimal days)
        {
            return days == 0m ? "" : LeaveRules.FormatShort(days);
        }

        /// <summary>This is leave credits, with zero left empty the way it is on the leave card.</summary>
        private static string Credits(decimal value)
        {
            return value == 0m ? "" : LeaveRules.Format(value);
        }
    }
}
