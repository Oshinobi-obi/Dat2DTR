using System.Drawing.Printing;

namespace Dat2DTR
{
    /// <summary>
    /// This is the "Reports" window: the five attendance reports of the whole office for the month on screen,
    /// one tab each (see AttendanceReports): 1. late on Mondays, 2. no Time-In on Mondays, 3. both together,
    /// 4. late in the month, 5. undertime in the month. Clicking a heading sorts the table.
    /// "Save to Excel..." saves all five in one workbook, one worksheet each. "Save as PDF..." and "Print..." ask
    /// first: only the report on screen, or all five (sideways pages, see ReportRenderer).
    /// </summary>
    internal sealed class ReportsForm : Form
    {
        private readonly List<AttendanceReport> _reports;
        private readonly string _monthText;
        private readonly DtrPaper _paper;
        private readonly string _startFolder;
        private readonly TabControl _tabs = new TabControl();
        private readonly Label _note = new Label();

        /// <summary>This is the folder the file was saved in (empty when nothing was saved).</summary>
        private string _savedFolder = "";

        /// <param name="monthText">This is the month of the reports, e.g. "October 2026".</param>
        /// <param name="reports">This is the five reports (see AttendanceReports.MakeAll).</param>
        /// <param name="startFolder">This is the folder the Save window starts in.</param>
        public ReportsForm(string monthText, List<AttendanceReport> reports, DtrPaper paper, string startFolder)
        {
            _reports = reports;
            _monthText = monthText;
            _paper = paper;
            _startFolder = startFolder;

            // This is the same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "Reports";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(1180, 660);
            MinimumSize = new Size(900, 420);

            var title = new Label();
            title.Text = "Attendance reports of the whole office  -  " + monthText;
            title.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            title.ForeColor = Theme.Navy;
            title.AutoEllipsis = true;
            title.SetBounds(16, 10, 1148, 26);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _note.ForeColor = Theme.TextMuted;
            _note.SetBounds(16, 40, 1148, 40);
            _note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Size = new Size(1180, 86);
            top.BackColor = Color.White;
            top.Controls.Add(title);
            top.Controls.Add(_note);
            GridFit.GrowWithText(this, _note, top);   // This is so the explanation is never cut off

            // This is one tab per report, each with its own table.
            _tabs.Dock = DockStyle.Fill;
            foreach (AttendanceReport report in reports)
            {
                var page = new TabPage(report.ShortName + "  (" + report.Rows.Count + ")");
                page.BackColor = Color.White;
                page.Controls.Add(MakeGrid(report));
                _tabs.TabPages.Add(page);
            }
            _tabs.SelectedIndexChanged += (sender, e) => ShowNote();

            var save = new Button();
            save.Text = "Save to Excel...";
            save.SetBounds(16, 16, 150, 34);
            save.Click += Save_Click;
            Theme.StylePrimary(save);

            var pdf = new Button();
            pdf.Text = "Save as PDF...";
            pdf.SetBounds(176, 16, 140, 34);
            pdf.Click += Pdf_Click;

            var print = new Button();
            print.Text = "Print...";
            print.SetBounds(326, 16, 110, 34);
            print.Click += Print_Click;

            var saved = new Label();
            saved.Text = "Excel: all five reports, one worksheet each.  PDF and Print: the report on screen, or all five.";
            saved.ForeColor = Theme.TextMuted;
            saved.TextAlign = ContentAlignment.MiddleLeft;
            saved.SetBounds(446, 16, 600, 34);

            var close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.Cancel;
            close.SetBounds(1180 - 16 - 96, 16, 96, 34);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(1180, 66);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(save);
            bottom.Controls.Add(pdf);
            bottom.Controls.Add(print);
            bottom.Controls.Add(saved);
            bottom.Controls.Add(close);

            Controls.Add(_tabs);
            Controls.Add(bottom);
            Controls.Add(top);
            CancelButton = close;
            ResumeLayout(false);
            PerformLayout();
            WindowState = FormWindowState.Maximized;   // This is so the long lists of dates have room (Restore gives the normal size)
            ShowNote();
        }

        /// <summary>This is the folder the reports were saved in, so the main window can remember it ("" = not saved).</summary>
        public string GetSavedFolder()
        {
            return _savedFolder;
        }

        /// <summary>This is the tab shown first: 0 = report 1 ... 4 = report 5.</summary>
        public void ShowReport(int index)
        {
            if (index >= 0 && index < _tabs.TabPages.Count)
                _tabs.SelectedIndex = index;
            ShowNote();
        }

        /// <summary>This is the explanation of the report on the tab that is shown.</summary>
        private void ShowNote()
        {
            int i = Math.Max(0, _tabs.SelectedIndex);
            if (i < _reports.Count)
                _note.Text = _reports[i].Title + ".   " + _reports[i].Note + "   Click a heading to sort.";
        }

        /// <summary>This is the table of one report. The last column (the dates) takes the rest of the width and wraps.</summary>
        private DataGridView MakeGrid(AttendanceReport report)
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowTemplate.Height = 25;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            for (int c = 0; c < report.Headings.Length; c++)
            {
                var column = new DataGridViewTextBoxColumn();
                column.HeaderText = report.Headings[c];
                double width = c < report.Widths.Length ? report.Widths[c] : 12;
                column.Width = (int)Math.Round(width * 7.5);
                column.SortMode = DataGridViewColumnSortMode.Automatic;
                bool number = report.Rows.Count > 0 && report.Rows[0][c] is int;
                bool dates = width >= 30 && c != 2;
                if (number || (!dates && c != 2 && c != 1))
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (dates)
                    column.Tag = GridFit.FreeText;
                if (c == report.Headings.Length - 1)
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns.Add(column);
            }
            GridFit.Attach(this, grid);    // This is so it fits the screen it is shown on (125%, 150%...)

            foreach (object[] values in report.Rows)
                grid.Rows.Add(values);
            if (report.Rows.Count == 0)
            {
                grid.Rows.Add();
                grid.Rows[0].Cells[Math.Min(2, grid.Columns.Count - 1)].Value = "Nobody this month.";
            }
            return grid;
        }

        /// <summary>
        /// This is the question before "Save as PDF..." and "Print...": only the report on screen, or all five.
        /// Returns the reports to use, or null when the user cancelled.
        /// </summary>
        private List<AttendanceReport>? AskWhich(string question)
        {
            AttendanceReport shown = _reports[Math.Max(0, Math.Min(_reports.Count - 1, _tabs.SelectedIndex))];
            int answer = OptionDialog.Ask(this, question, new[]
            {
                new KeyValuePair<string, string>("Only this report", shown.ShortName),
                new KeyValuePair<string, string>("All five reports", "Each report starts on a new page")
            });
            if (answer < 0)
                return null;
            return answer == 0 ? new List<AttendanceReport> { shown } : new List<AttendanceReport>(_reports);
        }

        /// <summary>This is "Save as PDF...": the report on screen, or all five, as one PDF file (sideways pages).</summary>
        private void Pdf_Click(object? sender, EventArgs e)
        {
            List<AttendanceReport>? chosen = AskWhich("What do you want to save as PDF?");
            if (chosen == null)
                return;
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the attendance reports as PDF";
                dialog.Filter = "PDF file (*.pdf)|*.pdf";
                dialog.FileName = (chosen.Count == 1 ? chosen[0].Title.Replace(" - ", " ") : "Attendance Reports " + _monthText) + ".pdf";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_startFolder))
                    dialog.InitialDirectory = _startFolder;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = dialog.FileName;
                if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    path += ".pdf";
                try
                {
                    DtrExporter.SaveReportsPdf(path, chosen, _paper);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The file could not be saved.\n\nIf a file with the same name is open, close it first.\n\n" +
                                    ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _savedFolder = Path.GetDirectoryName(path) ?? "";
                OfferToOpen(path, "The PDF was saved here:");
            }
        }

        /// <summary>This is "Print...": the report on screen, or all five, sideways (landscape).</summary>
        private void Print_Click(object? sender, EventArgs e)
        {
            List<AttendanceReport>? chosen = AskWhich("What do you want to print?");
            if (chosen == null)
                return;
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                MessageBox.Show(this, "No printer is installed on this computer.\n\nYou can use \"Save as PDF...\" instead.",
                                "Reports", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var document = new PrintDocument())
                using (var dialog = new PrintDialog())
                {
                    document.DocumentName = chosen.Count == 1 ? chosen[0].Title : "Attendance Reports " + _monthText;
                    SelectPaper(document);
                    document.DefaultPageSettings.Landscape = true;

                    List<ReportPage> pages = new List<ReportPage>();
                    int next = 0; // This is which page is printed next
                    document.BeginPrint += (s, args) => { next = 0; pages = new List<ReportPage>(); };
                    document.PrintPage += (s, args) =>
                    {
                        Graphics g = args.Graphics!;
                        // This is the printer's unit: 1/100 inch; the pages are laid out in points (1/72 inch).
                        // The drawing area starts after the edge the printer cannot print on, so we shift back by that.
                        g.PageUnit = GraphicsUnit.Display;
                        g.TranslateTransform(-args.PageSettings.HardMarginX, -args.PageSettings.HardMarginY);
                        g.ScaleTransform(100f / 72f, 100f / 72f);
                        double pageWidth = args.PageBounds.Width * 0.72, pageHeight = args.PageBounds.Height * 0.72;
                        if (pages.Count == 0)
                            pages = ReportRenderer.Paginate(chosen, pageWidth, pageHeight);
                        using (var canvas = new GdiCanvas(g))
                            ReportRenderer.Draw(canvas, pages[next], pageWidth, pageHeight);
                        next++;
                        args.HasMorePages = next < pages.Count;
                    };

                    dialog.Document = document;
                    dialog.UseEXDialog = true;
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;
                    Cursor = Cursors.WaitCursor;
                    document.Print();
                }
            }
            catch (Exception ex) when (ex is InvalidPrinterException || ex is System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(this, "The reports could not be printed.\n\n" + ex.Message,
                                "Reports", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>This is where the printer is asked for the paper size chosen in "Office Hours", if it has that size.</summary>
        private void SelectPaper(PrintDocument document)
        {
            PaperKind wanted;
            switch (_paper)
            {
                case DtrPaper.Letter: wanted = PaperKind.Letter; break;
                case DtrPaper.Long: wanted = PaperKind.Folio; break;
                case DtrPaper.Legal: wanted = PaperKind.Legal; break;
                default: wanted = PaperKind.A4; break;
            }
            foreach (PaperSize size in document.PrinterSettings.PaperSizes)
            {
                if (size.Kind == wanted)
                {
                    document.DefaultPageSettings.PaperSize = size;
                    break;
                }
            }
        }

        /// <summary>This is "Do you want to open it now?" after a file was saved.</summary>
        private void OfferToOpen(string path, string savedText)
        {
            if (MessageBox.Show(this, savedText + "\n\n" + path + "\n\nDo you want to open it now?",
                                "Reports", MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;
            try
            {
                var start = new System.Diagnostics.ProcessStartInfo(path);
                start.UseShellExecute = true;   // This is to let Windows pick the program
                System.Diagnostics.Process.Start(start);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                MessageBox.Show(this, "The file was saved, but it could not be opened.\n\n" + ex.Message,
                                "Reports", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>This is "Save to Excel...": all five reports in one workbook.</summary>
        private void Save_Click(object? sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save the attendance reports";
                dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx";
                dialog.FileName = "Attendance Reports " + _monthText + ".xlsx";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                if (Directory.Exists(_startFolder))
                    dialog.InitialDirectory = _startFolder;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = dialog.FileName;
                if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    path += ".xlsx";
                try
                {
                    XlsxWriter.SaveReports(path, _reports, _paper);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "The file could not be saved.\n\nIf a file with the same name is open in Excel, close it first.\n\n" +
                                    ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _savedFolder = Path.GetDirectoryName(path) ?? "";
                OfferToOpen(path, "The reports were saved here:");
            }
        }
    }
}
