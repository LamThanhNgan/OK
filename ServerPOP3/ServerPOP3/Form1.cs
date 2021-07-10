using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;

namespace ServerPOP3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        POP3server pop;
        void Print(string s)
        {
            listview.Items.Add(new ListViewItem(s));
            CheckForIllegalCrossThreadCalls = false;
        }
        private void btnListen_Click(object sender, EventArgs e)
        {
            pop = new POP3server();
            pop.Trace += new TraceHandler(Print);
            pop.Connect();
        }
    }
}
