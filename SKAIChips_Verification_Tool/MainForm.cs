using System;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    /// <summary>
    /// Main MDI container form providing access to register control and instrument setup utilities.
    /// </summary>
    public partial class MainForm : Form
    {
        private RegisterControlForm _regForm;
        private InstrumentForm _instrumentForm;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the form load event and sets the application title.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = "SKAIChips Verification Tool V4.0.0";
        }

        /// <summary>
        /// Opens the register control form as an MDI child.
        /// </summary>
        private void menuRegisterControl_Click(object sender, EventArgs e)
        {
            if (_regForm == null || _regForm.IsDisposed)
            {
                _regForm = new RegisterControlForm
                {
                    MdiParent = this,
                    WindowState = FormWindowState.Maximized
                };
                _regForm.Show();
            }
            else
            {
                _regForm.Activate();
            }
        }

        /// <summary>
        /// Closes the application when the Exit menu item is selected.
        /// </summary>
        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Opens the instrument setup form as a modal dialog anchored to the main window.
        /// </summary>
        private void menuSetupInstrument_Click(object sender, EventArgs e)
        {
            if (_instrumentForm == null || _instrumentForm.IsDisposed)
            {
                _instrumentForm = new InstrumentForm
                {
                    StartPosition = FormStartPosition.CenterParent
                };
                _instrumentForm.Show(this);
            }
            else
            {
                if (!_instrumentForm.Visible)
                    _instrumentForm.Show(this);

                _instrumentForm.Activate();
            }
        }
    }
}
