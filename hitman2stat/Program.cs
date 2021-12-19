using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace hitman2stat
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (File.Exists("d3d8") && File.Exists("gpcomms.dll"))
            {
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else MessageBox.Show("Some of the dlls were not found:\nd3d8\ngpcomms.dll\nTerminating program.",
                "Hitman 2 Stat Displayer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
