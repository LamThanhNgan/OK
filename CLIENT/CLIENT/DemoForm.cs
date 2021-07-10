using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CLIENT
{
    public partial class DemoForm : Form
    {
        public DemoForm()
        {
            InitializeComponent();
        }

        private void btnMyserver_Click(object sender, EventArgs e)
        {
            MyserverDemoForm severForm = new MyserverDemoForm();
            severForm.Show();
            this.Hide();
        }

        private void btnAvailServer_Click(object sender, EventArgs e)
        {
            GmailDemoForm severForm = new GmailDemoForm();
            severForm.Show();
            this.Hide();
        }
    }
}
