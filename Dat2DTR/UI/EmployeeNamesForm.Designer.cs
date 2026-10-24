namespace Dat2DTR
{
    partial class EmployeeNamesForm
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
            lblTip = new Label();
            dgvNames = new DataGridView();
            colId = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colSchedule = new DataGridViewComboBoxColumn();
            pnlButtons = new Panel();
            btnImport = new Button();
            btnExport = new Button();
            lblCount = new Label();
            btnOk = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvNames).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            //
            // This is lblTip
            //
            lblTip.BackColor = Color.White;
            lblTip.Dock = DockStyle.Top;
            lblTip.Location = new Point(0, 0);
            lblTip.Name = "lblTip";
            lblTip.Padding = new Padding(14, 8, 14, 8);
            lblTip.Size = new Size(800, 62);
            lblTip.TabIndex = 0;
            lblTip.Text = "Type the name for each biometric ID, the way it should appear on the DTR (example:  DELA CRUZ, JUAN M).\r\nThen choose the work schedule the person follows.\r\nIDs from the opened .dat file are listed automatically. You only need to do this once.";
            //
            // This is dgvNames
            //
            dgvNames.AllowUserToResizeRows = false;
            dgvNames.BackgroundColor = Color.White;
            dgvNames.BorderStyle = BorderStyle.None;
            dgvNames.ColumnHeadersHeight = 30;
            dgvNames.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvNames.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colSchedule });
            dgvNames.Dock = DockStyle.Fill;
            dgvNames.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvNames.Location = new Point(0, 62);
            dgvNames.Name = "dgvNames";
            dgvNames.RowHeadersWidth = 28;
            dgvNames.RowTemplate.Height = 26;
            dgvNames.Size = new Size(800, 446);
            dgvNames.TabIndex = 1;
            dgvNames.CellEndEdit += dgvNames_CellEndEdit;
            dgvNames.DataError += dgvNames_DataError;
            //
            // This is colId
            //
            colId.HeaderText = "Biometric ID";
            colId.Name = "colId";
            colId.SortMode = DataGridViewColumnSortMode.NotSortable;
            colId.Width = 140;
            //
            // This is colName
            //
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colName.HeaderText = "Name on the DTR";
            colName.Name = "colName";
            colName.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // This is colSchedule
            //
            colSchedule.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            colSchedule.FlatStyle = FlatStyle.Flat;
            colSchedule.HeaderText = "Work schedule";
            colSchedule.Name = "colSchedule";
            colSchedule.Width = 290;
            //
            // This is pnlButtons
            //
            pnlButtons.Controls.Add(btnImport);
            pnlButtons.Controls.Add(btnExport);
            pnlButtons.Controls.Add(lblCount);
            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Location = new Point(0, 508);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Size = new Size(800, 52);
            pnlButtons.TabIndex = 2;
            //
            // This is btnImport
            //
            btnImport.Location = new Point(12, 10);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(112, 32);
            btnImport.TabIndex = 0;
            btnImport.Text = "Import list...";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            //
            // This is btnExport
            //
            btnExport.Location = new Point(130, 10);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(112, 32);
            btnExport.TabIndex = 1;
            btnExport.Text = "Export list...";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            //
            // This is lblCount
            //
            lblCount.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblCount.ForeColor = Color.FromArgb(96, 100, 124);
            lblCount.Location = new Point(252, 10);
            lblCount.Name = "lblCount";
            lblCount.Size = new Size(330, 32);
            lblCount.TabIndex = 2;
            lblCount.Text = "0 of 0 IDs have a name";
            lblCount.TextAlign = ContentAlignment.MiddleLeft;
            //
            // This is btnOk
            //
            btnOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOk.Location = new Point(590, 10);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(94, 32);
            btnOk.TabIndex = 3;
            btnOk.Text = "Save";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            //
            // This is btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(692, 10);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 32);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // This is EmployeeNamesForm (the window itself)
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(800, 560);
            Controls.Add(dgvNames);
            Controls.Add(pnlButtons);
            Controls.Add(lblTip);
            MinimizeBox = false;
            MinimumSize = new Size(700, 400);
            Name = "EmployeeNamesForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Employee Names";
            ((System.ComponentModel.ISupportInitialize)dgvNames).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Label lblTip;
        private DataGridView dgvNames;
        private DataGridViewTextBoxColumn colId;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewComboBoxColumn colSchedule;
        private Panel pnlButtons;
        private Button btnImport;
        private Button btnExport;
        private Label lblCount;
        private Button btnOk;
        private Button btnCancel;
    }
}
