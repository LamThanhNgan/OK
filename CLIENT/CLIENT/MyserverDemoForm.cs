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
    public partial class MyserverDemoForm : Form
    {
        public MyserverDemoForm()
        {
            InitializeComponent();
        }

        string Email;
        int NumberOfMails, MailboxSize;
        int num;
        public Pop3.POP3 demo;

        #region Trace print 
        void print(string s)
        {
            CommandShow.Text += s;
            //CommandShow1.Items.Add(new ListViewItem(s));
        }
        #endregion

        #region Connect
        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Connect_Click(object sender, EventArgs e)
        {
            demo = new Pop3.POP3(txtServerName.Text, int.Parse(txtPort.Text), true, txtUserName.Text, txtPassWord.Text);

            demo.Trace += new Pop3.TraceHandler(print);
            demo.ReadTimeout = 60000;
            demo.Connect();
        }
        #endregion

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
        private void btnGetndMail_Click(object sender, EventArgs e)
        {
            number = textBoxNum.Text;
            //get email
            //richTextBoxMail.Text = "";
            if (textBoxNum.Text == "")
            {
                MessageBox.Show("Please enter the ordinal number of the mail.", "Warning", MessageBoxButtons.OK);
                return;
            }
            try
            {
                demo.GetRawEmail(Int32.Parse(textBoxNum.Text), out Email);
                textBoxNum.Clear();
                //richTextBoxMail.Text += Email;

                //get value from/subject
                //From = demo.GetValueFromHeader(Email, "From: ");
                //Subject = demo.GetValueFromHeader(Email, "Subject: ");

                //get value
                //_Message = demo.GetBodyMail(Email);
                //Show Form
                //Form_Mail form_Mail = new Form_Mail(this);
                //form_Mail.Show();
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

        private void lblStat_Click(object sender, EventArgs e)
        {
            demo.GetMailboxStats(out NumberOfMails, out MailboxSize);
        }

        private void LIST_Click(object sender, EventArgs e)
        {
            //get a list of mails
            List<int> EmailIds;
            demo.GetEmailIdList(out EmailIds);
        }

        private void label4_Click(object sender, EventArgs e)
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
        private void btnDel_Click(object sender, EventArgs e)
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
        private void label8_Click(object sender, EventArgs e)
        {
            demo.NOOP();
        }

        private void btnCreateNew_Click(object sender, EventArgs e)
        {
            demo.CreateNewAccount();
        }

        #region Clear
        /// <summary>
        /// Clear the richtextbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            CommandShow.Clear();
        }
        #endregion

        private void REset(object sender, EventArgs e)
        {
            demo.UndeleteAllEmails();
        }
    }
}
