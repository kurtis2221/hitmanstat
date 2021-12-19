using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using MemoryEdit;

namespace hitman2stat
{
    public partial class Form1 : Form
    {
        [DllImport("gpcomms.dll")]
        static extern void GPML_SetTextMultilineData(byte cObjectNumber,
            short wTextPosX, short wTextPosY, string pText,
            uint dwTextColor, bool bBlackBackground, byte cSize,
            bool bTextBold, short sizeX, short sizeY, byte cFontFamily);

        [DllImport("gpcomms.dll")]
        static extern bool GPML_ShowText(byte cObjectNumber, bool bShowIt);

        Memory mem;
        KeyHook.GlobalKeyboardHook gkh;

        string[] dnames =
        {
            "Shots fired",
            "Shots hit",
            "Headshots",
            "Accidents",
            "Close combat kills",
            "Targets killed",
            "Enemies killed",
            "Police killed",
            "Civilians killed",
            "Bodies found",
            "Covers blown"
        };

        uint[] addresses =
        {
            0x005B3B54,
            0x005B3B58,
            0x005B3B60,
            0x005B3B6C,
            0x005B3BB0,
            0x005B3BA4,
            0x005B3B78,
            0x005B3B88,
            0x005B3B90,
            0x005B3C08,
            0x005B3BA0
        };

        uint baseaddr = 0x0;

        string panel_text;
        bool show_panel = false;

        const string filename = "hitman4stat.ini";
        const string fname = "HitmanBloodMoney.exe";
        const string pname = "HitmanBloodMoney";
        const string dll1 = "d3d9", dll2 = "d3d9.dll";

        short pl_x = 0, pl_y = 256;
        byte pl_font = 16;
        short pl_w = 160, pl_h = 128;
        uint pl_color = 0xFFFFFFFF;
        bool pl_backg = true;
        Keys pl_key = Keys.F10;

        public Form1()
        {
            InitializeComponent();
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            if (File.Exists(filename))
            {
                string[] tmp;
                StreamReader sr = new StreamReader(filename, Encoding.Default);
                if (sr.Peek() > -1 && (tmp = sr.ReadLine().Split(',')).Length > 1)
                {
                    if (!short.TryParse(tmp[0], out pl_x)) pl_x = 0;
                    if (!short.TryParse(tmp[1], out pl_y)) pl_y = 256;
                }
                if (sr.Peek() > -1 && !byte.TryParse(sr.ReadLine(), out pl_font))
                    pl_font = 16;
                if (sr.Peek() > -1 && !uint.TryParse(sr.ReadLine(), System.Globalization.NumberStyles.HexNumber, Application.CurrentCulture, out pl_color))
                    pl_color = 0xFFFFFFFF;
                if (sr.Peek() > -1 && !bool.TryParse(sr.ReadLine(), out pl_backg))
                    pl_backg = true;
                if (sr.Peek() > -1)
                {
                    string tkey = sr.ReadLine();
                    if (Enum.IsDefined(typeof(Keys), tkey))
                        pl_key = (Keys)Enum.Parse(typeof(Keys), tkey);
                }
                sr.Close();
                tmp = null;
                pl_w = (short)(pl_font * 10);
                pl_h = (short)(pl_font * 11);
            }
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (File.Exists(fname))
            {
                button1.Enabled = false;
                if (File.Exists(dll1))
                    File.Move(dll1, dll2);
                System.Diagnostics.Process.Start(fname);
            retry:
                System.Threading.Thread.Sleep(100);
                if (!Memory.IsProcessOpen(pname))
                    goto retry;
                mem = new Memory(pname, 0x001F0FFF);
                DoInject();
                System.Threading.Thread.Sleep(500);
                if (File.Exists(dll2))
                    File.Move(dll2, dll1);
            }
        }

        private void DoInject()
        {
            baseaddr = mem.base_addr;
            //Enable key hook
            gkh = new KeyHook.GlobalKeyboardHook();
            gkh.Hook();
            gkh.KeyUp += new KeyEventHandler(gkh_KeyUp);
            timer1.Start();
        }

        private void gkh_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == pl_key)
            {
                show_panel = !show_panel;
                GPML_ShowText(0, show_panel);
                e.Handled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Memory.IsProcessOpen(pname))
            {
                if (show_panel)
                {
                    panel_text = null;
                    for (int i = 0; i < addresses.Length; i++)
                        panel_text += dnames[i] + " - " + mem.Read(baseaddr + addresses[i]).ToString() + "\n";
                    GPML_SetTextMultilineData(0, pl_x, pl_y, panel_text, pl_color, pl_backg, pl_font, true, pl_w, pl_h, 0);
                }
            }
            else
                Environment.Exit(0);
        }
    }
}