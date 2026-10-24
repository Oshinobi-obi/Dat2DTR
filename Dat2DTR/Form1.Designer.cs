namespace Dat2DTR
{
    partial class Form1
    {
        /// <summary>
        /// This is the required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// This is where any resources being used are cleaned up.
        /// </summary>
        /// <param name="disposing">This is true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// This is the required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pnlHeader = new Panel();
            picSeal = new LogoBox();
            picBanner = new LogoBox();
            lblTitle = new Label();
            lblSubtitle = new Label();
            btnSettings = new Button();
            btnHelp = new Button();
            pnlHeaderLine = new Panel();
            pnlSteps = new Panel();
            tlpSteps = new TableLayoutPanel();
            pnlStep1 = new Panel();
            badge1 = new StepBadge();
            lblStep1 = new Label();
            btnOpen = new Button();
            lblFile = new Label();
            pnlStep2 = new Panel();
            badge2 = new StepBadge();
            lblStep2 = new Label();
            cboMonth = new ComboBox();
            pnlStep3 = new Panel();
            badge3 = new StepBadge();
            lblStep3 = new Label();
            btnPrint = new Button();
            btnSave = new Button();
            pnlStepsLine = new Panel();
            pnlLeft = new Panel();
            lvEmployees = new ListView();
            colName = new ColumnHeader();
            colId = new ColumnHeader();
            colSchedule = new ColumnHeader();
            colLate = new ColumnHeader();
            colUndertime = new ColumnHeader();
            colCheck = new ColumnHeader();
            pnlLeftBottom = new Panel();
            btnTickAll = new Button();
            btnUntickAll = new Button();
            btnNames = new Button();
            btnHolidays = new Button();
            chkNoScans = new CheckBox();
            lblHint = new Label();
            pnlSearch = new Panel();
            txtSearch = new TextBox();
            pnlFilterGap = new Panel();
            cboFilter = new ComboBox();
            lblEmployees = new Label();
            pnlLeftLine = new Panel();
            pnlCenter = new Panel();
            preview = new DtrPreviewControl();
            lblNotice = new Label();
            pnlView = new Panel();
            btnViewDtr = new Button();
            btnViewLeave = new Button();
            pnlToolsLine = new Panel();
            pnlTools = new Panel();
            lblSchedule = new Label();
            cboSchedule = new ComboBox();
            btnLeave = new Button();
            btnEdit = new Button();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            lblRules = new ToolStripStatusLabel();
            toolTip = new ToolTip(components);
            pnlHeader.SuspendLayout();
            pnlSteps.SuspendLayout();
            tlpSteps.SuspendLayout();
            pnlStep1.SuspendLayout();
            pnlStep2.SuspendLayout();
            pnlStep3.SuspendLayout();
            pnlLeft.SuspendLayout();
            pnlLeftBottom.SuspendLayout();
            pnlSearch.SuspendLayout();
            pnlCenter.SuspendLayout();
            pnlView.SuspendLayout();
            pnlTools.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // This is pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(0, 0, 128);
            pnlHeader.Controls.Add(picSeal);
            pnlHeader.Controls.Add(picBanner);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(btnSettings);
            pnlHeader.Controls.Add(btnHelp);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(3, 4, 3, 4);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1417, 85);
            pnlHeader.TabIndex = 0;
            // 
            // This is picSeal
            // 
            picSeal.Location = new Point(16, 8);
            picSeal.Name = "picSeal";
            picSeal.Size = new Size(70, 70);
            picSeal.TabIndex = 4;
            picSeal.TabStop = false;
            // 
            // This is picBanner
            // 
            picBanner.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picBanner.Location = new Point(887, 9);
            picBanner.Name = "picBanner";
            picBanner.Size = new Size(196, 68);
            picBanner.TabIndex = 5;
            picBanner.TabStop = false;
            // 
            // This is lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(98, 7);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(132, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Dat2DTR";
            // 
            // This is lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.ForeColor = Color.FromArgb(255, 240, 170);
            lblSubtitle.Location = new Point(101, 49);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(481, 20);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Turns the biometrics .dat file into a Daily Time Record (CS Form No. 48)";
            // 
            // This is btnSettings
            // 
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(1099, 20);
            btnSettings.Margin = new Padding(3, 4, 3, 4);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(153, 45);
            btnSettings.TabIndex = 2;
            btnSettings.Text = "Office Hours...";
            toolTip.SetToolTip(btnSettings, "Change the office hours, grace period and working days");
            btnSettings.UseVisualStyleBackColor = false;
            btnSettings.Click += btnSettings_Click;
            // 
            // This is btnHelp
            // 
            btnHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHelp.Cursor = Cursors.Help;
            btnHelp.Location = new Point(1262, 20);
            btnHelp.Margin = new Padding(3, 4, 3, 4);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(139, 45);
            btnHelp.TabIndex = 3;
            btnHelp.Text = "How it works";
            toolTip.SetToolTip(btnHelp, "Read how to use the program and how Late and Undertime are computed");
            btnHelp.UseVisualStyleBackColor = false;
            btnHelp.Click += btnHelp_Click;
            // 
            // This is pnlHeaderLine
            // 
            pnlHeaderLine.BackColor = Color.FromArgb(255, 223, 0);
            pnlHeaderLine.Dock = DockStyle.Top;
            pnlHeaderLine.Location = new Point(0, 85);
            pnlHeaderLine.Name = "pnlHeaderLine";
            pnlHeaderLine.Size = new Size(1417, 4);
            pnlHeaderLine.TabIndex = 7;
            // 
            // This is pnlSteps
            // 
            pnlSteps.BackColor = Color.White;
            pnlSteps.Controls.Add(tlpSteps);
            pnlSteps.Dock = DockStyle.Top;
            pnlSteps.Location = new Point(0, 89);
            pnlSteps.Margin = new Padding(3, 4, 3, 4);
            pnlSteps.Name = "pnlSteps";
            pnlSteps.Padding = new Padding(16, 16, 16, 13);
            pnlSteps.Size = new Size(1417, 149);
            pnlSteps.TabIndex = 1;
            // 
            // This is tlpSteps
            // 
            tlpSteps.ColumnCount = 3;
            tlpSteps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            tlpSteps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            tlpSteps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tlpSteps.Controls.Add(pnlStep1, 0, 0);
            tlpSteps.Controls.Add(pnlStep2, 1, 0);
            tlpSteps.Controls.Add(pnlStep3, 2, 0);
            tlpSteps.Dock = DockStyle.Fill;
            tlpSteps.Location = new Point(16, 16);
            tlpSteps.Margin = new Padding(3, 4, 3, 4);
            tlpSteps.Name = "tlpSteps";
            tlpSteps.RowCount = 1;
            tlpSteps.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpSteps.Size = new Size(1385, 120);
            tlpSteps.TabIndex = 0;
            // 
            // This is pnlStep1
            // 
            pnlStep1.Controls.Add(badge1);
            pnlStep1.Controls.Add(lblStep1);
            pnlStep1.Controls.Add(btnOpen);
            pnlStep1.Controls.Add(lblFile);
            pnlStep1.Dock = DockStyle.Fill;
            pnlStep1.Location = new Point(0, 0);
            pnlStep1.Margin = new Padding(0);
            pnlStep1.Name = "pnlStep1";
            pnlStep1.Size = new Size(609, 120);
            pnlStep1.TabIndex = 0;
            // 
            // This is badge1
            // 
            badge1.Cursor = Cursors.Help;
            badge1.Location = new Point(0, 0);
            badge1.Margin = new Padding(3, 4, 3, 4);
            badge1.Name = "badge1";
            badge1.Size = new Size(34, 40);
            badge1.TabIndex = 0;
            badge1.TabStop = false;
            badge1.Text = "1";
            // 
            // This is lblStep1
            // 
            lblStep1.AutoSize = true;
            lblStep1.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStep1.ForeColor = Color.FromArgb(0, 0, 128);
            lblStep1.Location = new Point(43, 5);
            lblStep1.Name = "lblStep1";
            lblStep1.Size = new Size(223, 25);
            lblStep1.TabIndex = 1;
            lblStep1.Text = "Open the biometrics file";
            // 
            // This is btnOpen
            // 
            btnOpen.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOpen.Location = new Point(43, 53);
            btnOpen.Margin = new Padding(3, 4, 3, 4);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(192, 56);
            btnOpen.TabIndex = 2;
            btnOpen.Text = "Open .dat File...";
            toolTip.SetToolTip(btnOpen, "Choose the .dat file exported from the biometrics device (Ctrl+O)");
            btnOpen.UseVisualStyleBackColor = false;
            btnOpen.Click += btnOpen_Click;
            // 
            // This is lblFile
            // 
            lblFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblFile.AutoEllipsis = true;
            lblFile.ForeColor = Color.FromArgb(96, 100, 124);
            lblFile.Location = new Point(247, 53);
            lblFile.Name = "lblFile";
            lblFile.Size = new Size(349, 56);
            lblFile.TabIndex = 3;
            lblFile.Text = "No file opened yet.\r\nYou can also drag the file into this window.";
            lblFile.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // This is pnlStep2
            // 
            pnlStep2.Controls.Add(badge2);
            pnlStep2.Controls.Add(lblStep2);
            pnlStep2.Controls.Add(cboMonth);
            pnlStep2.Dock = DockStyle.Fill;
            pnlStep2.Location = new Point(609, 0);
            pnlStep2.Margin = new Padding(0);
            pnlStep2.Name = "pnlStep2";
            pnlStep2.Size = new Size(318, 120);
            pnlStep2.TabIndex = 1;
            // 
            // This is badge2
            // 
            badge2.Location = new Point(0, 0);
            badge2.Margin = new Padding(3, 4, 3, 4);
            badge2.Name = "badge2";
            badge2.Size = new Size(34, 40);
            badge2.TabIndex = 0;
            badge2.TabStop = false;
            badge2.Text = "2";
            // 
            // This is lblStep2
            // 
            lblStep2.AutoSize = true;
            lblStep2.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStep2.ForeColor = Color.FromArgb(0, 0, 128);
            lblStep2.Location = new Point(43, 5);
            lblStep2.Name = "lblStep2";
            lblStep2.Size = new Size(174, 25);
            lblStep2.TabIndex = 1;
            lblStep2.Text = "Choose the month";
            // 
            // This is cboMonth
            // 
            cboMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMonth.Enabled = false;
            cboMonth.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cboMonth.FormattingEnabled = true;
            cboMonth.Location = new Point(43, 61);
            cboMonth.Margin = new Padding(3, 4, 3, 4);
            cboMonth.Name = "cboMonth";
            cboMonth.Size = new Size(235, 33);
            cboMonth.TabIndex = 2;
            cboMonth.SelectedIndexChanged += cboMonth_SelectedIndexChanged;
            // 
            // This is pnlStep3
            // 
            pnlStep3.Controls.Add(badge3);
            pnlStep3.Controls.Add(lblStep3);
            pnlStep3.Controls.Add(btnPrint);
            pnlStep3.Controls.Add(btnSave);
            pnlStep3.Dock = DockStyle.Fill;
            pnlStep3.Location = new Point(927, 0);
            pnlStep3.Margin = new Padding(0);
            pnlStep3.Name = "pnlStep3";
            pnlStep3.Size = new Size(458, 120);
            pnlStep3.TabIndex = 2;
            // 
            // This is badge3
            // 
            badge3.Location = new Point(0, 0);
            badge3.Margin = new Padding(3, 4, 3, 4);
            badge3.Name = "badge3";
            badge3.Size = new Size(34, 40);
            badge3.TabIndex = 0;
            badge3.TabStop = false;
            badge3.Text = "3";
            // 
            // This is lblStep3
            // 
            lblStep3.AutoSize = true;
            lblStep3.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStep3.ForeColor = Color.FromArgb(0, 0, 128);
            lblStep3.Location = new Point(43, 5);
            lblStep3.Name = "lblStep3";
            lblStep3.Size = new Size(198, 25);
            lblStep3.TabIndex = 1;
            lblStep3.Text = "Print or save the DTR";
            // 
            // This is btnPrint
            // 
            btnPrint.Enabled = false;
            btnPrint.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnPrint.Location = new Point(43, 53);
            btnPrint.Margin = new Padding(3, 4, 3, 4);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(160, 56);
            btnPrint.TabIndex = 2;
            btnPrint.Text = "Print...";
            toolTip.SetToolTip(btnPrint, "Print the DTR (Ctrl+P)");
            btnPrint.UseVisualStyleBackColor = false;
            btnPrint.Click += btnPrint_Click;
            // 
            // This is btnSave
            // 
            btnSave.Enabled = false;
            btnSave.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSave.Location = new Point(215, 53);
            btnSave.Margin = new Padding(3, 4, 3, 4);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(160, 56);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save To...";
            toolTip.SetToolTip(btnSave, "Save the DTR to your computer as a PDF or an Excel file (Ctrl+S)");
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += btnSave_Click;
            // 
            // This is pnlStepsLine
            // 
            pnlStepsLine.BackColor = Color.FromArgb(208, 210, 226);
            pnlStepsLine.Dock = DockStyle.Top;
            pnlStepsLine.Location = new Point(0, 238);
            pnlStepsLine.Margin = new Padding(3, 4, 3, 4);
            pnlStepsLine.Name = "pnlStepsLine";
            pnlStepsLine.Size = new Size(1417, 1);
            pnlStepsLine.TabIndex = 2;
            // 
            // This is pnlLeft
            // 
            pnlLeft.BackColor = Color.FromArgb(246, 246, 251);
            pnlLeft.Controls.Add(lvEmployees);
            pnlLeft.Controls.Add(pnlLeftBottom);
            pnlLeft.Controls.Add(pnlSearch);
            pnlLeft.Controls.Add(lblEmployees);
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Location = new Point(0, 239);
            pnlLeft.Margin = new Padding(3, 4, 3, 4);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Padding = new Padding(14, 13, 14, 11);
            pnlLeft.Size = new Size(566, 642);
            pnlLeft.TabIndex = 3;
            // 
            // This is lvEmployees
            // 
            lvEmployees.CheckBoxes = true;
            lvEmployees.Columns.AddRange(new ColumnHeader[] { colName, colId, colSchedule, colLate, colUndertime, colCheck });
            lvEmployees.Dock = DockStyle.Fill;
            lvEmployees.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lvEmployees.FullRowSelect = true;
            lvEmployees.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvEmployees.Location = new Point(14, 91);
            lvEmployees.Margin = new Padding(3, 4, 3, 4);
            lvEmployees.MultiSelect = false;
            lvEmployees.Name = "lvEmployees";
            lvEmployees.ShowItemToolTips = true;
            lvEmployees.Size = new Size(538, 395);
            lvEmployees.TabIndex = 2;
            lvEmployees.UseCompatibleStateImageBehavior = false;
            lvEmployees.View = View.Details;
            lvEmployees.ItemChecked += lvEmployees_ItemChecked;
            lvEmployees.SelectedIndexChanged += lvEmployees_SelectedIndexChanged;
            lvEmployees.Resize += lvEmployees_Resize;
            // 
            // This is colName
            // 
            colName.Text = "Name";
            colName.Width = 168;
            // 
            // This is colId
            // 
            colId.Text = "ID";
            colId.Width = 82;
            // 
            // This is colSchedule
            // 
            colSchedule.Text = "Hours";
            colSchedule.Width = 56;
            // 
            // This is colLate
            // 
            colLate.Text = "Late";
            // 
            // This is colUndertime
            // 
            colUndertime.Text = "Undertime";
            colUndertime.Width = 84;
            // 
            // This is colCheck
            // 
            colCheck.Text = "!";
            colCheck.Width = 24;
            // 
            // This is pnlLeftBottom
            // 
            pnlLeftBottom.Controls.Add(btnTickAll);
            pnlLeftBottom.Controls.Add(btnUntickAll);
            pnlLeftBottom.Controls.Add(btnNames);
            pnlLeftBottom.Controls.Add(btnHolidays);
            pnlLeftBottom.Controls.Add(chkNoScans);
            pnlLeftBottom.Controls.Add(lblHint);
            pnlLeftBottom.Dock = DockStyle.Bottom;
            pnlLeftBottom.Location = new Point(14, 486);
            pnlLeftBottom.Margin = new Padding(3, 4, 3, 4);
            pnlLeftBottom.Name = "pnlLeftBottom";
            pnlLeftBottom.Size = new Size(538, 145);
            pnlLeftBottom.TabIndex = 3;
            // 
            // This is btnTickAll
            // 
            btnTickAll.Enabled = false;
            btnTickAll.Location = new Point(0, 11);
            btnTickAll.Margin = new Padding(3, 4, 3, 4);
            btnTickAll.Name = "btnTickAll";
            btnTickAll.Size = new Size(96, 40);
            btnTickAll.TabIndex = 0;
            btnTickAll.Text = "Tick all";
            btnTickAll.UseVisualStyleBackColor = true;
            btnTickAll.Click += btnTickAll_Click;
            // 
            // This is btnUntickAll
            // 
            btnUntickAll.Enabled = false;
            btnUntickAll.Location = new Point(103, 11);
            btnUntickAll.Margin = new Padding(3, 4, 3, 4);
            btnUntickAll.Name = "btnUntickAll";
            btnUntickAll.Size = new Size(96, 40);
            btnUntickAll.TabIndex = 1;
            btnUntickAll.Text = "Untick all";
            btnUntickAll.UseVisualStyleBackColor = true;
            btnUntickAll.Click += btnUntickAll_Click;
            // 
            // This is btnNames
            // 
            btnNames.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNames.Location = new Point(367, 11);
            btnNames.Margin = new Padding(3, 4, 3, 4);
            btnNames.Name = "btnNames";
            btnNames.Size = new Size(171, 40);
            btnNames.TabIndex = 2;
            btnNames.Text = "Employee Names...";
            toolTip.SetToolTip(btnNames, "Type the name that belongs to each biometric ID");
            btnNames.UseVisualStyleBackColor = true;
            btnNames.Click += btnNames_Click;
            // 
            // This is btnHolidays
            // 
            btnHolidays.Location = new Point(206, 11);
            btnHolidays.Margin = new Padding(3, 4, 3, 4);
            btnHolidays.Name = "btnHolidays";
            btnHolidays.Size = new Size(154, 40);
            btnHolidays.TabIndex = 5;
            btnHolidays.Text = "Holidays...";
            toolTip.SetToolTip(btnHolidays, "The holidays of the year, used for every employee (HOLIDAY instead of ABSENT)");
            btnHolidays.UseVisualStyleBackColor = true;
            btnHolidays.Click += btnHolidays_Click;
            // 
            // This is chkNoScans
            // 
            chkNoScans.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            chkNoScans.ForeColor = Color.FromArgb(0, 0, 128);
            chkNoScans.Location = new Point(2, 58);
            chkNoScans.Name = "chkNoScans";
            chkNoScans.Size = new Size(534, 28);
            chkNoScans.TabIndex = 3;
            chkNoScans.Text = "Also show employees with no scans this month";
            toolTip.SetToolTip(chkNoScans, "Tick this to find someone who did not scan at all this month,\r\nfor example an employee who was on leave for the whole month.");
            chkNoScans.UseVisualStyleBackColor = true;
            chkNoScans.CheckedChanged += chkNoScans_CheckedChanged;
            // 
            // This is lblHint
            // 
            lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblHint.ForeColor = Color.FromArgb(96, 100, 124);
            lblHint.Location = new Point(0, 90);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(538, 51);
            lblHint.TabIndex = 4;
            lblHint.Text = "Click a name to see the DTR.\r\nTick the boxes to print or save several at once.";
            // 
            // This is pnlSearch
            // 
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(pnlFilterGap);
            pnlSearch.Controls.Add(cboFilter);
            pnlSearch.Dock = DockStyle.Top;
            pnlSearch.Location = new Point(14, 48);
            pnlSearch.Margin = new Padding(3, 4, 3, 4);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new Size(538, 43);
            pnlSearch.TabIndex = 1;
            // 
            // This is txtSearch
            // 
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtSearch.Location = new Point(0, 0);
            txtSearch.Margin = new Padding(3, 4, 3, 4);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(312, 29);
            txtSearch.TabIndex = 0;
            toolTip.SetToolTip(txtSearch, "Type part of a name or an ID number to find an employee");
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // This is pnlFilterGap
            // 
            pnlFilterGap.Dock = DockStyle.Right;
            pnlFilterGap.Location = new Point(320, 0);
            pnlFilterGap.Name = "pnlFilterGap";
            pnlFilterGap.Size = new Size(8, 43);
            pnlFilterGap.TabIndex = 2;
            // 
            // This is cboFilter
            // 
            cboFilter.Dock = DockStyle.Right;
            cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFilter.DropDownWidth = 260;
            cboFilter.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cboFilter.FormattingEnabled = true;
            cboFilter.Items.AddRange(new object[] { "Show everyone", "Needs checking  (!)", "Typed in by hand  (*)", "With a leave  (L)" });
            cboFilter.Location = new Point(328, 0);
            cboFilter.Name = "cboFilter";
            cboFilter.Size = new Size(210, 29);
            cboFilter.TabIndex = 1;
            toolTip.SetToolTip(cboFilter, "Show only the employees that need checking, were typed in by hand, or have a leave");
            cboFilter.SelectedIndexChanged += cboFilter_SelectedIndexChanged;
            // 
            // This is lblEmployees
            // 
            lblEmployees.Dock = DockStyle.Top;
            lblEmployees.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblEmployees.ForeColor = Color.FromArgb(0, 0, 128);
            lblEmployees.Location = new Point(14, 13);
            lblEmployees.Name = "lblEmployees";
            lblEmployees.Size = new Size(538, 35);
            lblEmployees.TabIndex = 0;
            lblEmployees.Text = "Employees";
            // 
            // This is pnlLeftLine
            // 
            pnlLeftLine.BackColor = Color.FromArgb(208, 210, 226);
            pnlLeftLine.Dock = DockStyle.Left;
            pnlLeftLine.Location = new Point(566, 239);
            pnlLeftLine.Margin = new Padding(3, 4, 3, 4);
            pnlLeftLine.Name = "pnlLeftLine";
            pnlLeftLine.Size = new Size(1, 642);
            pnlLeftLine.TabIndex = 4;
            // 
            // This is pnlCenter
            // 
            pnlCenter.Controls.Add(preview);
            pnlCenter.Controls.Add(lblNotice);
            pnlCenter.Controls.Add(pnlView);
            pnlCenter.Controls.Add(pnlToolsLine);
            pnlCenter.Controls.Add(pnlTools);
            pnlCenter.Dock = DockStyle.Fill;
            pnlCenter.Location = new Point(567, 239);
            pnlCenter.Margin = new Padding(3, 4, 3, 4);
            pnlCenter.Name = "pnlCenter";
            pnlCenter.Size = new Size(850, 642);
            pnlCenter.TabIndex = 5;
            // 
            // This is preview
            // 
            preview.AutoScroll = true;
            preview.BackColor = Color.FromArgb(226, 227, 236);
            preview.Dock = DockStyle.Fill;
            preview.Location = new Point(0, 162);
            preview.Margin = new Padding(3, 4, 3, 4);
            preview.Name = "preview";
            preview.Size = new Size(850, 480);
            preview.TabIndex = 1;
            // 
            // This is lblNotice
            // 
            lblNotice.AutoEllipsis = true;
            lblNotice.BackColor = Color.FromArgb(255, 241, 173);
            lblNotice.Dock = DockStyle.Top;
            lblNotice.ForeColor = Color.FromArgb(0, 0, 110);
            lblNotice.Location = new Point(0, 109);
            lblNotice.Name = "lblNotice";
            lblNotice.Padding = new Padding(14, 0, 14, 0);
            lblNotice.Size = new Size(850, 53);
            lblNotice.TabIndex = 0;
            lblNotice.Text = "Please check";
            lblNotice.TextAlign = ContentAlignment.MiddleLeft;
            lblNotice.Visible = false;
            // 
            // This is pnlView
            // 
            pnlView.BackColor = Color.FromArgb(246, 246, 251);
            pnlView.Controls.Add(btnViewDtr);
            pnlView.Controls.Add(btnViewLeave);
            pnlView.Dock = DockStyle.Top;
            pnlView.Location = new Point(0, 61);
            pnlView.Name = "pnlView";
            pnlView.Size = new Size(850, 48);
            pnlView.TabIndex = 4;
            // 
            // This is btnViewDtr
            // 
            btnViewDtr.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnViewDtr.Location = new Point(14, 8);
            btnViewDtr.Name = "btnViewDtr";
            btnViewDtr.Size = new Size(84, 34);
            btnViewDtr.TabIndex = 0;
            btnViewDtr.Text = "DTR";
            toolTip.SetToolTip(btnViewDtr, "Show the Daily Time Record of this employee");
            btnViewDtr.UseVisualStyleBackColor = false;
            btnViewDtr.Click += btnViewDtr_Click;
            // 
            // This is btnViewLeave
            // 
            btnViewLeave.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnViewLeave.Location = new Point(102, 8);
            btnViewLeave.Name = "btnViewLeave";
            btnViewLeave.Size = new Size(206, 34);
            btnViewLeave.TabIndex = 1;
            btnViewLeave.Text = "Computation of Leaves";
            toolTip.SetToolTip(btnViewLeave, "Show how the leave credits of this employee were computed for this month");
            btnViewLeave.UseVisualStyleBackColor = false;
            btnViewLeave.Click += btnViewLeave_Click;
            // 
            // This is pnlToolsLine
            // 
            pnlToolsLine.BackColor = Color.FromArgb(208, 210, 226);
            pnlToolsLine.Dock = DockStyle.Top;
            pnlToolsLine.Location = new Point(0, 60);
            pnlToolsLine.Name = "pnlToolsLine";
            pnlToolsLine.Size = new Size(850, 1);
            pnlToolsLine.TabIndex = 3;
            // 
            // This is pnlTools
            // 
            pnlTools.BackColor = Color.White;
            pnlTools.Controls.Add(lblSchedule);
            pnlTools.Controls.Add(cboSchedule);
            pnlTools.Controls.Add(btnLeave);
            pnlTools.Controls.Add(btnEdit);
            pnlTools.Dock = DockStyle.Top;
            pnlTools.Location = new Point(0, 0);
            pnlTools.Name = "pnlTools";
            pnlTools.Size = new Size(850, 60);
            pnlTools.TabIndex = 2;
            // 
            // This is lblSchedule
            // 
            lblSchedule.AutoSize = true;
            lblSchedule.ForeColor = Color.FromArgb(0, 0, 128);
            lblSchedule.Location = new Point(14, 20);
            lblSchedule.Name = "lblSchedule";
            lblSchedule.Size = new Size(108, 20);
            lblSchedule.TabIndex = 0;
            lblSchedule.Text = "Work schedule:";
            // 
            // This is cboSchedule
            // 
            cboSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cboSchedule.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSchedule.DropDownWidth = 360;
            cboSchedule.Enabled = false;
            cboSchedule.FormattingEnabled = true;
            cboSchedule.Location = new Point(130, 16);
            cboSchedule.Name = "cboSchedule";
            cboSchedule.Size = new Size(380, 28);
            cboSchedule.TabIndex = 1;
            toolTip.SetToolTip(cboSchedule, "The office hours this employee follows. Change the hours themselves in Office Hours.");
            cboSchedule.SelectionChangeCommitted += cboSchedule_SelectionChangeCommitted;
            // 
            // This is btnLeave
            // 
            btnLeave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLeave.Enabled = false;
            btnLeave.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnLeave.Location = new Point(524, 10);
            btnLeave.Name = "btnLeave";
            btnLeave.Size = new Size(152, 40);
            btnLeave.TabIndex = 2;
            btnLeave.Text = "Edit Leave...";
            toolTip.SetToolTip(btnLeave, "File a leave for this employee (Vacation, Sick, Forced, Special Privilege, Wellness) and see the leave balance");
            btnLeave.UseVisualStyleBackColor = false;
            btnLeave.Click += btnLeave_Click;
            // 
            // This is btnEdit
            // 
            btnEdit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEdit.Enabled = false;
            btnEdit.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnEdit.Location = new Point(684, 10);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(152, 40);
            btnEdit.TabIndex = 3;
            btnEdit.Text = "Edit DTR...";
            toolTip.SetToolTip(btnEdit, "Type in a missing time or correct a wrong one for this employee");
            btnEdit.UseVisualStyleBackColor = false;
            btnEdit.Click += btnEdit_Click;
            // 
            // This is statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblRules });
            statusStrip.Location = new Point(0, 881);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(1417, 26);
            statusStrip.TabIndex = 6;
            statusStrip.ItemClicked += statusStrip_ItemClicked;
            // 
            // This is lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1311, 20);
            lblStatus.Spring = true;
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // This is lblRules
            // 
            lblRules.Name = "lblRules";
            lblRules.Size = new Size(89, 20);
            lblRules.Text = "Office hours";
            // 
            // This is Form1 (the window itself)
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1417, 907);
            Controls.Add(pnlCenter);
            Controls.Add(pnlLeftLine);
            Controls.Add(pnlLeft);
            Controls.Add(pnlStepsLine);
            Controls.Add(pnlSteps);
            Controls.Add(pnlHeaderLine);
            Controls.Add(pnlHeader);
            Controls.Add(statusStrip);
            KeyPreview = true;
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(1240, 784);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dat2DTR - Daily Time Record Maker";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            DragDrop += Form1_DragDrop;
            DragEnter += Form1_DragEnter;
            KeyDown += Form1_KeyDown;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlSteps.ResumeLayout(false);
            tlpSteps.ResumeLayout(false);
            pnlStep1.ResumeLayout(false);
            pnlStep1.PerformLayout();
            pnlStep2.ResumeLayout(false);
            pnlStep2.PerformLayout();
            pnlStep3.ResumeLayout(false);
            pnlStep3.PerformLayout();
            pnlLeft.ResumeLayout(false);
            pnlLeftBottom.ResumeLayout(false);
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            pnlCenter.ResumeLayout(false);
            pnlView.ResumeLayout(false);
            pnlTools.ResumeLayout(false);
            pnlTools.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private LogoBox picSeal;
        private LogoBox picBanner;
        private Panel pnlHeaderLine;
        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnSettings;
        private Button btnHelp;
        private Button btnHolidays;
        private Panel pnlFilterGap;
        private ComboBox cboFilter;
        private Panel pnlSteps;
        private TableLayoutPanel tlpSteps;
        private Panel pnlStep1;
        private StepBadge badge1;
        private Label lblStep1;
        private Button btnOpen;
        private Label lblFile;
        private Panel pnlStep2;
        private StepBadge badge2;
        private Label lblStep2;
        private ComboBox cboMonth;
        private Panel pnlStep3;
        private StepBadge badge3;
        private Label lblStep3;
        private Button btnPrint;
        private Button btnSave;
        private Panel pnlStepsLine;
        private Panel pnlLeft;
        private ListView lvEmployees;
        private ColumnHeader colName;
        private ColumnHeader colId;
        private ColumnHeader colLate;
        private ColumnHeader colSchedule;
        private ColumnHeader colUndertime;
        private ColumnHeader colCheck;
        private Panel pnlLeftBottom;
        private Button btnTickAll;
        private Button btnUntickAll;
        private Button btnNames;
        private CheckBox chkNoScans;
        private Label lblHint;
        private Panel pnlSearch;
        private TextBox txtSearch;
        private Label lblEmployees;
        private Panel pnlLeftLine;
        private Panel pnlCenter;
        private DtrPreviewControl preview;
        private Label lblNotice;
        private Panel pnlTools;
        private Label lblSchedule;
        private ComboBox cboSchedule;
        private Button btnEdit;
        private Button btnLeave;
        private Panel pnlToolsLine;
        private Panel pnlView;
        private Button btnViewDtr;
        private Button btnViewLeave;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblRules;
        private ToolTip toolTip;
    }
}
