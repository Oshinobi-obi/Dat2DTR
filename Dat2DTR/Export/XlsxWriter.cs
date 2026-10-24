using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Dat2DTR
{
    /// <summary>
    /// This is where the DTRs are written as an Excel workbook (.xlsx), one worksheet per employee.
    ///
    /// An .xlsx file is really a ZIP file with a few XML text files inside, so it
    /// can be written with what .NET already has - no Excel and no extra library needed.
    ///
    /// DTR worksheet layout (columns A to I):
    ///   A = DAY, B/C = A.M. arrival/departure, D/E = P.M. arrival/departure,
    ///   F/G = LATE hours/minutes, H/I = UNDERTIME hours/minutes
    ///
    /// Leave card worksheet layout (columns B to N, the same as the leave card kept by Personnel):
    ///   B = month, C to J = VACATION (Earned, Enjoyed, Net, LWOP, SPL, Leave Date/s, Late, Undertime),
    ///   K to N = SICK (Earned, Enjoyed, Net, Leave Date/s)
    /// </summary>
    internal static class XlsxWriter
    {
        // This is the list of cell styles. The numbers are positions in the <cellXfs> list in StylesXml below.
        private const int StyleNormal = 0;
        private const int StyleTitle = 1;         // This is big bold
        private const int StyleFormNumber = 2;    // This is small bold, right aligned
        private const int StyleBold = 3;
        private const int StyleGridCenter = 4;    // This is bold, centred, with borders
        private const int StyleGridLeft = 5;      // This is bold, left, with borders (not used for times any more: they are centred)
        private const int StyleGridSunday = 6;    // This is like GridCenter but red
        private const int StyleGridTotal = 7;     // This is "TOTAL"
        private const int StyleParagraph = 8;     // This is bold, wraps
        private const int StyleSmallBold = 9;
        private const int StyleUnderlined = 10;   // This is bold, centred, line underneath
        private const int StyleRuleAbove = 11;    // This is small bold with a thick line above

        // This is the styles of the leave card worksheet (Calibri 11, like the leave card kept by Personnel).
        private const int LeaveText = 12;         // This is plain
        private const int LeaveBold = 13;
        private const int LeaveHead = 14;         // This is a table heading: bold, centred, borders, grey background
        private const int LeaveCell = 15;         // This is centred, borders
        private const int LeaveNumber = 16;       // This is like LeaveCell, shown with 3 decimals
        private const int LeaveCellNow = 17;      // This is the open month: bold on pale yellow
        private const int LeaveNumberNow = 18;
        private const int LeaveRight = 19;        // This is plain, right aligned
        private const int LeaveBoldRight = 20;
        private const int LeaveWrap = 21;         // This is plain, wraps
        private const int LeaveMonthName = 22;    // This is left, borders
        private const int LeaveMonthNameNow = 23;
        private const int LeaveCenter = 24;       // This is plain, centred
        private const int LeaveDates = 25;        // This is like LeaveCell, but a long list of days is made smaller to fit
        private const int LeaveDatesNow = 26;
        private const int StyleSmallBoldCenter = 27;  // This is "Signature over Printed Name"
        private const int StyleBalance = 28;          // This is the VL / SL balance: bold, centred, 3 decimals, line underneath
        private const int LeaveBoldCenter = 29;       // This is bold, centred (the History of Availment heading)
        private const int LeaveLineBelow = 30;        // This is an empty cell with a line underneath (the line across the page)

        private const int FirstDayRow = 9;
        private const int LastDayRow = 39;        // This is 31 rows
        private const int TotalRow = 40;

        /// <param name="cards">
        /// This is the leave card of each employee, in the same order as the DTRs. Each one becomes a
        /// worksheet right after the employee's DTR. null = DTR worksheets only.
        /// </param>
        /// <param name="includeDtr">This is false for only the leave card worksheets.</param>
        public static void Save(string path, IList<DtrSheet> sheets, DtrPaper paper, IList<LeaveCard>? cards, bool includeDtr)
        {
            // This is the worksheets in the order they appear in the file: name and content.
            var names = new List<string>();
            var contents = new List<string>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < sheets.Count; i++)
            {
                DtrSheet sheet = sheets[i];
                string who = sheet.EmployeeName.Length > 0 ? sheet.EmployeeName : "ID " + sheet.EmployeeId;

                if (includeDtr || cards == null)
                {
                    names.Add(MakeSheetName("", who, usedNames));
                    contents.Add(WorksheetXml(sheet, paper));
                }

                if (cards != null && i < cards.Count)
                {
                    names.Add(MakeSheetName("Leave - ", who, usedNames));
                    contents.Add(LeaveWorksheetXml(cards[i], paper));
                }
            }

            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                AddFile(zip, "[Content_Types].xml", ContentTypesXml(contents.Count));
                AddFile(zip, "_rels/.rels", RootRelsXml);
                AddFile(zip, "xl/workbook.xml", WorkbookXml(names));
                AddFile(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(contents.Count));
                AddFile(zip, "xl/styles.xml", StylesXml);

                for (int i = 0; i < contents.Count; i++)
                    AddFile(zip, "xl/worksheets/sheet" + (i + 1) + ".xml", contents[i]);
            }
        }

        private static void AddFile(ZipArchive zip, string name, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                writer.Write(content);
        }

        // This is the DTR worksheet ----------------------------------------------------------------

        private static string WorksheetXml(DtrSheet sheet, DtrPaper paper)
        {
            var rows = new StringBuilder();
            var merges = new List<string>();

            // This is the heading ---
            rows.Append(Row(1, 20,
                Text("A1", "DAILY TIME RECORD", StyleTitle),
                Text("F1", "CIVIL SERVICE FORM No. 48", StyleFormNumber)));
            merges.Add("A1:E1");
            merges.Add("F1:I1");

            rows.Append(Row(2, 18, Text("A2", "Name: " + sheet.NameLine, StyleBold)));
            merges.Add("A2:I2");

            rows.Append(Row(3, 16.5,
                Text("A3", "For the month of", StyleNormal),
                Text("C3", sheet.MonthText, StyleUnderlined),
                Blank("D3", StyleUnderlined)));
            merges.Add("A3:B3");
            merges.Add("C3:D3");

            rows.Append(Row(4, 15,
                Text("A4", "Official hours for arrival", StyleNormal),
                Text("F4", "( Regular days", StyleNormal),
                Text("H4", sheet.OfficialHours, StyleUnderlined),
                Blank("I4", StyleUnderlined)));
            merges.Add("A4:C4");
            merges.Add("F4:G4");
            merges.Add("H4:I4");

            rows.Append(Row(5, 15,
                Text("A5", "      and departure", StyleNormal),
                Text("F5", "( Saturdays", StyleNormal),
                Text("H5", "Service as required", StyleUnderlined),
                Blank("I5", StyleUnderlined)));
            merges.Add("A5:C5");
            merges.Add("F5:G5");
            merges.Add("H5:I5");

            rows.Append(Row(6, 6));

            // This is the table headings ---
            rows.Append(Row(7, 16.5,
                Blank("A7", StyleGridCenter),
                Text("B7", "A. M.", StyleGridCenter), Blank("C7", StyleGridCenter),
                Text("D7", "P. M.", StyleGridCenter), Blank("E7", StyleGridCenter),
                Text("F7", "L A T E", StyleGridCenter), Blank("G7", StyleGridCenter),
                Text("H7", "U N D E R T I M E", StyleGridCenter), Blank("I7", StyleGridCenter)));
            merges.Add("B7:C7");
            merges.Add("D7:E7");
            merges.Add("F7:G7");
            merges.Add("H7:I7");

            rows.Append(Row(8, 16.5,
                Text("A8", "DAY", StyleGridCenter),
                Text("B8", "Arrival", StyleGridCenter), Text("C8", "Departure", StyleGridCenter),
                Text("D8", "Arrival", StyleGridCenter), Text("E8", "Departure", StyleGridCenter),
                Text("F8", "Hours", StyleGridCenter), Text("G8", "Minutes", StyleGridCenter),
                Text("H8", "Hours", StyleGridCenter), Text("I8", "Minutes", StyleGridCenter)));

            // This is one row per day (always 31 rows, like the paper form) ---
            for (int r = FirstDayRow; r <= LastDayRow; r++)
            {
                int index = r - FirstDayRow;
                DtrDay? day = index < sheet.Days.Count ? sheet.Days[index] : null;

                var cells = new List<string>();
                if (day == null)
                {
                    foreach (char column in "ABCDEFGHI")
                        cells.Add(Blank(column.ToString() + r, StyleGridCenter));
                }
                else
                {
                    string label = day.RowLabel; // This is "A B S E N T", a leave, or a remark such as HOLIDAY
                    cells.Add(Number("A" + r, day.Day, StyleGridCenter));

                    if (label.Length > 0 && day.HasLeave)
                    {
                        // This is a leave: it is written across the four time columns.
                        cells.Add(Text("B" + r, label, StyleGridCenter));
                        cells.Add(Blank("C" + r, StyleGridCenter));
                        cells.Add(Blank("D" + r, StyleGridCenter));
                        cells.Add(Blank("E" + r, StyleGridCenter));
                        merges.Add("B" + r + ":E" + r);
                    }
                    else if (label.Length > 0)
                    {
                        cells.Add(Blank("B" + r, StyleGridCenter));
                        cells.Add(Text("C" + r, label, StyleGridCenter));
                        cells.Add(Blank("D" + r, StyleGridCenter));
                        cells.Add(Blank("E" + r, StyleGridCenter));
                    }
                    else
                    {
                        // This is a half-day leave: the half that is on leave shows the leave across its two columns.
                        string morningLeave = day.HalfLabel(true), afternoonLeave = day.HalfLabel(false);

                        if (morningLeave.Length > 0)
                        {
                            cells.Add(Text("B" + r, morningLeave, StyleGridCenter));
                            cells.Add(Blank("C" + r, StyleGridCenter));
                            merges.Add("B" + r + ":C" + r);
                        }
                        else
                        {
                            cells.Add(Text("B" + r, DtrSheet.FormatFormTime(day.AmArrival), StyleGridCenter));
                            cells.Add(Text("C" + r, DtrSheet.FormatFormTime(day.AmDeparture), StyleGridCenter));
                        }

                        if (afternoonLeave.Length > 0)
                        {
                            cells.Add(Text("D" + r, afternoonLeave, StyleGridCenter));
                            cells.Add(Blank("E" + r, StyleGridCenter));
                            merges.Add("D" + r + ":E" + r);
                        }
                        else
                        {
                            cells.Add(Text("D" + r, DtrSheet.FormatFormTime(day.PmArrival), StyleGridCenter));
                            cells.Add(Text("E" + r, DtrSheet.FormatFormTime(day.PmDeparture), StyleGridCenter));
                        }
                    }

                    cells.Add(NumberOrBlank("F" + r, day.LateMinutes / 60));
                    cells.Add(NumberOrBlank("G" + r, day.LateMinutes % 60));
                    cells.Add(NumberOrBlank("H" + r, day.UndertimeMinutes / 60));

                    if (day.Remark.Length > 0)
                        cells.Add(Text("I" + r, day.Remark, day.Remark == "Sun" ? StyleGridSunday : StyleGridCenter));
                    else
                        cells.Add(NumberOrBlank("I" + r, day.UndertimeMinutes % 60));
                }
                rows.Append(Row(r, 16.5, cells.ToArray()));
            }

            // This is the TOTAL row. These are real Excel formulas, so if someone corrects a
            // number by hand in Excel, the totals update by themselves.
            string lateSum = "SUM(F9:F39)*60+SUM(G9:G39)";
            string underSum = "SUM(H9:H39)*60+SUM(I9:I39)";
            rows.Append(Row(TotalRow, 18,
                Text("A40", "TOTAL", StyleGridTotal),
                Blank("B40", StyleGridTotal), Blank("C40", StyleGridTotal),
                Blank("D40", StyleGridTotal), Blank("E40", StyleGridTotal),
                Formula("F40", "INT((" + lateSum + ")/60)", sheet.TotalLateMinutes / 60),
                Formula("G40", "MOD(" + lateSum + ",60)", sheet.TotalLateMinutes % 60),
                Formula("H40", "INT((" + underSum + ")/60)", sheet.TotalUndertimeMinutes / 60),
                Formula("I40", "MOD(" + underSum + ",60)", sheet.TotalUndertimeMinutes % 60)));
            merges.Add("A40:E40");

            // This is the certification and signatures ---
            rows.Append(Row(41, 8));
            rows.Append(Row(42, 14, Text("A42",
                "      I CERTIFY on my honor that the above is a true and correct report of the hours of work performed, " +
                "record of which was made daily at the time of arrival at and departure from office.", StyleParagraph)));
            rows.Append(Row(43, 14));
            merges.Add("A42:I43");

            rows.Append(Row(44, 8));

            var ruleCells = new List<string>();
            ruleCells.Add(Text("A45", "Verified as to the prescribed office hours", StyleRuleAbove));
            foreach (char column in "BCDEFGHI")
                ruleCells.Add(Blank(column + "45", StyleRuleAbove));
            rows.Append(Row(45, 15, ruleCells.ToArray()));

            // This is the two places to sign: at the bottom right, and in the middle below it.
            rows.Append(Row(46, 15));
            rows.Append(Row(47, 15, Blank("G47", StyleUnderlined), Blank("H47", StyleUnderlined), Blank("I47", StyleUnderlined)));
            rows.Append(Row(48, 13, Text("G48", "Signature over Printed Name", StyleSmallBoldCenter)));
            merges.Add("G48:I48");
            rows.Append(Row(49, 15));
            rows.Append(Row(50, 15, Text("A50", "(See instruction on back)", StyleSmallBold),
                                    Blank("C50", StyleUnderlined), Blank("D50", StyleUnderlined), Blank("E50", StyleUnderlined)));
            rows.Append(Row(51, 13, Text("C51", "Signature over Printed Name", StyleSmallBoldCenter)));
            merges.Add("C51:E51");

            // This is where the worksheet is put together ---
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            xml.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            xml.Append("<dimension ref=\"A1:I51\"/>");
            xml.Append("<sheetViews><sheetView showGridLines=\"0\" workbookViewId=\"0\"/></sheetViews>");
            xml.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
            xml.Append("<cols>");
            xml.Append("<col min=\"1\" max=\"1\" width=\"5.5\" customWidth=\"1\"/>");
            xml.Append("<col min=\"2\" max=\"5\" width=\"12.5\" customWidth=\"1\"/>");
            xml.Append("<col min=\"6\" max=\"9\" width=\"8.5\" customWidth=\"1\"/>");
            xml.Append("</cols>");
            xml.Append("<sheetData>").Append(rows).Append("</sheetData>");

            xml.Append("<mergeCells count=\"").Append(merges.Count).Append("\">");
            foreach (string range in merges)
                xml.Append("<mergeCell ref=\"").Append(range).Append("\"/>");
            xml.Append("</mergeCells>");

            xml.Append("<printOptions horizontalCentered=\"1\"/>");
            xml.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>");
            xml.Append("<pageSetup paperSize=\"").Append(PaperInfo.ExcelCodeOf(paper))
               .Append("\" orientation=\"portrait\" fitToWidth=\"1\" fitToHeight=\"1\"/>");
            xml.Append("</worksheet>");
            return xml.ToString();
        }

        // This is the leave card worksheet ----------------------------------------------------------------

        private const int LeaveFirstMonthRow = 8;   // This is January. December is row 19.

        private static string LeaveWorksheetXml(LeaveCard card, DtrPaper paper)
        {
            var rows = new StringBuilder();
            var merges = new List<string>();
            const string columns = "BCDEFGHIJKLMNOP";

            // This is who (the same three lines as the leave card kept by Personnel) ---
            // This is on the right: the Vacation Leave and Sick Leave left at the end of the month (empty when not known).
            LeaveMonth open = card.Current;
            rows.Append(Row(2, 16.5, Text("B2", "Name of Employee:", LeaveText), Text("D2", card.NameLine, LeaveBold),
                                     Text("N2", "For the month of " + card.MonthText, LeaveBoldRight)));
            rows.Append(Row(3, 16.5, Text("B3", "Office:", LeaveText),
                                     Text("N3", "VL", LeaveBoldRight), BalanceCell("O3", open.VlNet), Blank("P3", StyleUnderlined)));
            rows.Append(Row(4, 16.5, Text("B4", "First Day of Service", LeaveText),
                                     Text("N4", "SL", LeaveBoldRight), BalanceCell("O4", open.SlNet), Blank("P4", StyleUnderlined)));
            merges.Add("N2:P2");
            for (int r = 2; r <= 4; r++)
            {
                merges.Add("B" + r + ":C" + r);
                merges.Add("D" + r + ":L" + r);
            }
            merges.Add("O3:P3");
            merges.Add("O4:P4");

            // This is the table headings ---
            var head1 = new List<string>();
            head1.Add(Number("B6", card.Year, LeaveHead));
            head1.Add(Text("C6", "VACATION", LeaveHead));
            foreach (char c in "DEFGHIJKL") head1.Add(Blank(c + "6", LeaveHead));
            head1.Add(Text("M6", "SICK", LeaveHead));
            foreach (char c in "NOP") head1.Add(Blank(c + "6", LeaveHead));
            rows.Append(Row(6, 16.5, head1.ToArray()));
            merges.Add("B6:B7");
            merges.Add("C6:L6");
            merges.Add("M6:P6");

            string[] names = { "", "Earned", "Enjoyed", "Net", "Leave Date/s", "SPL", "WL", "CTO", "SOLO", "Late", "Undertime",
                               "Earned", "Enjoyed", "Net", "Leave Date/s" };
            var head2 = new List<string>();
            for (int i = 0; i < columns.Length; i++)
                head2.Add(Text(columns[i] + "7", names[i], LeaveHead));
            rows.Append(Row(7, 16.5, head2.ToArray()));

            // This is one row per month. "Net" is a real Excel formula (previous Net + Earned - Enjoyed),
            // the same way the leave card kept by Personnel works. When the balance is not known,
            // Net is left empty (never a number that only looks like a balance).
            int current = card.MonthStart.Month - 1;

            for (int i = 0; i < 12; i++)
            {
                int r = LeaveFirstMonthRow + i;
                bool now = i == current;
                int cellStyle = now ? LeaveCellNow : LeaveCell;
                int numberStyle = now ? LeaveNumberNow : LeaveNumber;
                int datesStyle = now ? LeaveDatesNow : LeaveDates;

                LeaveMonth? m = card.Months[i];
                LeaveMonth? next = i < 11 ? card.Months[i + 1] : null;
                string monthName = new DateTime(card.Year, i + 1, 1).ToString("MMMM", CultureInfo.InvariantCulture);

                var cells = new List<string>();
                cells.Add(Text("B" + r, monthName, now ? LeaveMonthNameNow : LeaveMonthName));

                if (m == null)
                {
                    // This is a month the program knows nothing about. If the next month starts from a known
                    // balance, that balance is written here as "Net", so the next row's formula can use it.
                    decimal vlShown = next != null && next.VlStart.HasValue ? next.VlStart.Value : 0m;
                    decimal slShown = next != null && next.SlStart.HasValue ? next.SlStart.Value : 0m;

                    foreach (char c in "CDEFGHIJKLMNOP")
                    {
                        if (c == 'E') cells.Add(AmountOrBlank("E" + r, vlShown, numberStyle));
                        else if (c == 'O') cells.Add(AmountOrBlank("O" + r, slShown, numberStyle));
                        else cells.Add(Blank(c.ToString() + r, cellStyle));
                    }
                    rows.Append(Row(r, 16.5, cells.ToArray()));
                    continue;
                }

                bool previousRowIsMonth = i > 0 && card.Months[i - 1] != null;

                // This is the VACATION part ----
                cells.Add(Amount("C" + r, m.VlEarned, numberStyle));

                // This is Enjoyed = leave + late + undertime, written out as a sum (for example =2.5+0.406+0.625)
                // the way it is typed on the leave card. It does not point at other cells, so the cell
                // still gives the right number when it is copied into another workbook.
                var enjoyedParts = new List<string>();
                if (m.VlLeave + m.SlToVl > 0m) enjoyedParts.Add(Plain(m.VlLeave + m.SlToVl));
                if (m.LateDays > 0m) enjoyedParts.Add(Plain(m.LateDays));
                if (m.UndertimeDays > 0m) enjoyedParts.Add(Plain(m.UndertimeDays));

                if (m.VlEnjoyed == 0m)
                    cells.Add(Blank("D" + r, numberStyle));
                else if (m.Lwop > 0m || enjoyedParts.Count < 2)
                    cells.Add(Amount("D" + r, m.VlEnjoyed, numberStyle));   // This is one number (or cut off at the balance)
                else
                    cells.Add(Formula("D" + r, string.Join("+", enjoyedParts.ToArray()), m.VlEnjoyed, numberStyle));

                NetCell(cells, 'E', 'C', 'D', r, i, previousRowIsMonth, m.VlStartTyped, m.VlStart, m.VlNet, numberStyle);

                cells.Add(Text("F" + r, m.VlDatesText, datesStyle));
                cells.Add(m.SplDays > 0m ? Amount("G" + r, m.SplDays, cellStyle) : Blank("G" + r, cellStyle));
                cells.Add(m.WellnessDays > 0m ? Amount("H" + r, m.WellnessDays, cellStyle) : Blank("H" + r, cellStyle));
                cells.Add(m.CtoDays > 0m ? Amount("I" + r, m.CtoDays, cellStyle) : Blank("I" + r, cellStyle));
                cells.Add(m.SoloDays > 0m ? Amount("J" + r, m.SoloDays, cellStyle) : Blank("J" + r, cellStyle));
                cells.Add(AmountOrBlank("K" + r, m.LateDays, numberStyle));
                cells.Add(AmountOrBlank("L" + r, m.UndertimeDays, numberStyle));

                // This is the SICK part ----
                cells.Add(Amount("M" + r, m.SlEarned, numberStyle));
                cells.Add(AmountOrBlank("N" + r, m.SlEnjoyed, numberStyle));
                NetCell(cells, 'O', 'M', 'N', r, i, previousRowIsMonth, m.SlStartTyped, m.SlStart, m.SlNet, numberStyle);
                cells.Add(Text("P" + r, m.SickDates, datesStyle));

                rows.Append(Row(r, 16.5, cells.ToArray()));
            }

            // This is used this year, under the table ---
            int row = LeaveFirstMonthRow + 13;
            string through = card.MonthStart.Month == 1 ? "January" : "January to " + card.Current.MonthName;
            rows.Append(Row(row, 16.5, Text("B" + row,
                "Used this year (" + through + "):     " +
                "Special Privilege Leave  " + LeaveCalculator.UsedText(card.SplUsed, LeaveType.SpecialPrivilege).Replace(" this year", "") + "      " +
                "Forced Leave  " + LeaveCalculator.UsedText(card.ForcedUsed, LeaveType.Forced).Replace(" this year", "") + "      " +
                "Wellness Leave  " + LeaveCalculator.UsedText(card.WellnessUsed, LeaveType.Wellness).Replace(" this year", "") + "      " +
                LeavePageRenderer.GivenText(card), LeaveBold)));
            row += 2;

            // This is how the open month was computed, in two columns like the printed page:
            // Vacation and Sick Leave on the left (B to H), SPL, Wellness Leave and CTO on the right (K to P),
            // Solo Parent Leave in the shorter one.
            rows.Append(Row(row, 16.5, Text("B" + row, "How " + card.MonthText + " was computed", LeaveBold)));
            row++;
            rows.Append(Row(row, 16.5, Text("B" + row, card.ScheduleText + ".   " + card.DayChargeText, LeaveText)));
            row++;

            List<LeaveLine> leftPart, rightPart;
            LeavePageRenderer.SplitColumns(card.Breakdown, out leftPart, out rightPart);
            List<LeaveLine?> leftLines = SpaceOut(leftPart);
            List<LeaveLine?> rightLines = SpaceOut(rightPart);

            for (int k = 0; k < Math.Max(leftLines.Count, rightLines.Count); k++)
            {
                var cells = new List<string>();
                LeaveLine? a = k < leftLines.Count ? leftLines[k] : null;
                LeaveLine? b = k < rightLines.Count ? rightLines[k] : null;
                AddLineCells(cells, merges, a, row, 'B', 'F', 'G', 'H');
                AddLineCells(cells, merges, b, row, 'K', 'N', 'O', 'P');
                rows.Append(cells.Count == 0 ? Row(row, 6) : Row(row, 16.5, cells.ToArray()));
                row++;
            }

            // This is the History of Availment of Leave: a line across the page, the heading in the middle,
            // and one row per month (January to December) with the days each leave was used.
            row++;
            var line = new List<string>();
            foreach (char c in columns) line.Add(Blank(c.ToString() + row, LeaveLineBelow));
            rows.Append(Row(row, 8, line.ToArray()));
            row += 2;
            rows.Append(Row(row, 18, Text("B" + row, "HISTORY OF AVAILMENT OF LEAVE  -  JANUARY TO DECEMBER " + card.Year, LeaveBoldCenter)));
            merges.Add("B" + row + ":P" + row);
            row += 2;
            row = HistoryRows(rows, merges, card, row);

            // This is where the worksheet is put together ---
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            xml.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            xml.Append("<dimension ref=\"A1:P").Append(row).Append("\"/>");
            xml.Append("<sheetViews><sheetView showGridLines=\"0\" workbookViewId=\"0\"/></sheetViews>");
            xml.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
            xml.Append("<cols>");
            xml.Append("<col min=\"1\" max=\"1\" width=\"2.5\" customWidth=\"1\"/>");     // This is A  (empty)
            xml.Append("<col min=\"2\" max=\"2\" width=\"14.7\" customWidth=\"1\"/>");    // This is B  month
            xml.Append("<col min=\"3\" max=\"5\" width=\"9.5\" customWidth=\"1\"/>");     // This is C-E  Earned, Enjoyed, Net
            xml.Append("<col min=\"6\" max=\"6\" width=\"20\" customWidth=\"1\"/>");      // This is F  Leave Date/s
            xml.Append("<col min=\"7\" max=\"10\" width=\"6.5\" customWidth=\"1\"/>");    // This is G-J  SPL, WL, CTO, SOLO
            xml.Append("<col min=\"11\" max=\"12\" width=\"11.5\" customWidth=\"1\"/>");  // This is K-L  Late, Undertime
            xml.Append("<col min=\"13\" max=\"15\" width=\"9.5\" customWidth=\"1\"/>");   // This is M-O  Earned, Enjoyed, Net (SICK)
            xml.Append("<col min=\"16\" max=\"16\" width=\"18\" customWidth=\"1\"/>");    // This is P  Leave Date/s
            xml.Append("</cols>");
            xml.Append("<sheetData>").Append(rows).Append("</sheetData>");

            xml.Append("<mergeCells count=\"").Append(merges.Count).Append("\">");
            foreach (string range in merges)
                xml.Append("<mergeCell ref=\"").Append(range).Append("\"/>");
            xml.Append("</mergeCells>");

            xml.Append("<printOptions horizontalCentered=\"1\"/>");
            xml.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>");
            xml.Append("<pageSetup paperSize=\"").Append(PaperInfo.ExcelCodeOf(paper))
               .Append("\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"1\"/>");
            xml.Append("</worksheet>");
            return xml.ToString();
        }

        /// <summary>
        /// This is the History of Availment of Leave table, from the given row: a heading row, January to December,
        /// and the total days. Each leave takes the columns below (merged). Returns the row after the table.
        /// </summary>
        private static int HistoryRows(StringBuilder rows, List<string> merges, LeaveCard card, int row)
        {
            // This is Month | Vacation | Forced | Sick | SPL | Wellness | CTO | Solo Parent | Days
            string[] names = { "Month", "Vacation Leave", "Forced Leave", "Sick Leave", "SPL", "Wellness",
                               "CTO", "Solo Parent", "Days" };
            string[] spans = { "B", "CE", "F", "GI", "JK", "L", "M", "NO", "P" };   // This is the first and last column of each

            var head = new List<string>();
            for (int k = 0; k < spans.Length; k++)
                AddSpan(head, merges, spans[k], row, names[k], LeaveHead, false);
            rows.Append(Row(row, 16.5, head.ToArray()));
            row++;

            var sums = new decimal[8];
            int current = card.MonthStart.Month - 1;
            for (int i = 0; i < 12; i++, row++)
            {
                LeaveMonth h = card.History[i] ?? new LeaveMonth();
                bool now = i == current;
                int style = now ? LeaveDatesNow : LeaveDates;
                string monthName = new DateTime(card.Year, i + 1, 1).ToString("MMMM", CultureInfo.InvariantCulture);
                string[] texts = { monthName, h.VacationDates, h.ForcedDates, h.SickDates, h.SplDates, h.WellnessDates,
                                   h.CtoDates, h.SoloDates, h.AllLeaveDays == 0m ? "" : LeaveRules.FormatShort(h.AllLeaveDays) };
                decimal[] days = { h.VacationDays, h.ForcedDays, h.SickDays, h.SplDays, h.WellnessDays, h.CtoDays, h.SoloDays,
                                   h.AllLeaveDays };
                for (int c = 0; c < days.Length; c++) sums[c] += days[c];

                var cells = new List<string>();
                AddSpan(cells, merges, spans[0], row, monthName, now ? LeaveMonthNameNow : LeaveMonthName, false);
                for (int k = 1; k < spans.Length; k++)
                    AddSpan(cells, merges, spans[k], row, texts[k], k == spans.Length - 1 ? (now ? LeaveCellNow : LeaveCell) : style,
                            k == spans.Length - 1 && h.AllLeaveDays > 0m, h.AllLeaveDays);
                rows.Append(Row(row, 16.5, cells.ToArray()));
            }

            var total = new List<string>();
            AddSpan(total, merges, spans[0], row, "Total days", LeaveHead, false);
            for (int k = 1; k < spans.Length; k++)
                AddSpan(total, merges, spans[k], row, "", LeaveHead, true, sums[k - 1]);
            rows.Append(Row(row, 16.5, total.ToArray()));
            return row + 1;
        }

        /// <summary>This is one cell of the history table over the columns in span ("CE" = C to E, merged).</summary>
        private static void AddSpan(List<string> cells, List<string> merges, string span, int row, string text, int style,
                                    bool asNumber, decimal number = 0m)
        {
            char first = span[0], last = span[span.Length - 1];
            string cell = first.ToString() + row;
            cells.Add(asNumber ? Amount(cell, number, style) : Text(cell, text, style));
            for (char c = (char)(first + 1); c <= last; c++)
                cells.Add(Blank(c.ToString() + row, style));
            if (last != first)
                merges.Add(cell + ":" + last + row);
        }

        /// <summary>
        /// This is where the "Net" cell of one month is added: a formula  balance before + Earned - Enjoyed.
        /// When the balance is not known, the cell is left empty.
        /// </summary>
        /// <param name="start">This is the balance at the start of the month (null = not known).</param>
        /// <param name="netValue">This is the balance at the end of the month (null = not known).</param>
        private static void NetCell(List<string> cells, char net, char earned, char enjoyed, int r, int monthIndex,
            bool previousRowIsMonth, bool startTyped, decimal? start, decimal? netValue, int style)
        {
            string cell = net.ToString() + r;
            if (!start.HasValue || !netValue.HasValue)
            {
                cells.Add(Blank(cell, style));
                return;
            }

            string change = "+" + earned + r + "-" + enjoyed + r;
            string formula;

            if (monthIndex > 0 && !(previousRowIsMonth && startTyped))
                formula = net.ToString() + (r - 1) + change;     // This is the usual case: continue from the Net of the row above
            else
                formula = Plain(start.Value) + change;           // This is January, or a balance that a person typed for this month

            cells.Add(Formula(cell, formula, netValue.Value, style));
        }

        /// <summary>This is a balance on a line (bold, underlined, 3 decimals), or just the line when it is not known.</summary>
        private static string BalanceCell(string cell, decimal? value)
        {
            return value.HasValue
                ? "<c r=\"" + cell + "\" s=\"" + StyleBalance + "\"><v>" + Plain(value.Value) + "</v></c>"
                : Blank(cell, StyleBalance);
        }

        /// <summary>This is the lines of one column, with an empty line (null) before every heading but the first.</summary>
        private static List<LeaveLine?> SpaceOut(List<LeaveLine> lines)
        {
            var result = new List<LeaveLine?>();
            foreach (LeaveLine line in lines)
            {
                if (line.Kind == LeaveLineKind.Heading && result.Count > 0)
                    result.Add(null);
                result.Add(line);
            }
            return result;
        }

        /// <summary>This is where one line of the computation is written: the words from textFrom to textTo, the number from valueFrom to valueTo.</summary>
        private static void AddLineCells(List<string> cells, List<string> merges, LeaveLine? line, int row,
                                         char textFrom, char textTo, char valueFrom, char valueTo)
        {
            if (line == null)
                return;

            bool strong = line.Kind != LeaveLineKind.Item;
            string text = line.Kind == LeaveLineKind.Heading ? line.Text : "      " + line.Text;
            cells.Add(Text(textFrom.ToString() + row, text, strong ? LeaveBold : LeaveText));

            // This is a line without a number (a heading): it may use the whole width of its column
            merges.Add(textFrom.ToString() + row + ":" + (line.Value.Length > 0 ? textTo : valueTo) + row);

            if (line.Value.Length > 0)
            {
                cells.Add(Text(valueFrom.ToString() + row, line.Value, strong ? LeaveBoldRight : LeaveRight));
                merges.Add(valueFrom.ToString() + row + ":" + valueTo + row);
            }
        }

        // This is the part that makes the cells ----------------------------------------------------------------

        private static string Row(int number, double height, params string[] cells)
        {
            return "<row r=\"" + number + "\" ht=\"" + height.ToString(CultureInfo.InvariantCulture) +
                   "\" customHeight=\"1\">" + string.Concat(cells) + "</row>";
        }

        private static string Text(string cell, string text, int style)
        {
            if (text.Length == 0)
                return Blank(cell, style);
            return "<c r=\"" + cell + "\" s=\"" + style + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" +
                   Escape(text) + "</t></is></c>";
        }

        private static string Number(string cell, int value, int style)
        {
            return "<c r=\"" + cell + "\" s=\"" + style + "\"><v>" + value + "</v></c>";
        }

        /// <summary>This is a whole number; zero is left empty, the way it is on the paper form.</summary>
        private static string NumberOrBlank(string cell, int value)
        {
            return value == 0 ? Blank(cell, StyleGridCenter) : Number(cell, value, StyleGridCenter);
        }

        private static string Formula(string cell, string formula, int currentValue)
        {
            return "<c r=\"" + cell + "\" s=\"" + StyleGridCenter + "\"><f>" + Escape(formula) + "</f><v>" +
                   currentValue + "</v></c>";
        }

        private static string Blank(string cell, int style)
        {
            return "<c r=\"" + cell + "\" s=\"" + style + "\"/>";
        }

        /// <summary>This is a number with decimals, e.g. leave credits.</summary>
        private static string Amount(string cell, decimal value, int style)
        {
            return "<c r=\"" + cell + "\" s=\"" + style + "\"><v>" + Plain(value) + "</v></c>";
        }

        /// <summary>This is a number with decimals; zero is left empty, the way it is on the leave card.</summary>
        private static string AmountOrBlank(string cell, decimal value, int style)
        {
            return value == 0m ? Blank(cell, style) : Amount(cell, value, style);
        }

        /// <summary>This is a formula together with the value it has right now.</summary>
        private static string Formula(string cell, string formula, decimal currentValue, int style)
        {
            return "<c r=\"" + cell + "\" s=\"" + style + "\"><f>" + Escape(formula) + "</f><v>" +
                   Plain(currentValue) + "</v></c>";
        }

        /// <summary>This is a number the way Excel formulas want it: 1.25 (always a point, never a comma).</summary>
        private static string Plain(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Escape(string text)
        {
            var sb = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default:
                        // This is where characters that are not allowed in XML are dropped
                        if (c >= 0x20 || c == '\t') sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        // This is the part that makes the worksheet names ----------------------------------------------------------------

        /// <summary>
        /// This is where the name of one worksheet is made. Excel worksheet names must be unique,
        /// at most 31 characters and must not contain  : \ / ? * [ ]
        /// </summary>
        /// <param name="prefix">This is "" for a DTR, "Leave - " for a leave card.</param>
        private static string MakeSheetName(string prefix, string wanted, HashSet<string> used)
        {
            var clean = new StringBuilder();
            foreach (char c in wanted)
                clean.Append(":\\/?*[]".IndexOf(c) >= 0 ? ' ' : c);

            string baseName = clean.ToString().Trim().Trim('\'');
            if (baseName.Length == 0) baseName = "DTR";
            baseName = prefix + baseName;
            if (baseName.Length > 31)
                baseName = baseName.Substring(0, 31).Trim().Trim('\'');   // This is because a name must not end with an apostrophe either

            string name = baseName;
            for (int n = 2; used.Contains(name); n++)
            {
                string suffix = " (" + n + ")";
                int keep = Math.Min(baseName.Length, 31 - suffix.Length);
                name = baseName.Substring(0, keep).Trim().Trim('\'') + suffix;
            }

            used.Add(name);
            return name;
        }

        // This is the fixed parts of the file ----------------------------------------------------------------

        private const string RootRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private static string ContentTypesXml(int sheetCount)
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            xml.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            xml.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            xml.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            xml.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            xml.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int i = 1; i <= sheetCount; i++)
                xml.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i)
                   .Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            xml.Append("</Types>");
            return xml.ToString();
        }

        private static string WorkbookXml(List<string> sheetNames)
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            xml.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
            xml.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            xml.Append("<bookViews><workbookView/></bookViews>");
            xml.Append("<sheets>");
            for (int i = 0; i < sheetNames.Count; i++)
                xml.Append("<sheet name=\"").Append(Escape(sheetNames[i])).Append("\" sheetId=\"").Append(i + 1)
                   .Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
            xml.Append("</sheets>");
            xml.Append("<calcPr calcId=\"0\" fullCalcOnLoad=\"1\"/>"); // This is so Excel works out every formula again when the file is opened
            xml.Append("</workbook>");
            return xml.ToString();
        }

        private static string WorkbookRelsXml(int sheetCount)
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            xml.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 1; i <= sheetCount; i++)
                xml.Append("<Relationship Id=\"rId").Append(i)
                   .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet")
                   .Append(i).Append(".xml\"/>");
            xml.Append("<Relationship Id=\"rId").Append(sheetCount + 1)
               .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            xml.Append("</Relationships>");
            return xml.ToString();
        }

        // This is the fonts, borders and the list of cell styles (cellXfs) used by the constants at the top.
        private static readonly string StylesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"0.000\"/></numFmts>" +   // This is 164 = three decimals (leave credits)
            "<fonts count=\"8\">" +
              "<font><sz val=\"9\"/><name val=\"Arial\"/></font>" +                                   // This is 0 normal
              "<font><b/><sz val=\"9\"/><name val=\"Arial\"/></font>" +                               // This is 1 bold
              "<font><b/><sz val=\"12\"/><name val=\"Arial\"/></font>" +                              // This is 2 title
              "<font><b/><sz val=\"8\"/><name val=\"Arial\"/></font>" +                               // This is 3 small bold
              "<font><b/><sz val=\"9\"/><color rgb=\"FFC00000\"/><name val=\"Arial\"/></font>" +      // This is 4 red bold
              "<font><b/><sz val=\"10\"/><name val=\"Arial\"/></font>" +                              // This is 5 TOTAL
              "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +                                // This is 6 leave card
              "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +                            // This is 7 leave card, bold
            "</fonts>" +
            "<fills count=\"4\">" +
              "<fill><patternFill patternType=\"none\"/></fill>" +
              "<fill><patternFill patternType=\"gray125\"/></fill>" +
              "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFFFF2B3\"/><bgColor indexed=\"64\"/></patternFill></fill>" + // This is 2 pale yellow: the open month
              "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE6E8F2\"/><bgColor indexed=\"64\"/></patternFill></fill>" + // This is 3 grey: table headings
            "</fills>" +
            "<borders count=\"4\">" +
              "<border><left/><right/><top/><bottom/><diagonal/></border>" +                                            // This is 0 none
              "<border><left style=\"thin\"><color auto=\"1\"/></left><right style=\"thin\"><color auto=\"1\"/></right>" +
                "<top style=\"thin\"><color auto=\"1\"/></top><bottom style=\"thin\"><color auto=\"1\"/></bottom><diagonal/></border>" + // This is 1 box
              "<border><left/><right/><top/><bottom style=\"thin\"><color auto=\"1\"/></bottom><diagonal/></border>" +  // This is 2 underline
              "<border><left/><right/><top style=\"medium\"><color auto=\"1\"/></top><bottom/><diagonal/></border>" +   // This is 3 thick line above
            "</borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"31\">" +
              Xf(0, 0, "left", false, 0) +      // This is 0  StyleNormal
              Xf(2, 0, "left", false, 0) +      // This is 1  StyleTitle
              Xf(3, 0, "right", false, 0) +     // This is 2  StyleFormNumber
              Xf(1, 0, "left", false, 0) +      // This is 3  StyleBold
              Xf(1, 1, "center", false, 0) +    // This is 4  StyleGridCenter
              Xf(1, 1, "left", false, 1) +      // This is 5  StyleGridLeft
              Xf(4, 1, "center", false, 0) +    // This is 6  StyleGridSunday
              Xf(5, 1, "left", false, 2) +      // This is 7  StyleGridTotal
              Xf(1, 0, "left", true, 0) +       // This is 8  StyleParagraph
              Xf(3, 0, "left", false, 0) +      // This is 9  StyleSmallBold
              Xf(1, 2, "center", false, 0) +    // This is 10 StyleUnderlined
              Xf(3, 3, "left", false, 0) +      // This is 11 StyleRuleAbove
              Xf(6, 0, "left", false, 0) +              // This is 12 LeaveText
              Xf(7, 0, "left", false, 0) +              // This is 13 LeaveBold
              Xf(7, 1, "center", false, 0, 0, 3) +      // This is 14 LeaveHead
              Xf(6, 1, "center", false, 0) +            // This is 15 LeaveCell
              Xf(6, 1, "center", false, 0, 164, 0) +    // This is 16 LeaveNumber
              Xf(7, 1, "center", false, 0, 0, 2) +      // This is 17 LeaveCellNow
              Xf(7, 1, "center", false, 0, 164, 2) +    // This is 18 LeaveNumberNow
              Xf(6, 0, "right", false, 0) +             // This is 19 LeaveRight
              Xf(7, 0, "right", false, 0) +             // This is 20 LeaveBoldRight
              Xf(6, 0, "left", true, 0) +               // This is 21 LeaveWrap
              Xf(6, 1, "left", false, 1) +              // This is 22 LeaveMonthName
              Xf(7, 1, "left", false, 1, 0, 2) +        // This is 23 LeaveMonthNameNow
              Xf(6, 0, "center", false, 0) +            // This is 24 LeaveCenter
              Xf(6, 1, "center", false, 0, 0, 0, true) +    // This is 25 LeaveDates
              Xf(7, 1, "center", false, 0, 0, 2, true) +    // This is 26 LeaveDatesNow
              Xf(3, 0, "center", false, 0) +            // This is 27 StyleSmallBoldCenter
              Xf(7, 2, "center", false, 0, 164, 0) +    // This is 28 StyleBalance
              Xf(7, 0, "center", false, 0) +            // This is 29 LeaveBoldCenter
              Xf(6, 2, "center", false, 0) +            // This is 30 LeaveLineBelow
            "</cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
            "</styleSheet>";

        /// <param name="indent">This is the small gap between the cell border and the text (0 = none).</param>
        /// <param name="numberFormat">This is 0 = as typed, 164 = three decimals.</param>
        /// <param name="fill">This is the background colour: a position in the "fills" list above (0 = none).</param>
        /// <param name="shrink">This is true when text that is too long for the cell should be shown smaller instead of spilling over.</param>
        private static string Xf(int font, int border, string align, bool wrap, int indent, int numberFormat = 0, int fill = 0,
                                 bool shrink = false)
        {
            return "<xf numFmtId=\"" + numberFormat + "\" fontId=\"" + font + "\" fillId=\"" + fill + "\" borderId=\"" + border +
                   "\" xfId=\"0\" applyNumberFormat=\"1\" applyFill=\"1\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\">" +
                   "<alignment horizontal=\"" + align + "\" vertical=\"" + (wrap ? "top" : "center") + "\"" +
                   (wrap ? " wrapText=\"1\"" : "") + (shrink ? " shrinkToFit=\"1\"" : "") +
                   (indent > 0 ? " indent=\"" + indent + "\"" : "") + "/></xf>";
        }
    }
}
