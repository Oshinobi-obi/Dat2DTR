namespace Dat2DTR
{
    /// <summary>Paper sizes the DTR can be saved or printed on.</summary>
    public static class PaperInfo
    {
        /// <summary>Width of the paper in points (1/72 inch).</summary>
        public static double WidthOf(DtrPaper paper)
        {
            return paper == DtrPaper.A4 ? 595.28 : 612;       // A4 = 210 mm, the rest = 8.5 in
        }

        /// <summary>Height of the paper in points (1/72 inch).</summary>
        public static double HeightOf(DtrPaper paper)
        {
            switch (paper)
            {
                case DtrPaper.Letter: return 792;   // 11 in
                case DtrPaper.Long: return 936;     // 13 in
                case DtrPaper.Legal: return 1008;   // 14 in
                default: return 841.89;             // A4 = 297 mm
            }
        }

        /// <summary>The number Excel uses for this paper size.</summary>
        public static int ExcelCodeOf(DtrPaper paper)
        {
            switch (paper)
            {
                case DtrPaper.Letter: return 1;
                case DtrPaper.Long: return 14;      // "Folio" 8.5 x 13 in
                case DtrPaper.Legal: return 5;
                default: return 9;                  // A4
            }
        }

        public static string NameOf(DtrPaper paper)
        {
            switch (paper)
            {
                case DtrPaper.Letter: return "Letter / Short (8.5 x 11 in)";
                case DtrPaper.Long: return "Long (8.5 x 13 in)";
                case DtrPaper.Legal: return "Legal (8.5 x 14 in)";
                default: return "A4 (210 x 297 mm)";
            }
        }
    }

    /// <summary>Saves finished DTRs to a file.</summary>
    public static class DtrExporter
    {
        /// <summary>
        /// One PDF file, one page per employee. When leave cards are given (one per DTR, in the
        /// same order), each DTR is followed by a "Computation of Leaves" page.
        /// </summary>
        /// <param name="includeDtr">false = only the Computation of Leaves pages.</param>
        public static void SavePdf(string path, IList<DtrSheet> sheets, DtrPaper paper, IList<LeaveCard>? cards = null,
                                   bool includeDtr = true)
        {
            var pdf = new PdfDocument();
            for (int i = 0; i < sheets.Count; i++)
            {
                if (includeDtr || cards == null)
                {
                    PdfPage page = pdf.AddPage(PaperInfo.WidthOf(paper), PaperInfo.HeightOf(paper));
                    DtrPageRenderer.Draw(page, sheets[i], page.Width, page.Height);
                }

                if (cards != null && i < cards.Count)
                {
                    PdfPage leavePage = pdf.AddPage(PaperInfo.WidthOf(paper), PaperInfo.HeightOf(paper));
                    LeavePageRenderer.Draw(leavePage, cards[i], leavePage.Width, leavePage.Height);
                }
            }

            string title = sheets.Count == 1
                ? "DTR " + sheets[0].MonthText + " - " + sheets[0].NameLine
                : "DTR " + (sheets.Count > 0 ? sheets[0].MonthText : "");
            pdf.Save(path, title);
        }

        /// <summary>
        /// One Excel workbook, one worksheet per employee. When leave cards are given (one per DTR,
        /// in the same order), each DTR worksheet is followed by a leave card worksheet.
        /// </summary>
        /// <param name="includeDtr">false = only the leave card worksheets.</param>
        public static void SaveExcel(string path, IList<DtrSheet> sheets, DtrPaper paper, IList<LeaveCard>? cards = null,
                                     bool includeDtr = true)
        {
            XlsxWriter.Save(path, sheets, paper, cards, includeDtr);
        }
    }
}
