using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace ServerPOP3
{
    #region DELEGATES
    /// <summary>
    /// định dạng thông tin giữa client,server
    /// </summary>
    /// <param name="TraceText"></param>
    public delegate void TraceHandler(string TraceText);
    #endregion
     
    class POP3server
    {
        #region TRACE
        /// <summary>
        /// Shows the communication between PopClient and PopServer, including warnings
        /// </summary>
        public event TraceHandler Trace;

        /// <summary>
        /// call Trace event
        /// </summary>
        /// <param name="text">string to be traced</param>
        /// <param name="parameters"></param>
        protected void CallTrace(string text, params object[] parameters)
        {
            if (Trace != null)
            {
                Trace(DateTime.Now.ToString("hh:mm:ss ") + " " + string.Format(text, parameters));
            }
        }

        /// <summary>
        /// Trace information received from POP3 server
        /// </summary>
        /// <param name="text">string to be traced</param>
        /// <param name="parameters"></param>
        protected void TraceFrom(string text, params object[] parameters)
        {
            if (Trace != null)
            {
                CallTrace("   " + string.Format(text, parameters));
            }
        }
        #endregion

        #region Private variaties
        public List<Socket> ListClients = new List<Socket>();
        private User user;
        private IPEndPoint IP;
        private Socket server;
        private string message = "";                        // cả dòng nội dung được nhận từ phía client
        ConnectSQL.Connect SQL = new ConnectSQL.Connect();
        private bool isListen = false;
        private string CRLF = "\r\n";
        private List<ConnectSQL.Connect.ListMail> listmail;
        private int Connections = 0;
        private string key = "Nhom-11-NT106.L21-19521884-19520500";
        #endregion

        #region Properties
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
       #endregion

        #region Constructor
        public POP3server()
        {
            //doing nothing
        }
        #endregion

        #region Connect
        /// <summary>
        /// Connect to client   
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            this.IP = new IPEndPoint(IPAddress.Any, 110);
            this.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);  // Bắt đầu lắng nghe

            TraceFrom("Server running on 127.0.0.1 : 110");
            TraceFrom(DateTime.Now.ToString());
            bool isConnect = false;
            isListen = true;

            Thread listen = new Thread(() =>
            {
                try
                {
                    while (isListen)
                    {
                        server.Listen(100);
                        if (Connections < 100)
                        {
                            Socket client = server.Accept();
                            Connections += 1;

                            //listClient.Add(client);
                            IPEndPoint ad = client.RemoteEndPoint as IPEndPoint;
                            ListClients.Add(client);

                             user = new User("", "");

                            TraceFrom("New client connected from: " + ad.Address.ToString() + " | Port :" + ad.Port);
                            SendRespondToClient(client, "+OK nhom11server ready for requests from " + ad.Address.ToString() + CRLF);
                            Thread re = new Thread(Receive);
                            re.IsBackground = true;
                            re.Start(client);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceFrom(ex.Message);
                    IP = new IPEndPoint(IPAddress.Any, 8080);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
            });
            listen.IsBackground = true;
            listen.Start();
            isConnect = true;
            return isConnect;
        }
        #endregion

        #region Create new account
        public bool CreateNewAccount(string User, string Pass)
        {

            string UserEncrypted = SQL.Encrypt(User, key);
            string PassEncrypted = SQL.Encrypt(Pass, key);

            if (SQL.CheckUsername(User) == false)
            {
                //username = User;
                //password = Pass;
                user.Username = UserEncrypted;
                user.Password = PassEncrypted;

                SQL.CreatNewUser(user.Username, user.Password);
                TraceFrom("Created new account.");
                return true;
            }
            else
            {
                TraceFrom("This username has already existed");
                return false;
            }

        }
        #endregion

        #region Receive
        byte[] data;
        /// <summary>
        /// Nhận các requests từ người dùng.
        /// </summary>
        /// <param name="obj"></param>
        public void Receive(object obj)      // nhan lien tuc
        {
            Socket _client = obj as Socket;
            int number ;
            string PassUser;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024*50000];

                    _client.Receive(data);

                    message = System.Text.Encoding.ASCII.GetString(data);
                    TraceFrom("Client just sent :" + message);
       
                    message = message.Substring(0,message.IndexOf("\r")+2);
                    message = SQL.Decrypt(message, key);
                    message = message + CRLF;
    
                    number = 0;
                    PassUser = "";
                    string cmd = message.Substring(0, 4);
                    
                    int n = message.IndexOf(" ");
                    
                    int m;
                    if (n > -1)
                    {
                        m = message.IndexOf("\r");
                        
                        bool isNum = Int32.TryParse(message.Substring(n, m - n), out number);
                        if(isNum == false)
                        {
                            PassUser = message.Substring(n, m - n);
                            ChooseCommand(_client, cmd, PassUser);
                        }
                        else
                        {
                            ChooseCommand(_client, cmd, number);
                        }
                    }
                    else
                    {
                        ChooseCommand(_client, cmd, number);
                    }    
                }
            }
            catch (Exception ex)
            {
                //AddMess(ex.Message);
                ListClients.Remove(_client);
                _client.Close();
                TraceFrom(ex.Message);
            }
        }
        #endregion

        #region Send respond
        /// <summary>
        /// gởi lại hồi  đáp 
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="respond"></param>
        public void SendRespondToClient(Socket _client,string respond)
        {
            respond = SQL.Encrypt(respond, key);
            byte[] data = new byte[1024 * 500000];
            data = System.Text.Encoding.ASCII.GetBytes(respond);
            try
            {
                _client.Send(data);
            }
            catch (Exception ex)
            {
                TraceFrom(ex.ToString());
            }
        }
        #endregion

        #region getUserName , pass
        void getUserName(string command)
        {
           user.Username = message.Substring(message.IndexOf(" ") + 1);
            //TraceFrom(username);
        }
        void getPassWord(string command)
        {
            user.Password = message.Substring(message.IndexOf(" ") + 1);
            //TraceFrom(password);
        }
        #endregion

        #region AUTH
        /// <summary>
        /// Check 
        /// </summary>
        /// <param name="User"></param>
        /// <param name="Pass"></param>
        /// <returns></returns>
        bool CheckUser(string User, string Pass)
        {
            int n = User.IndexOf("\r");
            int m = Pass.IndexOf("\r");
            User = User.Substring(0, n);
            Pass = Pass.Substring(0, m);
            string UserEncrypted = SQL.Encrypt(User, key);
            string PassEncrypted = SQL.Encrypt(Pass, key);
            TraceFrom(UserEncrypted);
            TraceFrom(PassEncrypted);
            user.Username = UserEncrypted;
            user.Password = PassEncrypted;

            bool Check = SQL.CheckAccount(user.Username ,user.Password );
            if (Check == true)
            {
                TraceFrom("Successful connection");
                return Check;
            }
            else
            {
                TraceFrom("User or password is incorrect");
                return Check;
            }
        }
        #endregion

        #region Switch commands
        public void ChooseCommand(Socket _client, string cmd, object number)
        {
            switch (cmd)
            {
                case "USER":
                    USER(_client);
                    break;
                case "PASS":
                    PASS(_client);
                    break;
                case "STAT":
                    STAT(_client);
                    break;
                case "LIST":
                    if ((int)number > 0)
                    {
                        LISTn(_client, (int)number);
                    }
                    else
                    {
                        LIST(_client);
                    }
                    break;
                case "RETR":
                    if((int)number>0)
                        RETR(_client, (int)number);
                    else
                    {
                        SendRespondToClient(_client, "Command not understood." + CRLF);
                    }
                    break;
                case "DELE":
                    if((int)number > 0)
                    {
                        DELE(_client, (int)number);
                    }
                    else
                    {
                        SendRespondToClient(_client, "Command not understood." + CRLF);
                    }
                    break;
                case "RSET":
                    RSET(_client);
                    break;
                case "QUIT":
                    QUIT(_client);
                    break;
                case "NOOP":
                    NOOP(_client);
                    break;
                case "CRTU":
                    CRTU(_client, (string)number);
                    break;
            }
        }
        #endregion

        #region Commands 
        #region USER 
        /// <summary>
        /// USER + username command
        /// </summary>
        /// <param name="_client"></param>
        public void USER(Socket _client)
        {
            string respond = "";
            getUserName(message);
            respond = "+ OK send PASS" + CRLF;
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region PASS
        /// <summary>
        /// PASS + password command
        /// </summary>
        /// <param name="_client"></param>
        public void PASS(Socket _client)
        {
            string respond = "";
            getPassWord(message);

            if (CheckUser(user.Username, user.Password))
                respond = "+OK welcome!" + CRLF;
            else respond = "-ERR [AUTH]"+CRLF;
            SendRespondToClient(_client, respond);
            
        }
        #endregion

        #region QUIT
        /// <summary>
        /// QUIT . Close server 
        /// </summary>
        /// <param name="_client"></param>
        public void QUIT(Socket _client)
        {
            SQL.Quit(user.Username,user.IsRset);
            TraceFrom("IsReset:" +user.IsRset.ToString());
            foreach (var item in SQL.STTmailDele)
            {
                //string user = (string)item.Value;
                string stt = item;
                TraceFrom(stt);
            }
            string respond = "+OK bye bye" + CRLF;
            SendRespondToClient(_client, respond);
           
            _client.Shutdown(SocketShutdown.Both);
            _client.Close();

            isListen = false;
            server.Close();

            TraceFrom("Server is closed.");

        }
        #endregion

        #region STAT
        /// <summary>
        /// STAT
        /// </summary>
        /// <param name="_client"></param>
        public void STAT(Socket _client)
        {
            string[] s = SQL.GetStat(user.Username);
            //S có só mail với dung lượng 

            string respond = "+OK " + s[0] + " " + s[1] + CRLF;
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region RETR
        /// <summary>
        /// RETR
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="n"></param>
        public void RETR(Socket _client, int n)
        {
            int num = SQL.SumOfMail(user.Username);

            string respond = "";
            if (n > num)
            {
                respond = "-ERR Message number out of range." + CRLF;
            }
            else
            {
                //Trace("client : retr " + n.ToString());
                
                string[] s = SQL.getByteofoneMail(n, user.Username);
                respond += "+OK " + s[1] + " bytes" + CRLF;
                respond += "Message-ID: " + CRLF;

                ConnectSQL.Connect.ListMail mail = SQL.ReadMail(n, user.Username);
                respond += "Date: " + mail.Date + CRLF;
                respond += "From: " + mail.From + CRLF;
                //Giải mã ra thông tin người nhận 
                string To = SQL.Decrypt(mail.To, key);
                respond += "To: " + To + CRLF;
                respond += "Subject: " + mail.Subject + CRLF;
                respond += "Content: " + mail.Body + CRLF;
                respond += "." + CRLF;
            }
            
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region LIST
        /// <summary>
        /// LIST - argurment
        /// </summary>
        /// <param name="_client"></param>
        public void LISTn(Socket _client, int number)
        {
            string respond = "";
            int n = SQL.SumOfMail(user.Username);
            if (number > n)
            {
                respond = "-ERR Message number out of range. " + CRLF;
            }
            else
            {
                string[] results = { "0", "0" };
                try
                {
                    results = SQL.getByteofoneMail(number, user.Username);
                }
                catch (Exception ex)
                {
                    TraceFrom(ex.Message);
                }
                respond = "+OK " + results[0] + " " + results[1] + CRLF;
            }
            SendRespondToClient(_client, respond);
        }
        /// <summary>
        /// LIST - no argurment
        /// </summary>
        /// <param name="_client"></param>
        public void LIST(Socket _client)
        {
            string respond = "+OK ";
            Dictionary<string,string> result = new Dictionary<string, string>();
            result = SQL.LIST(user.Username);
            foreach(KeyValuePair<string, string> item in result)
            {
                respond += item.Key + " " + item.Value+ CRLF;
            }
            respond += "." + CRLF;
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region NOOP
        /// <summary>
        /// NOOP
        /// </summary>
        /// <param name="_client"></param>
        public void NOOP(Socket _client)
        {
            string respond = "+OK" + CRLF;
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region RSET
        /// <summary>
        /// RESET
        /// </summary>
        /// <param name="_client"></param>
        public void RSET(Socket _client)
        {
            
            SQL.reset(user.Username, user.Password);
            user.IsRset = true;
            int num = SQL.SumOfMail(user.Username);
            string respond = "+OK " + num.ToString() + " messages" + CRLF;
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region DELE
        public void DELE(Socket _client, int n )
        {
            
            string respond = "";
            int num = SQL.SumOfMail(user.Username);
            if(n>num)
            {
                respond = "-ERR Message number out of range." + CRLF;
            }
            else
            {
               if(SQL.Dele(n,user.Username) == false)
                {
                    respond = "-ERR Message "+ n.ToString()+" already deleted."+CRLF;
                }
               else
                {
                    respond = "+OK Message " + n.ToString()+ " deleted." + CRLF ;
                }
            }
            SendRespondToClient(_client, respond);
        }
        #endregion

        #region CRTU
        /// <summary>
        /// CRTU [user pass]
        /// </summary>
        /// <param name="_client"></param>
        /// <param name=""></param>
        public void CRTU(Socket _client,string UserPass)
        {
            
            UserPass = UserPass.Substring(1);
            int n = UserPass.IndexOf(" ");
            
            string user = UserPass.Substring(0, n);
            string pass = UserPass.Substring(n+1);

            string respond = "";
            if (CreateNewAccount(user, pass) == true)
            {
                respond = "+OK Your account is created." + CRLF;
            }
            else
            {
                respond = "-ERR the username has already existed." + CRLF;
            }

            SendRespondToClient(_client, respond);

        }
        #endregion

        #endregion


    }
}
