namespace Dat2DTR
{
    partial class SettingsForm
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
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblWhich = new Label();
            cboWhich = new ComboBox();
            grpHours = new GroupBox();
            lblTimeIn = new Label();
            dtpTimeIn = new DateTimePicker();
            lblLunchStart = new Label();
            dtpLunchStart = new DateTimePicker();
            lblLunchEnd = new Label();
            dtpLunchEnd = new DateTimePicker();
            lblTimeOut = new Label();
            dtpTimeOut = new DateTimePicker();
            grpLate = new GroupBox();
            lblGrace = new Label();
            nudGrace = new NumericUpDown();
            lblGraceHint = new Label();
            rdoFromOfficial = new RadioButton();
            rdoAfterGrace = new RadioButton();
            grpDays = new GroupBox();
            chkMon = new CheckBox();
            chkTue = new CheckBox();
            chkWed = new CheckBox();
            chkThu = new CheckBox();
            chkFri = new CheckBox();
            chkSat = new CheckBox();
            chkSun = new CheckBox();
            lblPaper = new Label();
            cboPaper = new ComboBox();
            btnDefaults = new Button();
            btnOk = new Button();
            btnCancel = new Button();
            grpHours.SuspendLayout();
            grpLate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudGrace).BeginInit();
            grpDays.SuspendLayout();
            SuspendLayout();
            //
            // This is lblWhich
            //
            lblWhich.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblWhich.Location = new Point(14, 14);
            lblWhich.Name = "lblWhich";
            lblWhich.Size = new Size(130, 23);
            lblWhich.TabIndex = 8;
            lblWhich.Text = "Schedule to change:";
            lblWhich.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is cboWhich
            //
            cboWhich.DropDownStyle = ComboBoxStyle.DropDownList;
            cboWhich.FormattingEnabled = true;
            cboWhich.Location = new Point(150, 14);
            cboWhich.Name = "cboWhich";
            cboWhich.Size = new Size(356, 23);
            cboWhich.TabIndex = 9;
            cboWhich.SelectedIndexChanged += cboWhich_SelectedIndexChanged;
            //
            // This is grpHours
            //
            grpHours.Controls.Add(lblTimeIn);
            grpHours.Controls.Add(dtpTimeIn);
            grpHours.Controls.Add(lblLunchStart);
            grpHours.Controls.Add(dtpLunchStart);
            grpHours.Controls.Add(lblLunchEnd);
            grpHours.Controls.Add(dtpLunchEnd);
            grpHours.Controls.Add(lblTimeOut);
            grpHours.Controls.Add(dtpTimeOut);
            grpHours.Location = new Point(14, 50);
            grpHours.Name = "grpHours";
            grpHours.Size = new Size(492, 156);
            grpHours.TabIndex = 0;
            grpHours.TabStop = false;
            grpHours.Text = "Office hours";
            //
            // This is lblTimeIn
            //
            lblTimeIn.Location = new Point(16, 28);
            lblTimeIn.Name = "lblTimeIn";
            lblTimeIn.Size = new Size(210, 23);
            lblTimeIn.TabIndex = 0;
            lblTimeIn.Text = "Time In (start of work)";
            lblTimeIn.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is dtpTimeIn
            //
            dtpTimeIn.CustomFormat = "hh:mm tt";
            dtpTimeIn.Format = DateTimePickerFormat.Custom;
            dtpTimeIn.Location = new Point(236, 28);
            dtpTimeIn.Name = "dtpTimeIn";
            dtpTimeIn.ShowUpDown = true;
            dtpTimeIn.Size = new Size(120, 23);
            dtpTimeIn.TabIndex = 1;
            dtpTimeIn.ValueChanged += RuleChanged;
            //
            // This is lblLunchStart
            //
            lblLunchStart.Location = new Point(16, 59);
            lblLunchStart.Name = "lblLunchStart";
            lblLunchStart.Size = new Size(210, 23);
            lblLunchStart.TabIndex = 2;
            lblLunchStart.Text = "Lunch break starts";
            lblLunchStart.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is dtpLunchStart
            //
            dtpLunchStart.CustomFormat = "hh:mm tt";
            dtpLunchStart.Format = DateTimePickerFormat.Custom;
            dtpLunchStart.Location = new Point(236, 59);
            dtpLunchStart.Name = "dtpLunchStart";
            dtpLunchStart.ShowUpDown = true;
            dtpLunchStart.Size = new Size(120, 23);
            dtpLunchStart.TabIndex = 3;
            //
            // This is lblLunchEnd
            //
            lblLunchEnd.Location = new Point(16, 90);
            lblLunchEnd.Name = "lblLunchEnd";
            lblLunchEnd.Size = new Size(210, 23);
            lblLunchEnd.TabIndex = 4;
            lblLunchEnd.Text = "Lunch break ends";
            lblLunchEnd.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is dtpLunchEnd
            //
            dtpLunchEnd.CustomFormat = "hh:mm tt";
            dtpLunchEnd.Format = DateTimePickerFormat.Custom;
            dtpLunchEnd.Location = new Point(236, 90);
            dtpLunchEnd.Name = "dtpLunchEnd";
            dtpLunchEnd.ShowUpDown = true;
            dtpLunchEnd.Size = new Size(120, 23);
            dtpLunchEnd.TabIndex = 5;
            //
            // This is lblTimeOut
            //
            lblTimeOut.Location = new Point(16, 121);
            lblTimeOut.Name = "lblTimeOut";
            lblTimeOut.Size = new Size(210, 23);
            lblTimeOut.TabIndex = 6;
            lblTimeOut.Text = "Time Out (end of work)";
            lblTimeOut.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is dtpTimeOut
            //
            dtpTimeOut.CustomFormat = "hh:mm tt";
            dtpTimeOut.Format = DateTimePickerFormat.Custom;
            dtpTimeOut.Location = new Point(236, 121);
            dtpTimeOut.Name = "dtpTimeOut";
            dtpTimeOut.ShowUpDown = true;
            dtpTimeOut.Size = new Size(120, 23);
            dtpTimeOut.TabIndex = 7;
            //
            // This is grpLate
            //
            grpLate.Controls.Add(lblGrace);
            grpLate.Controls.Add(nudGrace);
            grpLate.Controls.Add(lblGraceHint);
            grpLate.Controls.Add(rdoFromOfficial);
            grpLate.Controls.Add(rdoAfterGrace);
            grpLate.Location = new Point(14, 216);
            grpLate.Name = "grpLate";
            grpLate.Size = new Size(492, 142);
            grpLate.TabIndex = 1;
            grpLate.TabStop = false;
            grpLate.Text = "Late";
            //
            // This is lblGrace
            //
            lblGrace.Location = new Point(16, 28);
            lblGrace.Name = "lblGrace";
            lblGrace.Size = new Size(210, 23);
            lblGrace.TabIndex = 0;
            lblGrace.Text = "Grace period (minutes)";
            lblGrace.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is nudGrace
            //
            nudGrace.Location = new Point(236, 28);
            nudGrace.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            nudGrace.Name = "nudGrace";
            nudGrace.Size = new Size(70, 23);
            nudGrace.TabIndex = 1;
            nudGrace.ValueChanged += RuleChanged;
            //
            // This is lblGraceHint
            //
            lblGraceHint.ForeColor = Color.FromArgb(96, 100, 124);
            lblGraceHint.Location = new Point(16, 58);
            lblGraceHint.Name = "lblGraceHint";
            lblGraceHint.Size = new Size(460, 20);
            lblGraceHint.TabIndex = 2;
            lblGraceHint.Text = "After the grace period, how is Late counted? (the same for every schedule)";
            lblGraceHint.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is rdoFromOfficial
            //
            rdoFromOfficial.Location = new Point(18, 82);
            rdoFromOfficial.Name = "rdoFromOfficial";
            rdoFromOfficial.Size = new Size(462, 24);
            rdoFromOfficial.TabIndex = 3;
            rdoFromOfficial.TabStop = true;
            rdoFromOfficial.Text = "Count from Time In";
            rdoFromOfficial.UseVisualStyleBackColor = true;
            //
            // This is rdoAfterGrace
            //
            rdoAfterGrace.Location = new Point(18, 108);
            rdoAfterGrace.Name = "rdoAfterGrace";
            rdoAfterGrace.Size = new Size(462, 24);
            rdoAfterGrace.TabIndex = 4;
            rdoAfterGrace.TabStop = true;
            rdoAfterGrace.Text = "Count only the minutes after the grace period";
            rdoAfterGrace.UseVisualStyleBackColor = true;
            //
            // This is grpDays
            //
            grpDays.Controls.Add(chkMon);
            grpDays.Controls.Add(chkTue);
            grpDays.Controls.Add(chkWed);
            grpDays.Controls.Add(chkThu);
            grpDays.Controls.Add(chkFri);
            grpDays.Controls.Add(chkSat);
            grpDays.Controls.Add(chkSun);
            grpDays.Location = new Point(14, 368);
            grpDays.Name = "grpDays";
            grpDays.Size = new Size(492, 62);
            grpDays.TabIndex = 2;
            grpDays.TabStop = false;
            grpDays.Text = "Working days (a working day with no scans is marked ABSENT)";
            //
            // This is chkMon
            //
            chkMon.Location = new Point(18, 26);
            chkMon.Name = "chkMon";
            chkMon.Size = new Size(62, 24);
            chkMon.TabIndex = 0;
            chkMon.Text = "Mon";
            chkMon.UseVisualStyleBackColor = true;
            //
            // This is chkTue
            //
            chkTue.Location = new Point(84, 26);
            chkTue.Name = "chkTue";
            chkTue.Size = new Size(62, 24);
            chkTue.TabIndex = 1;
            chkTue.Text = "Tue";
            chkTue.UseVisualStyleBackColor = true;
            //
            // This is chkWed
            //
            chkWed.Location = new Point(150, 26);
            chkWed.Name = "chkWed";
            chkWed.Size = new Size(62, 24);
            chkWed.TabIndex = 2;
            chkWed.Text = "Wed";
            chkWed.UseVisualStyleBackColor = true;
            //
            // This is chkThu
            //
            chkThu.Location = new Point(216, 26);
            chkThu.Name = "chkThu";
            chkThu.Size = new Size(62, 24);
            chkThu.TabIndex = 3;
            chkThu.Text = "Thu";
            chkThu.UseVisualStyleBackColor = true;
            //
            // This is chkFri
            //
            chkFri.Location = new Point(282, 26);
            chkFri.Name = "chkFri";
            chkFri.Size = new Size(62, 24);
            chkFri.TabIndex = 4;
            chkFri.Text = "Fri";
            chkFri.UseVisualStyleBackColor = true;
            //
            // This is chkSat
            //
            chkSat.Location = new Point(348, 26);
            chkSat.Name = "chkSat";
            chkSat.Size = new Size(62, 24);
            chkSat.TabIndex = 5;
            chkSat.Text = "Sat";
            chkSat.UseVisualStyleBackColor = true;
            //
            // This is chkSun
            //
            chkSun.Location = new Point(414, 26);
            chkSun.Name = "chkSun";
            chkSun.Size = new Size(62, 24);
            chkSun.TabIndex = 6;
            chkSun.Text = "Sun";
            chkSun.UseVisualStyleBackColor = true;
            //
            // This is lblPaper
            //
            lblPaper.Location = new Point(30, 444);
            lblPaper.Name = "lblPaper";
            lblPaper.Size = new Size(210, 23);
            lblPaper.TabIndex = 3;
            lblPaper.Text = "Paper size";
            lblPaper.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is cboPaper
            //
            cboPaper.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPaper.FormattingEnabled = true;
            cboPaper.Location = new Point(250, 444);
            cboPaper.Name = "cboPaper";
            cboPaper.Size = new Size(240, 23);
            cboPaper.TabIndex = 4;
            //
            // This is btnDefaults
            //
            btnDefaults.Location = new Point(14, 488);
            btnDefaults.Name = "btnDefaults";
            btnDefaults.Size = new Size(130, 32);
            btnDefaults.TabIndex = 5;
            btnDefaults.Text = "Restore defaults";
            btnDefaults.UseVisualStyleBackColor = true;
            btnDefaults.Click += btnDefaults_Click;
            //
            // This is btnOk
            //
            btnOk.Location = new Point(310, 488);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(94, 32);
            btnOk.TabIndex = 6;
            btnOk.Text = "Save";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            //
            // This is btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(412, 488);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 32);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // This is SettingsForm (the window itself)
            //
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 534);
            Controls.Add(lblWhich);
            Controls.Add(cboWhich);
            Controls.Add(grpHours);
            Controls.Add(grpLate);
            Controls.Add(grpDays);
            Controls.Add(lblPaper);
            Controls.Add(cboPaper);
            Controls.Add(btnDefaults);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Office Hours";
            grpHours.ResumeLayout(false);
            grpLate.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)nudGrace).EndInit();
            grpDays.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Label lblWhich;
        private ComboBox cboWhich;
        private GroupBox grpHours;
        private Label lblTimeIn;
        private DateTimePicker dtpTimeIn;
        private Label lblLunchStart;
        private DateTimePicker dtpLunchStart;
        private Label lblLunchEnd;
        private DateTimePicker dtpLunchEnd;
        private Label lblTimeOut;
        private DateTimePicker dtpTimeOut;
        private GroupBox grpLate;
        private Label lblGrace;
        private NumericUpDown nudGrace;
        private Label lblGraceHint;
        private RadioButton rdoFromOfficial;
        private RadioButton rdoAfterGrace;
        private GroupBox grpDays;
        private CheckBox chkMon;
        private CheckBox chkTue;
        private CheckBox chkWed;
        private CheckBox chkThu;
        private CheckBox chkFri;
        private CheckBox chkSat;
        private CheckBox chkSun;
        private Label lblPaper;
        private ComboBox cboPaper;
        private Button btnDefaults;
        private Button btnOk;
        private Button btnCancel;
    }
}
