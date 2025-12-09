using System.Drawing;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    partial class ProtocolSetupForm
    {
        #region Fields

        private System.ComponentModel.IContainer components = null;
        private Label lblProtocol;
        private ComboBox comboProtocol;
        private Label lblSpeed;
        private NumericUpDown numSpeed;
        private Label lblSlaveAddr;
        private TextBox txtSlaveAddr;
        private Button btnOk;
        private Button btnCancel;

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region InitializeComponent

        private void InitializeComponent()
        {
            lblProtocol = new Label();
            comboProtocol = new ComboBox();
            lblSpeed = new Label();
            numSpeed = new NumericUpDown();
            lblSlaveAddr = new Label();
            txtSlaveAddr = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numSpeed).BeginInit();
            SuspendLayout();
            // 
            // lblProtocol
            // 
            lblProtocol.AutoSize = true;
            lblProtocol.Location = new Point(12, 15);
            lblProtocol.Name = "lblProtocol";
            lblProtocol.Size = new Size(52, 15);
            lblProtocol.TabIndex = 0;
            lblProtocol.Text = "Protocol";
            // 
            // comboProtocol
            // 
            comboProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProtocol.FormattingEnabled = true;
            comboProtocol.Location = new Point(100, 12);
            comboProtocol.Name = "comboProtocol";
            comboProtocol.Size = new Size(140, 23);
            comboProtocol.TabIndex = 1;
            comboProtocol.SelectedIndexChanged += comboProtocol_SelectedIndexChanged;
            // 
            // lblSpeed
            // 
            lblSpeed.AutoSize = true;
            lblSpeed.Location = new Point(12, 49);
            lblSpeed.Name = "lblSpeed";
            lblSpeed.Size = new Size(73, 15);
            lblSpeed.TabIndex = 2;
            lblSpeed.Text = "Speed [kHz]";
            // 
            // numSpeed
            // 
            numSpeed.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numSpeed.Location = new Point(100, 47);
            numSpeed.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            numSpeed.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numSpeed.Name = "numSpeed";
            numSpeed.Size = new Size(140, 23);
            numSpeed.TabIndex = 3;
            numSpeed.Value = new decimal(new int[] { 400, 0, 0, 0 });
            // 
            // lblSlaveAddr
            // 
            lblSlaveAddr.AutoSize = true;
            lblSlaveAddr.Location = new Point(12, 81);
            lblSlaveAddr.Name = "lblSlaveAddr";
            lblSlaveAddr.Size = new Size(71, 15);
            lblSlaveAddr.TabIndex = 4;
            lblSlaveAddr.Text = "I2C Address";
            // 
            // txtSlaveAddr
            // 
            txtSlaveAddr.Location = new Point(100, 78);
            txtSlaveAddr.Name = "txtSlaveAddr";
            txtSlaveAddr.Size = new Size(140, 23);
            txtSlaveAddr.TabIndex = 5;
            txtSlaveAddr.Text = "0x52";
            // 
            // btnOk
            // 
            btnOk.Location = new Point(84, 120);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 25);
            btnOk.TabIndex = 6;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(165, 120);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // ProtocolSetupForm
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(252, 157);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(txtSlaveAddr);
            Controls.Add(lblSlaveAddr);
            Controls.Add(numSpeed);
            Controls.Add(lblSpeed);
            Controls.Add(comboProtocol);
            Controls.Add(lblProtocol);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ProtocolSetupForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Protocol Setup";
            ((System.ComponentModel.ISupportInitialize)numSpeed).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
