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
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }
        public Pop3.POP3 demo;
        public Pop3.POP3 POP3
        {
            get { return demo; }
            set { POP3 = demo; }
        }
        string TraceHandler = "";
        void print(string s)
        {
            //richTextBox1.AppendText(s);
            TraceHandler = s;
        }
        private void btn_Login_Click(object sender, EventArgs e)
        {
            demo = new Pop3.POP3("pop.Team11Server.nt106", 110, false, txt_UserName.Text, txt_Pass.Text);

            demo.Trace += new Pop3.TraceHandler(print);
            demo.ReadTimeout = 60000;
            if (demo.Connect() == false)
            {
                MessageBox.Show("Username or Password is incorrect!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                MailForm FormUser = new MailForm(demo);
                FormUser.Show();
                this.Hide();
            }
        }

        private void btn_CreatNewUser_Click(object sender, EventArgs e)
        {
            bool S = demo.CreateNewAccount();
            if (S == false)
            {
                MessageBox.Show("Username has already existed!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                MailForm FormUser = new MailForm(demo);
                FormUser.Show();
                this.Hide();
            }
        }
    }
}
