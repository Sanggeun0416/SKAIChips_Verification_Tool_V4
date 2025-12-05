using System;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    public partial class MainForm : Form
    {
        private RegisterControlForm _regForm;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = "SKAIChips Verification Tool V4.0.0";
        }

        private void menuRegisterControl_Click(object sender, EventArgs e)
        {
            if (_regForm == null || _regForm.IsDisposed)
            {
                _regForm = new RegisterControlForm();
                _regForm.MdiParent = this;
                _regForm.WindowState = FormWindowState.Maximized;
                _regForm.Show();
            }
            else
            {
                _regForm.Activate();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
