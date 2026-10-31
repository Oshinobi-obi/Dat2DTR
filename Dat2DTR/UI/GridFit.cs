namespace Dat2DTR
{
    /// <summary>
    /// This is what makes a table fit the screen it is shown on. Column widths and heights typed in pixels
    /// are too small when Windows shows text bigger (display scaling 125%, 150%...), so the
    /// headings get cut off ("NDERTIME"). After the window has opened, this measures the real
    /// text and makes:
    ///   - the heading row as tall as its headings, wrapped to the column widths,
    ///   - every column at least as wide as the longest word of its heading,
    ///   - the narrow columns at least as wide as their values (times, numbers, list choices),
    ///   - the rows tall enough for their text.
    /// A window bigger than the screen is made to fit the screen.
    /// It is done again when the text size changes (for example when the window is moved to
    /// another monitor with a different scaling).
    /// </summary>
    internal static class GridFit
    {
        /// <summary>
        /// This is what to put in a column's Tag for a column of free text (remarks, reasons, results):
        /// it only has to fit its heading, not its values.
        /// </summary>
        public const string FreeText = "free-text";

        public static void Attach(Form form, DataGridView grid)
        {
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            form.Load += (sender, e) => { KeepOnScreen(form); Later(grid, true); };
            grid.FontChanged += (sender, e) => Later(grid, true);
            // This is because a wider or narrower column wraps its heading differently.
            grid.ColumnWidthChanged += (sender, e) => Later(grid, false);
            grid.Resize += (sender, e) => Later(grid, false);
        }

        // This is the tables that already have a fitting waiting to be done (so it is not queued many times):
        // true = the whole fitting, false = only the heading height.
        private static readonly Dictionary<DataGridView, bool> Waiting = new Dictionary<DataGridView, bool>();

        /// <summary>
        /// This is where the fitting is run a moment later, when the table has finished what it is doing.
        /// Windows does not allow the heading height or a column width to be changed while the
        /// table is still stretching its "fill" column ("This operation cannot be performed while
        /// an auto-filled column is being resized"), so it is never done in the middle of that.
        /// </summary>
        /// <param name="whole">This is true for the whole fitting (Fit), false for only the heading height.</param>
        private static void Later(DataGridView grid, bool whole)
        {
            if (!grid.IsHandleCreated || grid.IsDisposed)
                return;
            bool queuedWhole;
            if (Waiting.TryGetValue(grid, out queuedWhole))
            {
                // This is when it is already queued: that one will see the newest sizes. Make it the whole fitting if needed.
                if (whole && !queuedWhole) Waiting[grid] = true;
                return;
            }
            Waiting[grid] = whole;
            grid.BeginInvoke(new Action(() =>
            {
                bool doWhole = Waiting[grid];
                Waiting.Remove(grid);
                if (grid.IsDisposed) return;
                try
                {
                    if (doWhole) Fit(grid);
                    else FitHeadingHeight(grid);
                }
                catch (InvalidOperationException)
                {
                    // This is when the table was busy after all: the sizes stay as they are. Nothing is lost,
                    // the next resize fits it again.
                }
            }));
        }

        /// <summary>
        /// This is where an instruction label is made as tall as its (wrapped) text, and the panel it sits in tall
        /// enough to show it all - also after the window is made narrower or the text bigger.
        /// </summary>
        public static void GrowWithText(Form form, Label label, Control panel)
        {
            EventHandler fit = (sender, e) =>
            {
                Size size = TextRenderer.MeasureText(label.Text, label.Font, new Size(Math.Max(1, label.Width), int.MaxValue),
                                                     TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                if (label.Height != size.Height) label.Height = size.Height;
                int panelHeight = label.Bottom + label.Font.Height / 2;
                if (panel.Height != panelHeight) panel.Height = panelHeight;
            };
            form.Load += fit;
            label.Resize += fit;
            label.FontChanged += fit;
        }

        /// <summary>
        /// This is for a window made for a big screen: it can be wider than a small laptop screen once Windows makes the
        /// text bigger. Then it is made as big as the screen allows and put back in the middle of it.
        /// </summary>
        public static void KeepOnScreen(Form form)
        {
            if (form.WindowState == FormWindowState.Maximized)
                return;   // This is a maximized window: it already fills the screen
            Rectangle area = Screen.FromControl(form).WorkingArea;
            if (form.Width <= area.Width && form.Height <= area.Height)
                return;
            form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
            form.Location = new Point(area.Left + (area.Width - form.Width) / 2, area.Top + (area.Height - form.Height) / 2);
        }

        /// <summary>This is where the heading row is made as tall as the tallest heading, wrapped to its column width.</summary>
        public static void FitHeadingHeight(DataGridView grid)
        {
            Font headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
            int padding = TextRenderer.MeasureText("MM", headerFont).Width;
            int lineHeight = TextRenderer.MeasureText("Ag", headerFont).Height;
            int tallest = lineHeight;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible) continue;
                int width = Math.Max(1, column.Width - padding / 2);
                Size size = TextRenderer.MeasureText(column.HeaderText ?? "", headerFont, new Size(width, int.MaxValue),
                                                     TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                tallest = Math.Max(tallest, size.Height);
            }
            int height = Math.Max(lineHeight + padding / 2, tallest + padding / 2);
            if (grid.ColumnHeadersHeight == height)
                return;
            try
            {
                grid.ColumnHeadersHeight = Math.Min(height, 32767);
            }
            catch (InvalidOperationException)
            {
                // This is when the table is busy stretching a column: try again once it is done.
                Later(grid, false);
            }
        }

        public static void Fit(DataGridView grid)
        {
            Font headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
            Font cellFont = grid.DefaultCellStyle.Font ?? grid.Font;
            using (var bold = new Font(cellFont, FontStyle.Bold))   // This is because changed boxes are bold
            {
                int padding = TextRenderer.MeasureText("MM", cellFont).Width;   // This is a gap that grows with the text size
                int listArrow = SystemInformation.VerticalScrollBarWidth;

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    // This is the longest word of the heading: it must fit (a heading wraps between words, not inside one).
                    int need = 0;
                    foreach (string word in column.HeaderText.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        need = Math.Max(need, TextRenderer.MeasureText(word, headerFont).Width);
                    need += padding;

                    bool freeText = Equals(column.Tag, FreeText) ||
                                    column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill;
                    if (column is DataGridViewCheckBoxColumn)
                    {
                        need = Math.Max(need, padding * 2);
                    }
                    else if (column is DataGridViewComboBoxColumn)
                    {
                        // This is every choice in the list, plus the arrow.
                        var list = (DataGridViewComboBoxColumn)column;
                        foreach (object item in list.Items)
                            need = Math.Max(need, TextRenderer.MeasureText(Convert.ToString(item) ?? "", bold).Width + padding + listArrow);
                    }
                    else if (!freeText)
                    {
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            string text = Convert.ToString(row.Cells[column.Index].Value) ?? "";
                            if (text.Length > 0)
                                need = Math.Max(need, TextRenderer.MeasureText(text, bold).Width + padding);
                        }
                    }

                    column.MinimumWidth = Math.Max(2, need);
                    if (column.AutoSizeMode != DataGridViewAutoSizeColumnMode.Fill && column.Width < need)
                        column.Width = need;
                }

                FitHeadingHeight(grid);

                // This is rows tall enough for the text.
                int rowHeight = Math.Max(grid.RowTemplate.Height, TextRenderer.MeasureText("Ag", bold).Height + padding / 3);
                grid.RowTemplate.Height = rowHeight;
                foreach (DataGridViewRow row in grid.Rows)
                    if (row.Height < rowHeight) row.Height = rowHeight;
            }
        }
    }
}
