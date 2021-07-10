
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;


namespace Pop3_1
{
    #region EMAILUI
    public struct EmailUid 
    {
    /// <summary>
    /// used in POP3 commands to indicate which message (only valid in the present session)
    /// </summary>
    public int EmailId;
    /// <summary>
    /// Uid is always the same for a message, regardless of session
    /// </summary>
    public string Uid;
    
  }
    #endregion

    #region SESSION
    /// <summary>
    /// A pop 3 connection goes through the following states:
    /// </summary>
    public enum Pop3ConnectionStateEnum {
    /// <summary>
    /// undefined
    /// </summary>
    None=0,
    /// <summary>
    /// not connected yet to POP3 server
    /// </summary>
    Disconnected,
    /// <summary>
    /// TCP connection has been opened and the POP3 server has sent the greeting. POP3 server expects user name and password
    /// </summary>
    Authorization,
    /// <summary>
    /// client has identified itself successfully with the POP3, server has locked all messages 
    /// </summary>
    Connected,
    /// <summary>
    /// QUIT command was sent, the server has deleted messages marked for deletion and released the resources
    /// </summary>
    Closed
  }
    #endregion

    #region DELEGATES
    /// <summary>
    /// định dạng thông tin giữa client,server
    /// </summary>
    /// <param name="TraceText"></param>
    public delegate void TraceHandler(string TraceText);
    #endregion

    #region POP3 
    public class POP3 
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
            if (Trace!=null) 
            {
            Trace(DateTime.Now.ToString("hh:mm:ss ") +" "+ string.Format(text, parameters) + CRLF);
            }
        }

        /// <summary>
        /// Trace information received from POP3 server
        /// </summary>
        /// <param name="text">string to be traced</param>
        /// <param name="parameters"></param>
        protected void TraceFrom(string text, params object[] parameters) 
        {
            if (Trace!=null) 
            {
                CallTrace("   " + string.Format(text, parameters) + CRLF);
            }
        }
        #endregion
        
        #region BIEN
        /// <summary>
        /// POP3 server name
        /// </summary>
        protected string popServer;
        /// <summary>
        /// POP3 server port
        /// </summary>
        protected int port;

        //private bool useSSL;

        #endregion

        #region TIMEOUT
        //private bool isAutoReconnect = false;
        //timeout has occured, we try to perform an autoreconnect
        private bool isTimeoutReconnect = false;

        
        /// <summary>
        /// Get / set read timeout (miliseconds)
        /// </summary> 

        protected int readTimeout = -1;
        public int ReadTimeout 
        {
            get { return readTimeout; }
            set 
            {
                readTimeout = value;
                if (pop3Stream!=null && pop3Stream.CanTimeout) 
                {
                    pop3Stream.ReadTimeout = readTimeout;
                }
            }
        }
        #endregion

        #region USERNAME
        /// <summary>
        /// Owner name of mailbox on POP3 server
        /// </summary>
        protected string username;
        #endregion

        #region PASSWORD
        
        /// <summary>
        /// Password for mailbox on POP3 server
        /// </summary>
        protected string password;
        #endregion

        #region GET STATE
        /// <summary>
        /// Get connection status with POP3 server
        /// </summary>
        public Pop3ConnectionStateEnum Pop3ConnectionState 
        {
            get { return pop3ConnectionState; }
        }
        /// <summary>
        /// connection status with POP3 server khởi tạo 
        /// </summary>
        /// 
        #endregion

        #region SET STATE
        protected Pop3ConnectionStateEnum pop3ConnectionState = Pop3ConnectionStateEnum.Disconnected;
        /// <summary>
        /// set POP3 connection state
        /// </summary>
        /// <param name="State"></param>
        protected void setPop3ConnectionState(Pop3ConnectionStateEnum State) 
        {
            pop3ConnectionState = State;
            CallTrace("   Pop3MailClient Connection State {0} reached", State);
        }
        #endregion

        #region ensur
        /// <summary>
        /// throw exception if POP3 connection is not in the required state
        /// </summary>
        /// <param name="requiredState"></param>
        protected void EnsureState(Pop3ConnectionStateEnum requiredState) {
      if (pop3ConnectionState!=requiredState) {
                string t = "GetMailboxStats only accepted during connection state: {0} \n The connection to server" + popServer+ " is in state " + pop3ConnectionState.ToString();
                CallTrace(t, requiredState.ToString());
            }
        }
        #endregion

        #region private fields
        //--------------
        /// <summary>
        /// TCP to POP3 server
        /// </summary>
        private TcpClient serverTcpConnection;
        /// <summary>
        /// Stream from POP3 server with or without SSL
        /// </summary>
        private Stream pop3Stream;
        /// <summary>
        /// Reader for POP3 message
        /// </summary>
        protected StreamReader pop3StreamReader;
        /// <summary>
        /// char 'array' for carriage return / line feed
        /// </summary>
        protected string CRLF = "\r\n";

        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Make POP3 client ready to connect to POP3 server
        /// </summary>
        /// <param name="PopServer"><example>pop.gmail.com</example></param>
        /// <param name="Port"><example>995</example></param>
        /// <param name="useSSL">True: SSL is used for connection to POP3 server</param>
        /// <param name="Username"><example>abc@gmail.com</example></param>
        /// <param name="Password">Secret</param>
        public POP3(string PopServer, int Port, bool useSSL, string Username, string Password) 
        {
            this.popServer = PopServer;
            this.port = Port; 
            this.username = Username;
            this.password = Password;
            this.useSSL = useSSL;
        }
        #endregion

        #region CONNECT
        /// <summary>
        /// Connect to POP3 server
        /// </summary>
        
        public void Connect()
        {
            if (pop3ConnectionState != Pop3ConnectionStateEnum.Disconnected && pop3ConnectionState != Pop3ConnectionStateEnum.Closed && !isTimeoutReconnect)
            {
                CallTrace("Connect command received, but connection state is: ", pop3ConnectionState.ToString());
            }
            else
            {
                //establish TCP connection
                try
                {
                    CallTrace("   Connect at port {0}", port);
                    serverTcpConnection = new TcpClient(popServer, port);
                }
                catch (Exception ex)
                {
                    CallTrace("Connection to server failed.\nRuntime Error: ", ex.ToString());
                }
                // SSL
                if (useSSL)   //port 995
                {
                    //get SSL stream
                    try
                    {
                        CallTrace("   Get SSL connection");
                        pop3Stream = new SslStream(serverTcpConnection.GetStream(), false);
                        pop3Stream.ReadTimeout = readTimeout;
                    }
                    catch (Exception ex)
                    {
                        string t = "Server " + popServer + " found, but cannot get SSL data stream.\nRuntime Error: ";
                        CallTrace(t, ex.ToString());
                    }

                    //perform SSL authentication
                    try
                    {
                        CallTrace("   Get SSL authentication");
                        ((SslStream)pop3Stream).AuthenticateAsClient(popServer);
                    }
                    catch (Exception ex)
                    {
                        string t = "Server " + popServer + " found, but problem with SSL Authentication.\nRuntime Error: ";
                        CallTrace(t, ex.ToString());
                    }
                }
                else  //not SSL  port 110 
                {
                    //create a stream to POP3 server without using SSL
                    try
                    {
                        CallTrace("   Get connection without SSL");
                        pop3Stream = serverTcpConnection.GetStream();
                        pop3Stream.ReadTimeout = readTimeout;
                    }
                    catch (Exception ex)
                    {
                        string t = "Server " + popServer + " found, but cannot get data stream (without SSL).\nRuntime Error: ";
                        CallTrace(t, ex);
                    }
                }
                //get stream for reading from pop server
                //POP3 allows only US-ASCII. The message will be translated in the proper encoding in a later step
                try
                {
                    pop3StreamReader = new StreamReader(pop3Stream, Encoding.ASCII);
                }
                catch (Exception ex)
                {
                    if (useSSL)
                    {
                        string t = "Server " + popServer + " found, but cannot read from SSL stream.\nRuntime Error: ";
                        CallTrace(t, ex);
                    }
                    else
                    {
                        string t = "Server " + popServer + " found, but cannot read from stream (without SSL).\nRuntime Error: ";
                        CallTrace(t, ex);
                    }
                }

                //ready for authorisation
                string response;
                if (!readSingleLine(out response))
                {
                    string t = "Server " + popServer + " not ready to start AUTHORIZATION.\nMessage: ";
                    CallTrace(t, response);
                }

                setPop3ConnectionState(Pop3ConnectionStateEnum.Authorization);

                //send user name
                if (!executeCommand("USER " + username, out response))
                {
                    string t = "Server " + popServer + " doesn't accept username '" + username + "'.\nMessage: ";
                    CallTrace(t, response);
                }

                //send password
                if (!executeCommand("PASS " + password, out response))
                {
                    string t = "Server " + popServer + " doesn't accept password '" + password + "' for user '" + username + "'.\nMessage: ";
                    CallTrace(t, response);
                }

                setPop3ConnectionState(Pop3ConnectionStateEnum.Connected);
            }
        }
        #endregion

        #region DISCONNECT
        /// <summary>
        /// Disconnect from POP3 Server
        /// </summary>
        public void Disconnect() 
        {
            if (pop3ConnectionState==Pop3ConnectionStateEnum.Disconnected || pop3ConnectionState==Pop3ConnectionStateEnum.Closed) 
            {
                TraceFrom(Pop3ConnectionState.ToString(), "Disconnect received, but was already disconnected."+CRLF);
            } 
            else 
            {
                //ask server to end session and possibly to remove emails marked for deletion
            try 
             {
                string response;
                if (executeCommand("QUIT", out response)) 
                {
                    //server says everything is ok
                    setPop3ConnectionState(Pop3ConnectionStateEnum.Closed);
                } 
                else 
                {
                    //server says there is a problem             
                    setPop3ConnectionState(Pop3ConnectionStateEnum.Disconnected);
                }
            } 
            finally 
            {
                //close connection
                if (pop3Stream!=null) 
                {
                    pop3Stream.Close();
                }
                pop3StreamReader.Close();
            }
            }
        }
        #endregion

        #region STAT
        /// <summary>
        /// Get mailbox statistics
        /// </summary>
        /// <param name="NumberOfMails"></param>
        /// <param name="MailboxSize"></param>
        /// <returns></returns>
        public bool GetMailboxStats(out int NumberOfMails, out int MailboxSize)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            //interpret response
            string response;
            NumberOfMails = 0;
            MailboxSize = 0;
            if (executeCommand("STAT", out response))
            {
                //got a positive response
                string[] responseParts = response.Split(' ');
                if (responseParts.Length < 2)
                {
                    //response format wrong
                    string t = "Server " + popServer + " sends illegally formatted response." + "\nExpected format: +OK int int" + "\nReceived response: ";
                    CallTrace(t, response);
                }
                NumberOfMails = int.Parse(responseParts[1]);
                MailboxSize = int.Parse(responseParts[2]);
                return true;
            }
            return false;
        }
        #endregion

        #region RETR
        /// <summary>
        /// Send RETR command to POP 3 server to fetch one particular message
        /// </summary>
        /// <param name="MessageNo">ID of message required</param>
        /// <returns>false: negative server respond, message not delivered</returns>
        protected bool SendRetrCommand(int MessageNo)
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            // retrieve mail with message number
            string response;
            if (!executeCommand("RETR " + MessageNo.ToString(), out response))
            {
                CallTrace("GetRawEmail: negative response for email (ID: {0}) request", MessageNo.ToString());
                return false;
            }
            return true;
        }
        #endregion

        #region DOCMAIL 
        /// <summary>
        /// contains one MIME part of the email in US-ASCII, needs to be translated in .NET string (Unicode)
        /// contains the complete email in US-ASCII, needs to be translated in .NET string (Unicode)
        /// For speed reasons, reuse StringBuilder
        /// </summary>
        protected StringBuilder RawEmailSB;


        /// <summary>
        /// Reads the complete text of a message
        /// </summary>
        /// <param name="MessageNo">Email to retrieve</param>
        /// <param name="EmailText">ASCII string of complete message</param>
        /// <returns></returns>
        public bool GetRawEmail(int MessageNo, out string EmailText)
        {
            //send 'RETR int' command to server
            if (!SendRetrCommand(MessageNo))
            {
                EmailText = null;
                return false;
            }

            //get the lines
            string response;
            int LineCounter = 0;
            //empty StringBuilder
            if (RawEmailSB == null)
            {
                RawEmailSB = new StringBuilder(100000);
            }
            else
            {
                RawEmailSB.Length = 0;
            }
            isTraceRawEmail = true;
            while (readMultiLine(out response))
            {
                LineCounter += 1;
            }
            EmailText = RawEmailSB.ToString();
            TraceFrom("email with {0} lines,  {1} chars received", LineCounter.ToString(), EmailText.Length);
            return true;
        }
        #endregion

        #region DELE 1 mail
        /// <summary>
        /// Delete message from server.
        /// The POP3 server marks the message as deleted.  Any future
        /// reference to the message-number associated with the message
        /// in a POP3 command generates an error.  The POP3 server does
        /// not actually delete the message until the POP3 session
        /// enters the UPDATE state.
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public bool DeleteEmail(int msg_number) 
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            string response;
            if (!executeCommand("DELE " + msg_number.ToString(), out response)) 
            {
                string t = "DeleteEmail " + response + " negative response for email (Id: {0}) delete request";
                CallTrace(t, msg_number);
                return false;
            }
            return true;
        }
        #endregion

        #region get id mail list
        /// <summary>
        /// Get a list of all Email IDs available in mailbox
        /// </summary>
        /// <returns></returns>
        public bool GetEmailIdList(out List<int> EmailIds) 
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            EmailIds = new List<int>();
            //get server response status line
            string response;
            if (!executeCommand("LIST", out response)) 
            {
                CallTrace("GetEmailIdList {0} negative response for email list request", response);
                return false;
            }
            //get every email id
            int EmailId;
            while (readMultiLine(out response)) 
            {
                if (int.TryParse(response.Split(' ')[0], out EmailId)) 
                {
                    EmailIds.Add(EmailId);
                } 
                else CallTrace("GetEmailIdList {0} first characters should be integer (EmailId)",response);
            }
            TraceFrom("{0} email ids received", EmailIds.Count);
            return true;
        }

        #endregion

        #region get size of mail 
        /// <summary>
        /// get size of one particular email
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public int GetEmailSize(int msg_number) 
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            string response;
            executeCommand("LIST " + msg_number.ToString(), out response);
            int EmailSize = 0;
            string[] responseSplit = response.Split(' ');
            if (responseSplit.Length<2 || !int.TryParse(responseSplit[2], out EmailSize)) 
            {
                CallTrace("GetEmailSize : {0} '+OK int int' format expected (EmailId, EmailSize)",response);
            }
        return EmailSize;
        }
        #endregion

        #region undele all maill
        /// <summary>
        /// Unmark any emails from deletion. The server only deletes email really
        /// once the connection is properly closed.
        /// </summary>
        /// <returns>true: emails are unmarked from deletion</returns>
        public bool UndeleteAllEmails()
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            string response;
            return executeCommand("RSET", out response);
        }

        #endregion

        #region NOOP
        /// <summary>
        /// Sends an 'empty' command to the POP3 server. Server has to respond with +OK
        /// </summary>
        /// <returns>true: server responds as expected</returns>
        public bool NOOP()
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            string response;
            if (!executeCommand("NOOP", out response))
            {
                CallTrace("NOOP {0} negative response for NOOP request", response);
                return false;
            }
            return true;
        }
        #endregion

        #region UIDL
        /// <summary>
        /// get a list with all currently available messages and the UIDs
        /// </summary>
        /// <param name="EmailIds">EmailId Uid list</param>
        /// <returns>false: server sent negative response (didn't send list)</returns>
        public bool GetUniqueEmailIdList(out SortedList<string, int> EmailIds) 
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            EmailIds = new SortedList<string, int>();

            //get server response status line
            string response;
            if (!executeCommand("UIDL", out response)) 
            {
                CallTrace("GetUniqueEmailIdList {0} negative response for email list request",response);
                return false;
            }

            //get every email unique id
            int EmailId;
            while (readMultiLine(out response)) 
            {
                string[] responseSplit = response.Split(' ');
                if (responseSplit.Length<2) 
                {
                    CallTrace("GetUniqueEmailIdList {0} response not in format 'int string'", response);
                } 
                else if (!int.TryParse(responseSplit[0], out EmailId)) 
                {
                    CallTrace("GetUniqueEmailIdList {0} first charaters should be integer (Unique EmailId)",response);
                } 
                else 
                {
                    EmailIds.Add(responseSplit[1], EmailId);
                }
            }
            TraceFrom("{0} unique email ids received", EmailIds.Count);
            return true;
         }
        #endregion

        #region get the size of 1 mail
        /// <summary>
        /// get size of one particular email
        /// </summary>
        /// <param name="msg_number"></param>
        /// <returns></returns>
        public int GetUniqueEmailId(EmailUid msg_number) 
        {
            EnsureState(Pop3ConnectionStateEnum.Connected);
            string response;
            executeCommand("LIST " + msg_number.ToString(), out response);
            int EmailSize = 0;
            string[] responseSplit = response.Split(' ');
            if (responseSplit.Length<2 || !int.TryParse(responseSplit[2], out EmailSize)) {
                CallTrace("GetEmailSize {0} '+OK int int' format expected (EmailId, EmailSize)", response);
            }
            return EmailSize;
         }
        #endregion

        #region EXEC
        public bool isDebug = false;
        private bool executeCommand(string command, out string response)
        {
            //send command to server
            byte[] commandBytes = System.Text.Encoding.ASCII.GetBytes((command + CRLF).ToCharArray());
            CallTrace("Client: '{0}'", command);
            bool isSupressThrow = false;
            try 
            {
                pop3Stream.Write(commandBytes, 0, commandBytes.Length);
                if (isDebug) 
                {
                    isDebug=false;
                    throw new IOException("Test", new SocketException(10053));
                }
            } 
            catch (IOException ex) 
            {
                //Unable to write data to the transport connection. Check if reconnection should be tried
                isSupressThrow = executeReconnect(ex, command, commandBytes);
                if (!isSupressThrow) 
                {
                    throw;
                }
            }
            pop3Stream.Flush();

            //read response from server
            response = null;
            try 
            {
                response = pop3StreamReader.ReadLine();
            } 
            catch (IOException ex) 
            {
                 //Unable to write data to the transport connection. Check if reconnection should be tried
                isSupressThrow = executeReconnect(ex, command, commandBytes);
                if (isSupressThrow) 
                {
                    //wait for response one more time
                    response = pop3StreamReader.ReadLine();
                } 
                else 
                {
                    throw;
                }
            }
            if (response==null) {
                CallTrace("Server '{0}' has not responded, timeout has occured.",popServer);
                return false;
            }
            CallTrace("Server: '{0}'", response);
            return (response.Length>0 && response[0]=='+');
        }
        #endregion

        #region RECONNECT
        /// <summary>
        /// reconnect, if there is a timeout exception and isAutoReconnect is true
        /// 
        /// </summary>
        private bool executeReconnect(IOException ex, string command, byte[] commandBytes) 
        {
            if (ex.InnerException!=null && ex.InnerException is SocketException) 
            {
                //SocketException
                SocketException innerEx = (SocketException)ex.InnerException;
                if (innerEx.ErrorCode==10053) 
                {
                    //probably timeout: An established connection was aborted by the software in your host machine.
                    CallTrace("ExecuteCommand", "probably timeout occured");
                }
            }
            return false;
        }
        #endregion

        #region READ 1
        /// <summary>
        /// read single line response from POP3 server. 
        /// <example>Example server response: +OK asdfkjahsf</example>
        /// </summary>
        /// <param name="response">response from POP3 server</param>
        /// <returns>true: positive response</returns>
        protected bool readSingleLine(out string response)
        {
            response = null;
            try
            {
                response = pop3StreamReader.ReadLine();
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            if (response == null)
            {
                CallTrace("Server {0} has not responded, timeout has occured.", popServer);
                return false;
            }
            CallTrace("Rx '{0}'", response);
            return (response.Length > 0 && response[0] == '+');
        }
        #endregion

        #region read nhieu line
        /// <summary>
        /// read one line in multiline mode from the POP3 server. 
        /// </summary>
        /// <param name="response">line received</param>
        /// <returns>false: end of message</returns>
        /// <returns></returns>
        protected bool readMultiLine(out string response) 
        {
            response = null;
            response = pop3StreamReader.ReadLine();
            if (response==null) 
            {
                CallTrace("Server {0} has not responded, probably timeout has occured.", response);
            }
            if (isTraceRawEmail) 
            {
                //collect all responses as received
                RawEmailSB.Append(response + CRLF);
            }
            //check for byte stuffing, i.e. if a line starts with a '.', another '.' is added, unless
            //it is the last line
            if (response.Length>0 && response[0]=='.') 
            {
                if (response==".") 
                {
                //closing line found
                return false;
                }
            //remove the first '.'
            response = response.Substring(1, response.Length-1);
            }
            return true;
        }

        #endregion

        #region FORM

        /// <summary>
        /// show the mail
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Header"></param>
        /// <returns></returns>
        ////GetValueFromHeader
        public string GetValueFromHeader(string Data,string Header)
        {
            string result = "";
            int Start = Data.IndexOf(Header);//start là vị trí xuất hiện từ header trong mảng data
            if (Start < 0)
            {
                return "";
            }
            Start += Header.Length;
            int cend = Data.IndexOf("\r\n", Start);
            if (cend<=0)
            {
                return "";
            }
            result = Data.Substring(Start , cend - Start).Trim();
            return result;
        }
        public string GetBodyMail(string Data)
        {
            string result = "";
            string boundary = "--"+GetValueFromHeader(Data, "boundary=");
            boundary= boundary.Replace("\"","");

            int Start = Data.IndexOf("Content-Type: text");
            if (Start<0)
            {
                return "";
            }
            if (Data.Contains("Content-Transfer")==true)
            {
                Start = Data.IndexOf("Content-Transfer");
            }
            Start = Data.IndexOf("\r\n", Start);
            int cent = Data.IndexOf("Content-Type: text",Start);
            result = Data.Substring(Start, cent - Start).Trim();
            result = result.Replace(boundary,"");
            return result;
        }
        #endregion

        #region   ...
        /// <summary>
        /// Should the raw content, the US-ASCII code as received, be traced
        /// GetRawEmail will switch it on when it starts and off once finished
        /// 
        /// Inheritors might use it to get the raw email
        /// </summary>
        protected bool isTraceRawEmail = false;
        private bool useSSL;
        #endregion
    }
    #endregion
}






