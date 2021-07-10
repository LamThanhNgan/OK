using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CLIENT
{
    public partial class MailForm : Form
    {
        private Pop3.POP3 demo;
        int NumberOfMails, MailboxSize;


        void Print(string s)
        {
            // richTextBox1.AppendText(s) ;
        }

        #region Construstor 
        public MailForm()
        {
            InitializeComponent();
        }
        public MailForm(Pop3.POP3 F)
        {
            InitializeComponent();
            demo = F;
            demo.Trace += new Pop3.TraceHandler(Print);
        }
        #endregion

        /// <summary>
        /// Lấy số lượng mail có trong account 
        /// </summary>
        public void GetNumberOfMail()
        {
            demo.GetMailboxStats(out NumberOfMails, out MailboxSize);
        }
        string getID(string email)
        {
            int n = email.IndexOf("Message-ID:");
            int m = email.IndexOf("From");
            email = email.Substring(n, m - n);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }

        string getFromAddress(string email)
        {
            int n = email.IndexOf("From:");
            int m = email.IndexOf("To:");
            email = email.Substring(n, m - n);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }

        string getToAddress(string email)
        {
            int n = email.IndexOf("To:");
            int m = email.IndexOf("Subject:");
            email = email.Substring(n, m - n);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }
        string getSubjectAddress(string email)
        {
            int n = email.IndexOf("Subject:");
            int m = email.IndexOf("Content:");
            email = email.Substring(n, m - n);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }
        string getContentAddress(string email)
        {
            int n = email.IndexOf("Content:");
            int m = email.Length;
            email = email.Substring(n, m - 5);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }
        string getDate(string email)
        {
            int n = email.IndexOf("Date:");
            int m = email.IndexOf("From:");
            email = email.Substring(n, m - n);
            int k = email.IndexOf(":");
            return email.Substring(k + 1);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            rtxt_Mail.Clear();
            int mailNumber = e.RowIndex + 1;
            string mail = "";
            demo.GetRawEmail(mailNumber, out mail);
            string username = getToAddress(mail);
            int a = username.IndexOf('\r');
            username = username.Substring(1,a-2);
            int n = mail.IndexOf("+OK");
            int m = mail.Length;
            mail = mail.Substring(n, m - 5);
            int k = mail.IndexOf(":");
            mail = mail.Substring(k + 1);
            rtxt_Mail.AppendText(mail);
            dataGridView1.Rows.Remove(dataGridView1.Rows[e.RowIndex]);
            demo.DeleteEmail(mailNumber);
            string path = Directory.GetCurrentDirectory();
            string filePath = path + username + "Number" + mailNumber + ".txt";
            FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate);
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(rtxt_Mail.Text);
            }

            fs.Close();
        }

        private void MailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            demo.Disconnect();
        }

        private void btn_RSET_Click(object sender, EventArgs e)
        {
            demo.UndeleteAllEmails();
            LoadMail();
        }
        private void LoadMail()
        {

            dataGridView1.Rows.Clear();
            rtxt_Mail.Clear();
            txtUsername.Text = demo.GetUser;
            txtTime.AppendText(DateTime.Now.ToString());
            string Email = "";
            demo.GetMailboxStats(out NumberOfMails, out MailboxSize);
            for (int i = 1; i <= NumberOfMails; i++)
            {
                demo.GetRawEmail(i, out Email);

                string _Sender = getFromAddress(Email);
                string _Subject = getSubjectAddress(Email);
                string _Date = getDate(Email);
                dataGridView1.Rows.Add(_Sender, _Subject, _Date);

            }
        }

        private void MailForm_Load(object sender, EventArgs e)
        {
            
            LoadMail();
        }

    }
}
