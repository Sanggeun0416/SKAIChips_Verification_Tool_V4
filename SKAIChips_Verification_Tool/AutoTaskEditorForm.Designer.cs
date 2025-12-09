using System.Drawing;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    partial class AutoTaskEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        private DataGridView dgvBlocks;
        private Button btnOk;
        private Button btnCancel;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnUp;
        private Button btnDown;

        private DataGridViewComboBoxColumn colType;
        private DataGridViewTextBoxColumn colAddr;
        private DataGridViewTextBoxColumn colValue;
        private DataGridViewTextBoxColumn colDelay;
        private DataGridViewTextBoxColumn colTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            dgvBlocks = new DataGridView();
            colType = new DataGridViewComboBoxColumn();
            colAddr = new DataGridViewTextBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            colDelay = new DataGridViewTextBoxColumn();
            colTitle = new DataGridViewTextBoxColumn();
            btnOk = new Button();
            btnCancel = new Button();
            btnAdd = new Button();
            btnRemove = new Button();
            btnUp = new Button();
            btnDown = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvBlocks).BeginInit();
            SuspendLayout();
            // 
            // dgvBlocks
            // 
            dgvBlocks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvBlocks.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvBlocks.Columns.AddRange(new DataGridViewColumn[] { colType, colAddr, colValue, colDelay, colTitle });
            dgvBlocks.Location = new Point(12, 12);
            dgvBlocks.MultiSelect = true;
            dgvBlocks.Name = "dgvBlocks";
            dgvBlocks.RowTemplate.Height = 25;
            dgvBlocks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBlocks.Size = new Size(640, 320);
            dgvBlocks.TabIndex = 0;
            // 
            // colType
            // 
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.Width = 80;
            // 
            // colAddr
            // 
            colAddr.HeaderText = "Address (hex)";
            colAddr.Name = "colAddr";
            colAddr.Width = 130;
            // 
            // colValue
            // 
            colValue.HeaderText = "Value / Key";
            colValue.Name = "colValue";
            colValue.Width = 130;
            // 
            // colDelay
            // 
            colDelay.HeaderText = "Delay(ms)";
            colDelay.Name = "colDelay";
            colDelay.Width = 80;
            // 
            // colTitle
            // 
            colTitle.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colTitle.HeaderText = "Title";
            colTitle.Name = "colTitle";
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.Location = new Point(496, 345);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 25);
            btnOk.TabIndex = 5;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(577, 345);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAdd.Location = new Point(12, 345);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(60, 25);
            btnAdd.TabIndex = 1;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnRemove
            // 
            btnRemove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRemove.Location = new Point(78, 345);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(60, 25);
            btnRemove.TabIndex = 2;
            btnRemove.Text = "Remove";
            btnRemove.UseVisualStyleBackColor = true;
            btnRemove.Click += btnRemove_Click;
            // 
            // btnUp
            // 
            btnUp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUp.Location = new Point(144, 345);
            btnUp.Name = "btnUp";
            btnUp.Size = new Size(60, 25);
            btnUp.TabIndex = 3;
            btnUp.Text = "Up";
            btnUp.UseVisualStyleBackColor = true;
            btnUp.Click += btnUp_Click;
            // 
            // btnDown
            // 
            btnDown.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDown.Location = new Point(210, 345);
            btnDown.Name = "btnDown";
            btnDown.Size = new Size(60, 25);
            btnDown.TabIndex = 4;
            btnDown.Text = "Down";
            btnDown.UseVisualStyleBackColor = true;
            btnDown.Click += btnDown_Click;
            // 
            // AutoTaskEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(664, 381);
            Controls.Add(btnDown);
            Controls.Add(btnUp);
            Controls.Add(btnRemove);
            Controls.Add(btnAdd);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(dgvBlocks);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AutoTaskEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "AutoTask Editor";
            ((System.ComponentModel.ISupportInitialize)dgvBlocks).EndInit();
            ResumeLayout(false);
        }
    }
}
