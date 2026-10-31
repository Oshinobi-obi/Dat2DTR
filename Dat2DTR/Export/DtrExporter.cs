namespace Dat2DTR
{
    /// <summary>This is the list of paper sizes the DTR can be saved or printed on.</summary>
    public static class PaperInfo
    {
        /// <summary>This is the width of the paper in points (1/72 inch).</summary>
        public static double WidthOf(DtrPaper paper)
        {
            return paper == DtrPaper.A4 ? 595.28 : 612;       // This is A4 = 210 mm, the rest = 8.5 in
        }

        /// <summary>This is the height of the paper in points (1/72 inch).</summary>
        public static double HeightOf(DtrPaper paper)
        {
            switch (paper)
            {
                case DtrPaper.Letter: return 792;   // This is 11 in
                case DtrPaper.Long: return 936;     // This is 13 in
                case DtrPaper.Legal: return 1008;   // This is 14 in
                default: return 841.89;             // This is A4 = 297 mm
            }
        }

        /// <summary>This is the number Excel uses for this paper size.</summary>
        public static int ExcelCodeOf(DtrPaper paper)
        {
            switch (paper)
            {
                case DtrPaper.Letter: return 1;
                case DtrPaper.Long: return 14;      // This is "Folio" 8.5 x 13 in
                case DtrPaper.Legal: return 5;
                default: return 9;                  // This is A4
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

    /// <summary>This is where finished DTRs are saved to a file.</summary>
    public static class DtrExporter
    {
        /// <summary>
        /// This is one PDF file, one page per employee. When leave cards are given (one per DTR, in the
        /// same order), each DTR is followed by a "Computation of Leaves" page.
        /// </summary>
        /// <param name="includeDtr">This is false for only the Computation of Leaves pages.</param>
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

        /// <summary>This is one PDF file with one Certificate of Leave Credits per page.</summary>
        public static void SaveCertificatesPdf(string path, IList<LeaveCertificate> certificates, DtrPaper paper)
        {
            var pdf = new PdfDocument();
            foreach (LeaveCertificate certificate in certificates)
            {
                PdfPage page = pdf.AddPage(PaperInfo.WidthOf(paper), PaperInfo.HeightOf(paper));
                CertificateRenderer.Draw(page, certificate, page.Width, page.Height);
            }
            string title = certificates.Count == 1
                ? "Certificate of Leave Credits - " + (certificates[0].EmployeeName.Length > 0 ? certificates[0].EmployeeName : certificates[0].EmployeeId)
                : "Certificates of Leave Credits";
            pdf.Save(path, title);
        }

        /// <summary>This is one PDF file with the attendance reports, sideways (landscape), each report starting on a new page.</summary>
        public static void SaveReportsPdf(string path, IList<AttendanceReport> reports, DtrPaper paper)
        {
            var pdf = new PdfDocument();
            double width = PaperInfo.HeightOf(paper), height = PaperInfo.WidthOf(paper);   // This is landscape: the long side across
            foreach (ReportPage report in ReportRenderer.Paginate(reports, width, height))
            {
                PdfPage page = pdf.AddPage(width, height);
                ReportRenderer.Draw(page, report, page.Width, page.Height);
            }
            pdf.Save(path, reports.Count == 1 ? reports[0].Title : "Attendance Reports");
        }

        /// <summary>
        /// This is one Excel workbook, one worksheet per employee. When leave cards are given (one per DTR,
        /// in the same order), each DTR worksheet is followed by a leave card worksheet.
        /// </summary>
        /// <param name="includeDtr">This is false for only the leave card worksheets.</param>
        public static void SaveExcel(string path, IList<DtrSheet> sheets, DtrPaper paper, IList<LeaveCard>? cards = null,
                                     bool includeDtr = true)
        {
            XlsxWriter.Save(path, sheets, paper, cards, includeDtr);
        }
    }
}
