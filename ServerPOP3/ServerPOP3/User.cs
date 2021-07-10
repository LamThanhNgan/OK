using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerPOP3
{
    class User
    {
        private string username = "";
        private string password = "";
        private bool isConnect = false;
        bool isRset = false;

        public string Username
        {
            get { return username; }
            set { username = value; }
        }
        public bool IsRset
        {
            get { return isRset; }
            set { isRset = value; }
        }
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public User(string user, string pass)
        {
            username = user;
            password = pass;
            
        }
    }
}
