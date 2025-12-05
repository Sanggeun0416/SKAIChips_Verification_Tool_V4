using System;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
