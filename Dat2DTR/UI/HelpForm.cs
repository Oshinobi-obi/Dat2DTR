namespace Dat2DTR
{
    /// <summary>
    /// The "How it works" window: what is new in this DTR, a step-by-step guide,
    /// how to edit times by hand, and the rules used for Late and Undertime.
    ///
    /// The text is built from the current settings, so it always explains the
    /// office hours and grace period that are really being used.
    ///
    /// To change the words, edit BuildGuide() below. Each line there is one
    /// heading, paragraph, numbered step or bullet.
    /// </summary>
    internal sealed class HelpForm : Form
    {
        private readonly RichTextBox _box = new RichTextBox();
        private readonly DtrSettings _settings;
        private readonly Button _closeButton;

        private readonly Font _titleFont = new Font("Segoe UI", 17f, FontStyle.Bold);
        private readonly Font _headingFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        private readonly Font _textFont = new Font("Segoe UI", 10.5f);
        private readonly Font _boldFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _noteFont = new Font("Segoe UI", 9.75f, FontStyle.Italic);
        private readonly Font _gapFont = new Font("Segoe UI", 5f);

        private readonly int _stepIndent;    // how far the text under a numbered step is pushed in
        private readonly int _bulletIndent;  // how far a bullet is pushed in
        private readonly int _bulletWidth;   // width of the bullet mark, so wrapped lines line up

        public HelpForm(DtrSettings settings)
        {
            // Same order as a designer-made form, so it scales correctly on 125% / 150% screens.
            SuspendLayout();
            Text = "How Dat2DTR works";
            Icon = Theme.GetAppIcon();
            Font = new Font("Segoe UI", 9f);
            AutoScaleDimensions = new SizeF(7f, 15f);
            AutoScaleMode = AutoScaleMode.Font;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 660);
            MinimumSize = new Size(520, 380);

            // Measured from the font, so the spacing stays right on any screen scaling.
            _stepIndent = TextRenderer.MeasureText("10.  ", _boldFont, Size.Empty, TextFormatFlags.NoPadding).Width;
            _bulletWidth = TextRenderer.MeasureText("\u2022   ", _textFont, Size.Empty, TextFormatFlags.NoPadding).Width;
            _bulletIndent = _bulletWidth / 2;

            _box.ReadOnly = true;
            _box.BorderStyle = BorderStyle.None;
            _box.BackColor = Color.White;
            _box.ScrollBars = RichTextBoxScrollBars.Vertical;
            _box.DetectUrls = false;
            _box.TabStop = false;
            _box.Dock = DockStyle.Fill;

            var textHolder = new Panel();
            textHolder.Dock = DockStyle.Fill;
            textHolder.Padding = new Padding(22, 16, 6, 6);
            textHolder.BackColor = Color.White;
            textHolder.Controls.Add(_box);

            var close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.OK;
            close.SetBounds(760 - 16 - 100, 9, 100, 32);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Size = new Size(760, 50);
            bottom.BackColor = Theme.Panel;
            bottom.Controls.Add(close);

            Controls.Add(textHolder);
            Controls.Add(bottom);
            AcceptButton = close;
            CancelButton = close;
            ResumeLayout(false);
            PerformLayout();

            // The text is written when the window opens (see OnLoad), because the
            // text box can only be formatted once it really exists on screen.
            _settings = settings;
            _closeButton = close;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            BuildGuide(_settings);

            // Start at the top of the text.
            _box.Select(0, 0);
            _box.ScrollToCaret();
            _closeButton.Select();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont.Dispose();
                _headingFont.Dispose();
                _textFont.Dispose();
                _boldFont.Dispose();
                _noteFont.Dispose();
                _gapFont.Dispose();
            }
            base.Dispose(disposing);
        }

        // ================================================================ the words

        private void BuildGuide(DtrSettings s)
        {
            Title("How to use Dat2DTR");
            Paragraph("Dat2DTR turns the .dat file from the biometrics device into a Daily Time Record " +
                      "(Civil Service Form No. 48). It computes Late and Undertime for you, and it deducts " +
                      "filed leaves, Late and Undertime from the leave credits (the Computation of Leaves).");

            // ------------------------------------------------------------
            Heading("What is new in this DTR");
            Bullet("A new  L A T E  column (Hours and Minutes) was added after the P.M. column.");
            Bullet("Late and Undertime are computed automatically for every day and added up in the TOTAL row.");
            Bullet("There are " + s.Schedules.Length + " work schedules. Each employee follows one of them:");
            foreach (WorkSchedule schedule in s.Schedules)
                SubBullet(schedule.Description);
            Bullet("\"Edit DTR\" lets you type in a missing time, correct a wrong one, or mark a day as HOLIDAY.");
            Bullet("\"Edit Leave\" lets you file a leave (Vacation, Sick, Mandatory / Forced, Special Privilege, Wellness, CTO). " +
                   "The leave is written on the DTR and deducted from the leave credits automatically.");
            Bullet("A new page, the Computation of Leaves, shows the leave card of the employee and how the month was computed. " +
                   "When you print or save, you choose: the DTR only, the Computation of Leaves only, or both.");
            Bullet("CTO is kept as a balance: the CTO given (at most 6 days at a time, each with its own expiry date) minus the CTO used.");
            Bullet("The DTR has two signature lines: \"Signature over Printed Name\" at the bottom right, " +
                   "and another one in the middle below it.");
            Bullet("The official hours of the employee's schedule are printed beside \"Regular days\".");
            Bullet("The word ORIGINAL is not printed. Stamp it by hand after printing, the same as before.");
            Bullet("The DTR can be saved as a PDF or an Excel file, not only printed.");
            Bullet("Several employees can be printed or saved in one go.");
            Bullet("Days with a missing or an extra scan are pointed out, so someone can check them.");
            Bullet("The names are kept in the program. You type them once, not every month.");

            // ------------------------------------------------------------
            Heading("Do this once: type the names and choose the schedules");
            Paragraph("The .dat file only contains ID numbers. The program has to be told which name belongs to which ID, " +
                      "and which work schedule each person follows.");
            Step(1, "Open the .dat file first.", "This way all the ID numbers in the file are listed for you.");
            Step(2, "Click \"Employee Names...\" at the bottom left.",
                 "A table opens with three columns: Biometric ID, Name on the DTR and Work schedule.");
            Step(3, "Type each person's name beside the ID.", "Type it the way it should appear on the DTR. Example:  DELA CRUZ, JUAN M");
            Step(4, "Choose the work schedule of each person.",
                 "Click the box in the Work schedule column and pick Schedule A, B, C or D. New people start on Schedule A.");
            Step(5, "Click \"Save\".", "The program remembers the names and schedules. Next month you can skip this part.");
            Note("Already have the list in Excel? Save it as a CSV file with the columns ID, Name and (if you want) Schedule (the letter A, B, C or D), " +
                 "then use \"Import list...\". Use \"Export list...\" to copy the list to another computer.");
            Note("A new employee shows as \"(no name yet)\" in the list. Add the name the same way.");

            // ------------------------------------------------------------
            Heading("Once a year: the holidays");
            Paragraph("Holidays are typed once and used for every employee: on a holiday the DTR says HOLIDAY instead of ABSENT, " +
                      "and nothing is deducted. Nobody has to file them in Edit Leave.");
            Step(1, "Click \"Holidays...\" below the list of employees.", "");
            Step(2, "Choose the year at the bottom and click \"Add\".",
                 "The Philippine holidays whose dates are fixed by law are filled in: New Year's Day, Holy Week (worked out for the year), " +
                 "Araw ng Kagitingan, Labor Day, Independence Day, Ninoy Aquino Day, National Heroes Day, All Saints' Day, Bonifacio Day, " +
                 "the Immaculate Conception, Christmas Day, Rizal Day and the Last Day of the Year.");
            Step(3, "Add the ones declared by Proclamation.",
                 "The window has two tables: Regular Holidays and Special (Non-Working) Days. Type the date and the name in the empty " +
                 "line at the bottom of the table the day belongs to - for example Eid'l Fitr and Eid'l Adha under Regular Holidays, " +
                 "Chinese New Year, All Souls' Day and Christmas Eve under Special Days. Check the Proclamation of the year: " +
                 "a holiday can also be moved. \"Remove this line\" takes one away.");
            Step(4, "Click \"Save\".", "Every DTR is made again with the holidays.");
            Note("A leave filed in Edit Leave on the same day comes first. Someone who scanned on a holiday keeps the times, " +
                 "and no late or undertime is counted. Holidays on a Saturday or Sunday are not shown on the DTR.");

            // ------------------------------------------------------------
            Heading("Every month: make the DTRs, step by step");
            Step(1, "Get the file from the biometrics device.",
                 "Export the attendance from the device to a USB flash drive. The file name ends in .dat. Plug the USB into this computer.");
            Step(2, "Click \"Open .dat File...\"  (number 1 on the screen).",
                 "Find the file and click Open. You can also drag the file into the window. " +
                 "Beside the button you will see the file name, how many scans and employees it has, and the dates it covers.");
            Step(3, "Choose the month  (number 2 on the screen).",
                 "The program already picks the most likely month. Change it only if it is not the month you want.");
            Step(4, "Look at the list of employees on the left.",
                 "Each row shows the person's hours (for example 7-6 or 8-5) and the total Late and Undertime for the month. " +
                 "To find someone quickly, type part of the name or the ID in the box above the list. " +
                 "The list beside it shows only the employees that need checking ( ! ), were typed in by hand ( * ) or have a leave ( L ).");
            Step(5, "Click a name.",
                 "The DTR appears on the right, exactly as it will be printed. Use the mouse wheel to scroll down the page. " +
                 "On screen only (never printed), days to check are light red, days typed in by hand light yellow, " +
                 "and days with Late / Undertime typed in light orange.");
            Step(6, "Check the work schedule above the DTR.",
                 "If it is not the right one for this person, pick the other one from the list. The DTR is computed again at once.");
            Step(7, "Read the yellow message, if there is one.",
                 "An employee with  !  in the list has days with a missing or an extra scan. " +
                 "The yellow message says which days and why. Click \"Edit DTR...\" to fix them (see the next part).");
            Step(8, "File the leaves, if there are any.",
                 "Click \"Edit Leave...\" and choose the kind of leave on the day it was taken (see \"Edit Leave\" below).");
            Step(9, "To print: click \"Print...\"  (number 3 on the screen).",
                 "Choose the printer and click Print.");
            Step(10, "To save: click \"Save To...\".",
                 "Choose the folder, then choose the type of file at the bottom of the window: " +
                 "PDF document (best for printing later or sending) or Excel workbook. Click Save.");
            Step(11, "Sign and stamp the printed DTR as usual.", "");

            // ------------------------------------------------------------
            Heading("Edit DTR: typing in a missing or wrong time");
            Paragraph("Use this when someone forgot to scan (for example at lunch), when a scan is wrong, " +
                      "or when a day should say HOLIDAY instead of ABSENT. (For a leave, use \"Edit Leave...\" instead, so that it is deducted.)");
            Step(1, "Click the employee, then click \"Edit DTR...\" above the DTR.",
                 "A table opens with one row for each day of the month.");
            Step(2, "Click the box you want to change and type the time.",
                 "Example:  12:05 PM.  You can also type it short, like  1205  or  12:05. " +
                 "If you leave out AM or PM, the program picks it from the column (6:10 in P.M. Departure means 6:10 PM). " +
                 "To remove a time, empty the box.");
            Step(3, "For a holiday, type it in the Remark column.",
                 "Example:  HOLIDAY.  It is printed on that day in place of ABSENT. A remark is only printed on a day that has no times.");
            Step(4, "Look at the Late and Undertime columns.",
                 "They show the Late and Undertime of that day with your change, before you save. Boxes you changed are yellow.");
            Step(5, "Click \"Save\".",
                 "The DTR is computed again with what you typed.");
            Note("Made a mistake? \"Undo this day\" puts one day back to what the biometrics file says. \"Undo all\" does it for the whole month.");
            Note("\"Next >\" and \"< Previous\" save and open the employee below or above in the list, so you can go through everyone " +
                 "without closing the window. (Edit Leave has the same buttons.)");
            Note("Clicking Cancel or the X after changing something asks first, so a change is not thrown away by mistake.");
            Note("What you type is remembered on this computer for that employee and day. The .dat file itself is never changed. " +
                 "Days typed in by hand are listed in the yellow message, and the employee gets a  *  in the list.");

            SubHeading("Typing in the Late or Undertime of a day (for example a 3:00 PM work suspension)");
            Paragraph("When work is suspended at 3:00 PM and everyone goes home at 3:00, the program still counts undertime, " +
                      "because the scans are earlier than the official Time Out. You can type the right Late or Undertime yourself.");
            Step(1, "In Edit DTR, type the right Hours and Minutes in the UNDERTIME (or LATE) columns of that day.",
                 "The columns are the same as on the DTR: LATE Hours, LATE Minutes, UNDERTIME Hours, UNDERTIME Minutes. " +
                 "Type  0  for none. Example: 1 hour 30 minutes = 1 in Hours and 30 in Minutes. The boxes turn yellow.");
            Step(2, "Type the reason in the Reason box.",
                 "Example:  Work suspended at 3:00 PM.  The program does not save without a reason.");
            Step(3, "Click \"Save\".",
                 "The DTR prints what you typed, the totals add it up, and the Computation of Leaves deducts it from the Vacation Leave.");
            Note("The Result column keeps showing what the program computed, and the yellow message lists every day you changed " +
                 "(for example  Day 8 - undertime 13m -> none (Work suspended at 3:00 PM)),  so anyone can see what was changed and why.");
            Note("To go back to the computed value, empty both boxes (Hours and Minutes), or click \"Undo this day\".");

            // ------------------------------------------------------------
            Heading("Edit Leave: filing a leave and computing the leave credits");
            Paragraph("Use this when an employee filed a leave. The program writes the leave on the DTR, deducts it from the " +
                      "leave credits and works out the new balance. The kinds of leave are: Vacation Leave, Sick Leave, " +
                      "Mandatory / Forced Leave, Special Privilege Leave, Wellness Leave and CTO. " +
                      "Holiday, Suspended and Official Business (for example a school visit) are in the same list, but nothing is deducted for them.");
            Step(1, "Click the employee, then click \"Edit Leave...\" above the DTR.",
                 "A table opens with one row for each day of the month. The column \"On the DTR\" shows what the person scanned that day.");
            Step(2, "On the day of the leave, click the box under \"Leave\" and pick the kind of leave.",
                 "The row turns yellow, and the column \"Deducted\" shows how much is taken from the credits.");
            Step(3, "Under \"How long\", pick Whole day or Half day.",
                 "\"Half day - morning\" means the person was on leave in the morning and worked in the afternoon. " +
                 "\"Half day - afternoon\" is the other way around.");
            Step(4, "The first time only: type the balance brought forward.",
                 "On the right, type the Vacation Leave and the Sick Leave balance at the end of last month " +
                 "(copy the Net from the employee's leave card). You do this once for each employee. " +
                 "After that the program carries the balance over every month by itself.");
            Step(5, "Read the Computation on the right.",
                 "It shows line by line what was earned, what was deducted and the new balance. It changes while you choose.");
            Step(6, "Click \"Save\".",
                 "The DTR now shows the leave (for example VACATION LEAVE) in place of ABSENT, and the Computation of Leaves is shown.");
            Note("To take a leave away, pick \"(no leave)\" on that day. \"Remove all leave this month\" clears the whole month.");
            Note("Leaves are remembered on this computer. An employee with a leave in the month gets an  L  in the list, " +
                 "and the days are listed in the yellow message.");
            Note("Someone who was on leave for the whole month did not scan, so the name is not in the list. " +
                 "Tick \"Also show employees with no scans this month\" below the list, click the name and file the leave. " +
                 "From then on the name stays in the list for that month.");

            SubHeading("Holiday, Suspended, WFH and IDLAR");
            Bullet("\"Holiday\" and \"Suspended (work suspension)\" are also in the Leave list. They are NOT deducted and not counted. " +
                   "The DTR shows HOLIDAY or WORK SUSPENDED in place of ABSENT.");
            Bullet("WFH = work from home. Tick it on a day with no leave, a Holiday or a Suspended day. " +
                   "On a Suspended day the heading of that column reads \"Is it WFH?\".");
            Bullet("IDLAR can be ticked only after WFH. Tick it when the IDLAR (the daily accomplishment report) is handed in: " +
                   "the day then counts as a day at work. The DTR shows WORK FROM HOME (or SUSPENDED - WORK FROM HOME).");
            Bullet("WFH without IDLAR: on an ordinary day the DTR still shows ABSENT, and the day is listed to check. " +
                   "When you click Save, the program asks once whether to save it like that. " +
                   "A Holiday is saved without asking: it needs neither WFH nor IDLAR.");

            SubHeading("The same leave for many employees (for example a holiday)");
            Step(1, "Tick the employees in the list on the left (or click \"Tick all\").", "");
            Step(2, "Click \"Edit Leave...\" and choose \"The ... I ticked\".",
                 "\"Only this one\" opens the usual table for the employee on screen.");
            Step(3, "Choose the days (From ... to), the leave (for example Holiday or Suspended), how long, and WFH / IDLAR if needed.",
                 "Days that are not working days in an employee's schedule are skipped for that employee.");
            Step(4, "Click \"Apply\".",
                 "If some of those days already have another leave filed, the program asks whether to replace it or keep it. " +
                 "Choosing \"(no leave - remove what is filed)\" removes what is filed on those days.");

            SubHeading("Work suspended from a time (for example 3:00 PM), for everyone");
            Step(1, "Tick the employees (\"Tick all\" for everyone), click \"Edit Leave...\" and choose \"The ... I ticked\".", "");
            Step(2, "Choose the date, Leave = Suspended, How long = \"From a time\", and set the time (for example 3:00 PM).", "");
            Step(3, "Click \"Apply\".",
                 "The day stays an ordinary working day until 3:00 PM - late, undertime and ABSENT as usual - and no undertime is counted after it. " +
                 "Nothing is deducted for the suspension. The yellow message shows \"Work suspended: Day 8 from 3:00 PM\".");
            Note("For one employee, choose Suspended in Edit Leave and pick \"From 3:00 PM\" under How long.");

            SubHeading("Seeing the Computation of Leaves");
            Bullet("Above the DTR there are two buttons:  DTR  and  Computation of Leaves.  " +
                   "Click \"Computation of Leaves\" to see the leave page of the employee. Click \"DTR\" to go back.");
            Bullet("The top of the page is the leave card of the year, one row for each month. The VACATION part has: " +
                   "Earned, Enjoyed, Net, Leave Date/s, SPL, WL (Wellness Leave), CTO, Late and Undertime. " +
                   "The SICK part has: Earned, Enjoyed, Net and Leave Date/s. The month you are working on is yellow.");
            Bullet("Below it, the computation of the month is written out line by line: Vacation Leave and Sick Leave on the left; " +
                   "Special Privilege Leave, Wellness Leave and CTO on the right.");
            Bullet("At the top right, under \"For the month of\", the page shows the Vacation Leave (VL) and Sick Leave (SL) " +
                   "the employee has left at the end of the month. They stay empty until the balance is known.");
            Bullet("Under the table, \"Used this year\" shows the Special Privilege, Forced and Wellness Leave and the CTO used since January.");
            Bullet("Things you should check are shown in the Edit Leave window (bottom right). They are not printed.");

            SubHeading("Printing or saving the Computation of Leaves");
            Bullet("Click \"Print...\" or \"Save To...\". The first question is what you want: " +
                   "DTR only,  Computation of Leaves only,  or Both (each DTR followed by its Computation of Leaves).");
            Bullet("In an Excel file, the Computation of Leaves is a sheet named \"Leave - (name)\". " +
                   "Net is a real Excel formula (the Net of the month before + Earned - Enjoyed), the same as on the leave card of Personnel.");
            Bullet("To bring a month into the leave card workbook of Personnel, copy the numbers of that month's row. " +
                   "Check first that the columns are in the same order there.");

            SubHeading("CTO: giving and using");
            Step(1, "In Edit Leave, click \"CTO given...\".",
                 "A table opens with every time CTO was given to this employee.");
            Step(2, "Type the date it was given, how many days, and the date it expires.",
                 "Example:  Sep 15, 2026   2   Sep 14, 2027.  Leave \"Expires on\" empty to make it one year. " +
                 "An employee can have at most " + LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) + " days of CTO at one time; " +
                 "the program does not let you give more.");
            Step(3, "Click \"Save\".", "");
            Step(4, "When the employee uses CTO, file it like a leave:",
                 "pick \"CTO\" under Leave on that day (whole day or half a day).");
            Note("CTO used is taken from the CTO that expires first. CTO not used by its expiry date is lost; " +
                 "the computation shows how much expired and when the rest expires.");

            // ------------------------------------------------------------
            Heading("How leave is computed");
            Paragraph("The rules follow the leave rules of the Civil Service Commission (Omnibus Rules on Leave).");
            Bullet("Every month the employee earns " + LeaveRules.FormatShort(LeaveRules.MonthlyCredit) + " days of Vacation Leave and " +
                   LeaveRules.FormatShort(LeaveRules.MonthlyCredit) + " days of Sick Leave.");
            Bullet("Vacation Leave and Mandatory / Forced Leave are deducted from the Vacation Leave credits.");
            Bullet("Sick Leave is deducted from the Sick Leave credits. If there are not enough, the rest is deducted from the Vacation Leave credits.");
            Bullet("Late and Undertime of the whole month are turned into a part of a day and deducted from the Vacation Leave credits. " +
                   "The table of the Civil Service Commission is used:  8 hours = 1 day,  1 hour = .125,  1 minute = .002.  " +
                   "Example: Late of 3 hours 15 minutes = .375 + .031 = " + LeaveRules.Format(LeaveRules.MinutesToDays(195)) + " day.");
            Bullet("What one day of leave costs depends on the hours of the work schedule (8 hours = 1 day):");
            foreach (WorkSchedule schedule in s.Schedules)
            {
                decimal charge = LeaveRules.DayCharge(schedule);
                SubBullet("Schedule " + schedule.Letter + " (" + schedule.OfficialHoursText + "):  a whole day of leave = " +
                          LeaveRules.FormatShort(charge) + ",  half a day = " + LeaveRules.FormatShort(charge / 2));
            }
            Bullet("If the Vacation Leave credits are not enough, the rest is without pay. It is shown in the computation as \"without pay\".");
            Bullet("An employee with days without pay earns less that month. The CSC table of computation for leave without pay is used " +
                   "(for example 1 day without pay = 1.208 earned, 6 days = 1.000, 30 days = 0).");
            Bullet("Special Privilege Leave (3 days a year) and Wellness Leave (5 days a year) are NOT deducted from the leave credits. " +
                   "They are counted, and the computation shows how many days are left for the year. " +
                   "Forced Leave is counted too (5 days a year).");
            Bullet("CTO is taken from the CTO given to the employee (see \"CTO: giving and using\").");
            Bullet("Balance at the end of the month (Net)  =  balance brought forward + earned - enjoyed.  " +
                   "It becomes the balance brought forward of the next month.");

            SubHeading("What a leave changes on the DTR");
            Bullet("A whole day of leave:  the leave is printed across the row in place of ABSENT. No late or undertime is counted for that day.");
            Bullet("Half a day of leave:  the half on leave shows the leave, and no late or undertime is counted for that half. " +
                   "The other half follows the usual rules.");
            Bullet("If the times of the day do not agree with the leave (times on a day of leave, times in the half that is on leave, " +
                   "or no times in the other half), the day is pointed out in the yellow message and in Edit Leave. " +
                   "The program still deducts the leave as filed. It is for you to correct the leave or the times.");

            SubHeading("What the program does not do by itself");
            Bullet("A day marked ABSENT with no leave filed is NOT deducted. The Computation of Leaves lists these days, so you can decide what to do.");
            Bullet("A half-day leave on a day with no times at all: only the half day of leave is deducted. Nothing is deducted for the other half. " +
                   "The day is pointed out in Edit Leave.");
            Bullet("For a new employee who started in the middle of the month, type the credits earned under \"Credits earned this month\" " +
                   "in Edit Leave. The CSC table for leave with pay is: 1 day = .042,  5 days = .208,  10 days = .417,  15 days = .625,  " +
                   "20 days = .833,  25 days = 1.042,  30 days = 1.250.");
            Bullet("Unused Forced Leave at the end of the year, monetization and terminal leave are not computed.");
            Note("To carry the balance over, the program remembers the Late and Undertime of every month you open. " +
                 "If a month is skipped, the balance of the next month is \"not known\" until you open the skipped month, " +
                 "or type the balance again in Edit Leave.");
            Note("A month is only remembered from a file that holds the whole month. If you look at a month through a file " +
                 "that starts in the middle of it, or that ends earlier than a file you opened before, the page says so and " +
                 "the earlier result is kept.");
            Note("The leaves and balances are kept in small files on this computer (leaves.csv, leave-ledger.csv and cto.csv). " +
                 "Do not leave them open in Excel while you use Dat2DTR: the program then cannot read them and will not save.");

            // ------------------------------------------------------------
            Heading("Printing or saving several employees at once");
            Step(1, "Tick the box beside each name you want.", "\"Tick all\" ticks everyone in the list. \"Untick all\" clears the ticks.");
            Step(2, "Click \"Print...\" or \"Save To...\".", "");
            Step(3, "Choose what you want:  DTR only,  Computation of Leaves only,  or Both.",
                 "Save To also offers \"Biometrics file (.dat)\": the scans of the chosen employees in the chosen month, " +
                 "saved as a new .dat file exactly as they were in the opened file. Times typed in with Edit DTR and leaves are not in it.");
            Step(4, "Answer the question \"Whose?\"",
                 "Only this one = the employee you are looking at.   The ones I ticked = everyone with a tick.   Everyone = all employees in the list.");
            Note("In a PDF each employee gets one page for each part you chose. In an Excel file each employee gets one sheet for each part " +
                 "(see the tabs at the bottom of Excel).");
            Note("The same works for the work schedule: tick several people, then pick the schedule above the DTR, " +
                 "and you are asked whether to change only this person, the ticked ones, or everyone.");

            // ------------------------------------------------------------
            Heading("How Late and Undertime are computed");
            Paragraph("The rules are the same for every schedule. Only the times and the working days are different. " +
                      "These are the rules being used right now:");
            foreach (WorkSchedule schedule in s.Schedules)
                DescribeRules(schedule, s);

            SubHeading("Absent, Saturday and Sunday (every schedule)");
            Bullet("A working day with no scans is marked  A B S E N T.  Absences are not added to undertime.");
            Bullet("Friday in a Monday-to-Thursday schedule also says  A B S E N T  when it has no scans, the same as on the DTR you use now. " +
                   "It is not a working day in that schedule, so no late or undertime is computed for it, even if the person scanned.");
            Bullet("Days before the first scan or after the last scan in the file are left blank, because the file does not cover them.");
            Bullet("Saturdays and Sundays are only labelled Sat and Sun. No late or undertime is computed for them.");

            // ------------------------------------------------------------
            Heading("Which scan goes in which column");
            Bullet("First scan of the day  =  A.M. Arrival");
            Bullet("Last scan of the day  =  P.M. Departure");
            Bullet("The two scans around lunch time  =  A.M. Departure and P.M. Arrival");
            Bullet("Two scans within one minute count as one (the finger was scanned twice).");
            Bullet("The In / Out button of the device is ignored, because people often forget to press it.");
            Note("If a day has only one scan, or one lunch scan is missing, the program shows what it has and asks you to check that day. " +
                 "It does not guess the missing time. Use \"Edit DTR...\" to type it in.");

            // ------------------------------------------------------------
            Heading("Changing the office hours, grace period or working days");
            Step(1, "Click \"Office Hours...\" at the top right.", "");
            Step(2, "Choose the schedule to change (A, B, C or D) in the list at the top.", "");
            Step(3, "Change what you need.",
                 "Time In, lunch break, Time Out, grace period and working days belong to the chosen schedule. " +
                 "How Late is counted and the paper size are the same for every schedule.");
            Step(4, "Click \"Save\".", "Every DTR is computed again right away with the new rules. The program remembers them.");

            // ------------------------------------------------------------
            Heading("Common questions");
            Question("The name shows \"(no name yet)\".",
                     "That ID has no name typed in. Click \"Employee Names...\" and type it.");
            Question("Friday says ABSENT for someone who works Monday to Thursday.",
                     "That is correct. It is how the DTR is filled in. Friday is not a working day in that schedule, " +
                     "so nothing is added to Late or Undertime for it.");
            Question("Some employees go out to inspect schools on some Fridays. What do I do?",
                     "Keep them on their Monday-to-Friday schedule (for example C or D). On a Friday they spent visiting schools, " +
                     "file \"Official Business (school visit)\" in Edit Leave. The DTR then shows OFFICIAL BUSINESS instead of ABSENT, " +
                     "and nothing is deducted. On the Fridays they stay in the office, nothing needs to be done. " +
                     "If they scanned in the morning and left for the school in the afternoon, file it as \"Half day - afternoon\". " +
                     "For a whole group, tick them in the list and use Edit Leave > \"The ... I ticked\".");
            Question("Someone who works on Fridays gets no Late or Undertime on Fridays.",
                     "That person is on the wrong schedule. Click the name and pick the Monday-to-Friday schedule above the DTR.");
            Question("A holiday shows as ABSENT.",
                     "Click \"Edit DTR...\" and type HOLIDAY in the Remark column of that day.");
            Question("Someone who was on leave shows as ABSENT.",
                     "Click \"Edit Leave...\" and pick the kind of leave on that day. It is then deducted from the leave credits.");
            Question("The Computation of Leaves says the balance is not known.",
                     "Click \"Edit Leave...\" and type the balance brought forward from last month (copy it from the employee's leave card). " +
                     "You only do this once for each employee.");
            Question("The leave page was not printed, or it is not in the PDF.",
                     "When you click \"Print...\" or \"Save To...\", choose \"Computation of Leaves only\" or \"Both\".");
            Question("It will not let me give more CTO.",
                     "An employee can have at most " + LeaveRules.FormatShort(LeaveKinds.CtoMaxDays) + " days of CTO at one time. " +
                     "Give more only after some of it was used or has expired.");
            Question("Someone forgot to scan at lunch.",
                     "Click \"Edit DTR...\" and type the missing times. The yellow message for that day goes away.");
            Question("An employee is missing from the list.",
                     "The list shows people who scanned at least once in the chosen month, or who have a leave filed in it. " +
                     "Check that the right month is chosen. For someone who did not scan at all, " +
                     "tick \"Also show employees with no scans this month\" below the list.");
            Question("It says the file could not be saved.",
                     "A file with the same name is probably open in Excel or in a PDF reader. Close it and save again.");
            Question("A time on the DTR looks wrong.",
                     "The program shows exactly what the biometrics device recorded. Check the clock of the device, or correct the time with \"Edit DTR...\".");
            Question("What do the numbers in the .dat file mean?",
                     "Each line of the file is one scan. Example:   529000    2017-09-20 12:34:53    1    0    1    0");
            SubBullet("529000  =  the biometric ID of the employee.");
            SubBullet("2017-09-20 12:34:53  =  the date and time of the scan.");
            SubBullet("1  =  the number of the device (machine) that was used.");
            SubBullet("0  =  the In / Out state:  0 means check-in,  1 means check-out.");
            SubBullet("1  =  how the person was checked:  1 means fingerprint,  15 means face.");
            SubBullet("0  =  the work code. It is not used, so it is always 0.");
            Note("Dat2DTR only uses the ID and the date and time. The other numbers are ignored. " +
                 "This is the usual layout of ZKTeco-type biometrics devices; the manual of your device has the exact meaning.");
            Question("Are there keyboard shortcuts?",
                     "Yes.  Ctrl+O = open a file.   Ctrl+P = print.   Ctrl+S = save.");
        }

        /// <summary>Writes the Late and Undertime rules of one schedule, with its own times in the examples.</summary>
        private void DescribeRules(WorkSchedule w, DtrSettings s)
        {
            string timeIn = DtrSettings.FormatTime(w.TimeIn);
            string timeOut = DtrSettings.FormatTime(w.TimeOut);
            string lunchStart = DtrSettings.FormatTime(w.LunchStart);
            string lunchEnd = DtrSettings.FormatTime(w.LunchEnd);
            string graceEnd = DtrSettings.FormatTime(w.TimeIn + TimeSpan.FromMinutes(w.GraceMinutes));

            int afterGrace = 11; // the example arrives 11 minutes after the grace period ends
            string lateTime = DtrSettings.FormatTime(w.TimeIn + TimeSpan.FromMinutes(w.GraceMinutes + afterGrace));
            int lateMinutes = s.CountLateFromOfficialTime ? w.GraceMinutes + afterGrace : afterGrace;

            string lunchLateTime = DtrSettings.FormatTime(w.LunchEnd + TimeSpan.FromMinutes(10));
            string earlyOutTime = DtrSettings.FormatTime(w.TimeOut - TimeSpan.FromMinutes(30));
            string morningHalf = DtrSheet.FormatDuration((int)(w.LunchStart - w.TimeIn).TotalMinutes);
            string afternoonHalf = DtrSheet.FormatDuration((int)(w.TimeOut - w.LunchEnd).TotalMinutes);

            SubHeading(w.Description);
            Bullet("Lunch break:  " + lunchStart + " to " + lunchEnd + ".   Grace period:  " + w.GraceMinutes + " minutes.");
            Bullet("Late:  arriving up to " + graceEnd + " is NOT late. After " + graceEnd + " it is late" +
                   (s.CountLateFromOfficialTime
                       ? ", and the minutes are counted from " + timeIn + "."
                       : ", and only the minutes after " + graceEnd + " are counted.") +
                   "  Example: arriving at " + lateTime + " = " + lateMinutes + " minutes late.");
            Bullet("Late:  coming back from lunch after " + lunchEnd + ", counted from " + lunchEnd + ".  " +
                   "Example: back at " + lunchLateTime + " = 10 minutes late.");
            Bullet("Undertime:  leaving before " + lunchStart + " in the morning, or before " + timeOut + " in the afternoon.  " +
                   "Example: leaving at " + earlyOutTime + " = 30 minutes undertime.");
            Bullet("Undertime:  a half day with no scans at all (when the other half has scans) counts as " +
                   morningHalf + " for the morning, " + afternoonHalf + " for the afternoon.");
            Gap();
        }

        // ================================================================ building blocks

        private void Title(string text)
        {
            Write(text + "\n", _titleFont, Theme.Header, 0, 0);
            Gap();
        }

        private void Heading(string text)
        {
            Gap();
            Gap();
            Write(text + "\n", _headingFont, Theme.Accent, 0, 0);
            Gap();
        }

        private void SubHeading(string text)
        {
            Gap();
            Write(text + "\n", _boldFont, Theme.TextStrong, 0, 0);
        }

        private void Paragraph(string text)
        {
            Write(text + "\n", _textFont, Theme.TextStrong, 0, 0);
            Gap();
        }

        /// <summary>A numbered step: a bold line saying what to do, and a line underneath with details.</summary>
        private void Step(int number, string whatToDo, string details)
        {
            Write(number + ".  ", _boldFont, Theme.Warm, 0, _stepIndent);
            Write(whatToDo + "\n", _boldFont, Theme.TextStrong, 0, _stepIndent);
            if (details.Length > 0)
                Write(details + "\n", _textFont, Theme.TextStrong, _stepIndent, 0);
            Gap();
        }

        private void Bullet(string text)
        {
            Write("\u2022   " + text + "\n", _textFont, Theme.TextStrong, _bulletIndent, _bulletWidth);
        }

        /// <summary>A second-level bullet, pushed in further.</summary>
        private void SubBullet(string text)
        {
            Write("-   " + text + "\n", _textFont, Theme.TextStrong, _bulletIndent + _bulletWidth, _bulletWidth);
        }

        /// <summary>A tip in smaller, slanted, grey letters.</summary>
        private void Note(string text)
        {
            Write(text + "\n", _noteFont, Theme.TextMuted, _stepIndent, 0);
            Gap();
        }

        private void Question(string question, string answer)
        {
            Write(question + "\n", _boldFont, Theme.TextStrong, 0, 0);
            Write(answer + "\n", _textFont, Theme.TextStrong, _stepIndent, 0);
            Gap();
        }

        /// <summary>A little empty space between two blocks.</summary>
        private void Gap()
        {
            Write("\n", _gapFont, Theme.TextStrong, 0, 0);
        }

        /// <summary>Adds text at the end of the box, then gives that new text its look.</summary>
        /// <param name="indent">How far the paragraph is pushed in from the left.</param>
        /// <param name="hangingIndent">Extra push for the 2nd line onwards (lines up wrapped text).</param>
        private void Write(string text, Font font, Color color, int indent, int hangingIndent)
        {
            int start = _box.TextLength;
            _box.AppendText(text);

            // Select what was just added and format it.
            _box.Select(start, _box.TextLength - start);
            _box.SelectionFont = font;
            _box.SelectionColor = color;
            _box.SelectionIndent = indent;
            _box.SelectionHangingIndent = hangingIndent;

            // Put the cursor back at the end, ready for the next piece of text.
            _box.Select(_box.TextLength, 0);
        }
    }
}
