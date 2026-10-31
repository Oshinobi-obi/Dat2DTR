namespace Dat2DTR
{
    /// <summary>This is one printed page of an attendance report: which report, and which of its rows.</summary>
    public sealed class ReportPage
    {
        public AttendanceReport Report = new AttendanceReport();

        /// <summary>This is the first row on the page and how many rows (0 rows = the "Nobody this month" page).</summary>
        public int FirstRow, RowCount;

        /// <summary>This is true for the first page of the report (with the explanation); false = "(continued)".</summary>
        public bool IsFirst;

        /// <summary>This is the page number in the whole printout, and how many pages there are.</summary>
        public int Number, Total;
    }

    /// <summary>
    /// This is where the attendance reports (see AttendanceReports) are drawn as pages, sideways (landscape):
    /// the title, the explanation, then the table with the headings repeated on every page. A long list of
    /// dates wraps onto more lines. The printout and the PDF both call this same code.
    /// </summary>
    public static class ReportRenderer
    {
        private const double Margin = 36;
        private const double TitleSize = 13, NoteSize = 9, HeadSize = 8.5, CellSize = 8.5;
        private const double LineHeight = 11, CellPad = 4;
        private const int Black = 0x000000;
        private const int Grey = 0x505050;
        private const int HeadingFill = 0xE6E8F2;   // This is light grey-blue behind the headings

        /// <summary>
        /// This is where the reports are cut into pages that fit the paper (each report starts on a new page).
        /// pageWidth / pageHeight are the size of the page as it is printed (landscape: the wide side first).
        /// </summary>
        public static List<ReportPage> Paginate(IList<AttendanceReport> reports, double pageWidth, double pageHeight)
        {
            var pages = new List<ReportPage>();
            foreach (AttendanceReport report in reports)
            {
                double[] widths = ColumnWidths(report, pageWidth);
                double headHeight = HeadingHeight(report, widths);
                int row = 0;
                bool first = true;
                do
                {
                    double top = TableTop(report, pageWidth, first) + headHeight;
                    double bottom = pageHeight - Margin - 18;   // This is room for the page number
                    int count = 0;
                    while (row + count < report.Rows.Count)
                    {
                        double height = RowHeight(report, report.Rows[row + count], widths);
                        if (count > 0 && top + height > bottom) break;
                        top += height;
                        count++;
                    }
                    pages.Add(new ReportPage { Report = report, FirstRow = row, RowCount = count, IsFirst = first });
                    row += count;
                    first = false;
                }
                while (row < report.Rows.Count);
            }
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Number = i + 1;
                pages[i].Total = pages.Count;
            }
            return pages;
        }

        public static void Draw(IDtrCanvas canvas, ReportPage page, double pageWidth, double pageHeight)
        {
            AttendanceReport report = page.Report;
            double left = Margin, right = pageWidth - Margin, width = right - left;

            // This is the title and (on the first page of the report) the explanation ------------------------------------------------------------
            double y = Margin + TitleSize;
            string title = report.Title + (page.IsFirst ? "" : "   (continued)");
            canvas.DrawText(title, left, y, DtrPageRenderer.FitSize(title, width, TitleSize, true), true, Black);
            if (page.IsFirst)
            {
                foreach (string line in DtrPageRenderer.Wrap(report.Note, width, 0, NoteSize, false))
                {
                    y += NoteSize + 4;
                    canvas.DrawText(line, left, y, NoteSize, false, Grey);
                }
            }

            // This is the table ------------------------------------------------------------
            double[] widths = ColumnWidths(report, pageWidth);
            double top = TableTop(report, pageWidth, page.IsFirst);
            double headHeight = HeadingHeight(report, widths);
            double tableWidth = widths.Sum();
            canvas.FillRectangle(left, top, tableWidth, headHeight, HeadingFill);
            double x = left;
            for (int c = 0; c < widths.Length; c++)
            {
                List<string> lines = DtrPageRenderer.Wrap(report.Headings[c], widths[c] - CellPad * 2, 0, HeadSize, true);
                double textTop = top + (headHeight - lines.Count * LineHeight) / 2;
                for (int l = 0; l < lines.Count; l++)
                    DtrPageRenderer.DrawCentered(canvas, lines[l], x, x + widths[c], textTop + LineHeight * l + HeadSize, HeadSize, true, Black);
                x += widths[c];
            }

            var lineTops = new List<double> { top, top + headHeight };
            double rowTop = top + headHeight;
            for (int r = page.FirstRow; r < page.FirstRow + page.RowCount; r++)
            {
                object[] values = report.Rows[r];
                double height = RowHeight(report, values, widths);
                x = left;
                for (int c = 0; c < widths.Length && c < values.Length; c++)
                {
                    string text = Convert.ToString(values[c]) ?? "";
                    if (IsWrapped(report, c))
                    {
                        List<string> lines = DtrPageRenderer.Wrap(text, widths[c] - CellPad * 2, 0, CellSize, false);
                        for (int l = 0; l < lines.Count; l++)
                            canvas.DrawText(lines[l], x + CellPad, rowTop + CellPad + LineHeight * l + CellSize, CellSize, false, Black);
                    }
                    else
                    {
                        double size = DtrPageRenderer.FitSize(text, widths[c] - CellPad * 2, CellSize, false);
                        double baseline = rowTop + CellPad + CellSize;
                        if (c == 2) canvas.DrawText(text, x + CellPad, baseline, size, false, Black);   // This is the name, on the left
                        else DtrPageRenderer.DrawCentered(canvas, text, x, x + widths[c], baseline, size, values[c] is int, Black);
                    }
                    x += widths[c];
                }
                rowTop += height;
                lineTops.Add(rowTop);
            }
            if (page.RowCount == 0)
            {
                canvas.DrawText("Nobody this month.", left + CellPad, rowTop + CellPad + CellSize, CellSize, false, Black);
                rowTop += LineHeight + CellPad * 2;
                lineTops.Add(rowTop);
            }

            // This is the lines of the table.
            for (int i = 0; i < lineTops.Count; i++)
                canvas.DrawLine(left, lineTops[i], left + tableWidth, lineTops[i], i == 0 || i == lineTops.Count - 1 ? 0.9 : 0.4);
            x = left;
            for (int c = 0; c <= widths.Length; c++)
            {
                canvas.DrawLine(x, top, x, rowTop, c == 0 || c == widths.Length ? 0.9 : 0.4);
                if (c < widths.Length) x += widths[c];
            }

            // This is the page number at the bottom.
            string foot = "Page " + page.Number + " of " + page.Total;
            DtrPageRenderer.DrawRight(canvas, foot, right, pageHeight - Margin, 8, false);
            canvas.DrawText("Dat2DTR  -  " + report.Title, left, pageHeight - Margin, DtrPageRenderer.FitSize("Dat2DTR  -  " + report.Title, width - 80, 8, false), false, Grey);
        }

        /// <summary>This is the width of each column: the Excel widths, stretched to fill the page.</summary>
        private static double[] ColumnWidths(AttendanceReport report, double pageWidth)
        {
            int count = report.Headings.Length;
            var widths = new double[count];
            double sum = 0;
            for (int c = 0; c < count; c++)
            {
                widths[c] = c < report.Widths.Length ? report.Widths[c] : 12;
                sum += widths[c];
            }
            double available = pageWidth - Margin * 2;
            for (int c = 0; c < count; c++)
                widths[c] = widths[c] / sum * available;
            return widths;
        }

        /// <summary>This is true for the long columns (the dates), which wrap onto more lines.</summary>
        private static bool IsWrapped(AttendanceReport report, int column)
        {
            return column != 2 && column < report.Widths.Length && report.Widths[column] >= 30;
        }

        private static double TableTop(AttendanceReport report, double pageWidth, bool first)
        {
            double y = Margin + TitleSize;
            if (first)
                y += DtrPageRenderer.Wrap(report.Note, pageWidth - Margin * 2, 0, NoteSize, false).Count * (NoteSize + 4);
            return y + 12;
        }

        private static double HeadingHeight(AttendanceReport report, double[] widths)
        {
            int lines = 1;
            for (int c = 0; c < widths.Length; c++)
                lines = Math.Max(lines, DtrPageRenderer.Wrap(report.Headings[c], widths[c] - CellPad * 2, 0, HeadSize, true).Count);
            return lines * LineHeight + CellPad * 2;
        }

        private static double RowHeight(AttendanceReport report, object[] values, double[] widths)
        {
            int lines = 1;
            for (int c = 0; c < widths.Length && c < values.Length; c++)
                if (IsWrapped(report, c))
                    lines = Math.Max(lines, DtrPageRenderer.Wrap(Convert.ToString(values[c]) ?? "", widths[c] - CellPad * 2, 0, CellSize, false).Count);
            return lines * LineHeight + CellPad * 2 - 2;
        }
    }
}
