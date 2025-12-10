using System;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    /// <summary>
    /// Entry point class for launching the SKAI Chips verification tool application.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        /// <summary>
        /// Configures the application and starts the main form.
        /// </summary>
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
