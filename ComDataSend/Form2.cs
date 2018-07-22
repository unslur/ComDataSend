using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(".");
            int fileNum = dir.GetFiles().Length; // 该目录下的文件数量。
            if (fileNum > 4) {
                PrintLog("打开串口");
                timer1.Enabled = false;
            }
            PrintLog(fileNum.ToString());
        }
        private void PrintLog(string info)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                if (textBox1.Text.Length > 0)
                {
                    textBox1.AppendText("\r\n");
                }
                textBox1.AppendText(info);
            }));
        }
    }
}
