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
            "Headshots",
            "Enemies harmed",
            "Enemies killed",
            "Innocents harmed",
            "Innocents killed",
            "Close encounters",
            "Alerts"
        };

        uint[] pointers =
        {
            0x208,
            0x20C,
            0x210,
            0x214,
            0x218,
            0x220,
            0x21C
        };

        uint baseaddr = 0x0;
        uint basepoint = 0x0;
        uint basepoint2 = 0x0;

        /*Less effective method
        //Odd levels
        uint[] offsets =
        {
            0x4,
            0x48,
            0x14,
            0x4,
            0x10,
            0x4,
            0x7C,
            0x7C,
            0x667
        };

        //Even levels
        uint[] offsets2 =
        {
            0x0,
            0x0,
            0x4,
            0x0,
            0x4,
            0x0,
            0x4,
            0x0,
            0x4,
            0x44,
            0x4,
            0x10,
            0x4,
            0x7C,
            0x7C,
            0x667
        };*/

        string panel_text;
        bool show_panel = false;
        //bool offs_type = false;

        const string filename = "hitman2stat.ini";
        const string fname = "hitman2.exe";
        const string pname = "hitman2";
        const string dll1 = "d3d8", dll2 = "d3d8.dll";

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
                pl_h = (short)(pl_font * 8);
            }
            if (Memory.IsProcessOpen(pname))
            {
                mem = new Memory(pname, 0x001F0FFF);
                button1.Enabled = false;
                DoInject();
            }
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
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
            //Shots fired
            //Get write access
            mem.SetProtection(baseaddr + 0x00110800, 0x100, Memory.Protection.PAGE_READWRITE);
            //JMP
            mem.WriteByte(baseaddr + 0x00110852, new byte[] { 0xEB, 0xD2 } ,2);
            //ECX value place - pointer helper
            mem.WriteByte(baseaddr + 0x0011081C, new byte[] { 0x0, 0x0, 0x0, 0x0 }, 4);
            //Original code
            mem.WriteByte(baseaddr + 0x00110826, new byte[] { 0x8B, 0xF1 }, 2);
            //MOV ECX value
            mem.WriteByte(baseaddr + 0x00110828, new byte[] {0x89, 0x0D, 0x1C, 0x08, 0x51, 0x00}, 6);
            //JMP
            mem.WriteByte(baseaddr + 0x0011082E, new byte[] { 0xEB, 0x24 }, 2);

            //Other stats
            //Get write access
            mem.SetProtection(baseaddr + 0x00105D00, 0x100, Memory.Protection.PAGE_READWRITE);
            //JMP
            mem.WriteByte(baseaddr + 0x00105DF1, new byte[] { 0xEB, 0x5F }, 2);
            //ESI value place - pointer helper
            mem.WriteByte(baseaddr + 0x00105DF3, new byte[] { 0x0, 0x0, 0x0, 0x0 }, 4);
            //Original code
            mem.WriteByte(baseaddr + 0x00105E52, new byte[] { 0x89, 0x9E, 0x08, 0x02, 0x00, 0x00 }, 6);
            //MOV ESI value
            mem.WriteByte(baseaddr + 0x00105E58, new byte[] { 0x89, 0x35, 0xF3, 0x5D, 0x50, 0x00 }, 6);
            //JMP
            mem.WriteByte(baseaddr + 0x00105E5E, new byte[] { 0xEB, 0x97 }, 2);

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
            }
            /*if (e.KeyCode == Keys.F11)
                offs_type = !offs_type;*/
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Memory.IsProcessOpen(pname))
            {
                if (show_panel && (basepoint = (uint)mem.Read(baseaddr + 0x00105DF3)) > 0)
                {
                    basepoint2 = (uint)mem.Read(baseaddr + 0x0011081C);
                    int i;
                    /*Less effective method
                    if (offs_type)
                    {
                        basepoint2 = (uint)mem.Read(baseaddr + 0x002A8C68) + 0x8;
                        for (i = 0; i < offsets.Length; i++)
                            basepoint2 = (uint)mem.Read(basepoint2) + offsets[i];
                        panel_text = "Shots fired - " + mem.Read(basepoint2).ToString() + "\n";
                    }
                    else
                    {
                        basepoint2 = (uint)mem.Read(baseaddr + 0x002A8C68) + 0x4F8;
                        for (i = 0; i < offsets2.Length; i++)
                            basepoint2 = (uint)mem.Read(basepoint2) + offsets2[i];
                        panel_text = "Shots fired - " + mem.Read(basepoint2).ToString() + "\n";
                    }*/
                    panel_text = "Shots fired - " + mem.Read(basepoint2 + 0x11C7).ToString() + "\n";
                    for (i = 0; i < pointers.Length; i++)
                        panel_text += dnames[i] + " - " + mem.Read(basepoint + pointers[i]).ToString() + "\n";
                    GPML_SetTextMultilineData(0, pl_x, pl_y, panel_text, pl_color, pl_backg, pl_font, true, pl_w, pl_h, 0);
                }
            }
            else
                Environment.Exit(0);
        }
    }
}