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
    public partial class GmailDemoForm : Form
    {
        public GmailDemoForm()
        {
            InitializeComponent();
        }
        string Email;
        int NumberOfMails, MailboxSize;
        
        public Pop3_1.POP3 demo;
        #region Trace print 
        void print(string s)
        {
            CommandShow.Text += s;
        }
        #endregion
        /// <summary>
        /// CONNECT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Connect_Click(object sender, EventArgs e)
        {
            demo = new Pop3_1.POP3(txtServerName.Text, int.Parse(txtPort.Text), true, txtUserName.Text, txtPassWord.Text);

            demo.Trace += new Pop3_1.TraceHandler(print);
            demo.ReadTimeout = 60000;
            demo.Connect();
        }

        public List<string> GetMail(string temp)//để form_mail lấy dữ liệu Mail
        {
            List<string> result = new List<string> { };
            while (temp.Trim() != null)
            {

                if (temp.Length == 0) break;

                if (temp.ToUpper().StartsWith("FROM"))
                {
                    result.Add(temp.Substring(5, temp.Length - 5));
                    //From[i] = From[i].Substring(From[i].IndexOf('<'));
                    MessageBox.Show("from");
                }

                if (temp.ToUpper().StartsWith("SUBJECT"))
                {
                    result.Add(temp.Substring(8, temp.Length - 8));
                    MessageBox.Show("subject");
                }

            }
            return result;
        }
        public List<string> GetFormMail;
        public string From = "";
        public string Subject = "";
        public string _Message = "";
        public string number = "";
        private void RETR(object sender, EventArgs e)
        {
            number = textBoxNum.Text;
            //get email
            if (textBoxNum.Text == "")
            {
                MessageBox.Show("Please enter the ordinal number of the mail.", "Warning", MessageBoxButtons.OK);
                return;
            }
            try
            {
                demo.GetRawEmail(Int32.Parse(textBoxNum.Text), out Email);
                textBoxNum.Clear();

                //get value from/subject
                From = demo.GetValueFromHeader(Email, "From: ");
                Subject = demo.GetValueFromHeader(Email, "Subject: ");

                //get value
                _Message = demo.GetBodyMail(Email);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK);
            }

        }


        #region Disconnect
        private void btnDisconect_Click(object sender, EventArgs e)
        {
            demo.Disconnect();
        }
        #endregion
        /// <summary>
        /// xóa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CLEAR(object sender, EventArgs e)
        {
            CommandShow.Clear();
        }

        private void STAT(object sender, EventArgs e)
        {
            demo.GetMailboxStats(out NumberOfMails, out MailboxSize);
        }

        private void LIST(object sender, EventArgs e)
        {
            //get a list of mails
            List<int> EmailIds;
            demo.GetEmailIdList(out EmailIds);
        }

        private void LISTn(object sender, EventArgs e)
        {
            //get email size
            if (textBoxNum.Text == "")
            {
                MessageBox.Show("Please Enter the numberlist.", "Warning", MessageBoxButtons.OK);
            }
            else
            {
                demo.GetEmailSize(int.Parse(textBoxNum.Text));
            }
            textBoxNum.Clear();
        }
        private void NOOP(object sender, EventArgs e)
        {
            demo.NOOP();
        }

        private void txtUserName_Click(object sender, EventArgs e)
        {
            txtUserName.Text = "";
        }

        private void txtPassWord_Click(object sender, EventArgs e)
        {
            txtPassWord.Text = "";
        }
        private void DELE(object sender, EventArgs e)
        {
            if (textBoxNum.Text == "")
            {
                MessageBox.Show("Please Enter the numberlist.", "Warning", MessageBoxButtons.OK);
                return;
            }
            //delete email
            demo.DeleteEmail(int.Parse(textBoxNum.Text));
            textBoxNum.Clear();
        }
        private void RSET(object sender, EventArgs e)
        {
          demo.UndeleteAllEmails();
        }
    }
}
