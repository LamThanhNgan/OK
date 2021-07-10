using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections;
using System.Security.Cryptography;

namespace ConnectSQL
{
    public class Connect
    {
        #region class - variaties
        protected SqlConnectionStringBuilder Connection;
        //List chứa các user đã đăng nhập và sử dụng pop3server
        public List<ClassUser> ListUser = new List<ClassUser> { };
        
        //class của từng user chứa username, pass, list mail (from,to,subject,body mail) của từng user
        public class ClassUser
        {
            public string Username;
            public string Password;
            public List<ListMail> Mail = new List<ListMail> { }; 
        };
        /// <summary>
        /// Mail 
        /// </summary>
        public class ListMail
        {
            public string stt;   // số thứ tự trong sql chính
            public string Date;
            public string From;
            public string To;
            public string Subject;
            public string Body;
        }
        private bool isAuth = false;
        #endregion

        #region connect
        /// <summary>
        /// connect function
        /// </summary>
        public Connect()
        {
            //Connect to SQL (database: ACCOUNT)
            string connectString = @"Data Source=DESKTOP-5EL48GO\SQLEXPRESS;Initial Catalog=ACCOUNT;Integrated Security=True";
            try
            {
                this.Connection = new SqlConnectionStringBuilder(connectString);
            }
            catch
            {
                MessageBox.Show("Connect failed");
            }
        }
        #endregion

        #region extract
        //Truy xuất dữ liệu trong SQL có parameters
        public DataTable ExcureQuery(string query, object[] parameters = null)
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(Connection.ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);

                //điều kiện là string chứa @
                if (parameters != null)
                {
                    string[] splitPara = query.Split(' ');
                    int i = 0;
                    foreach (string item in splitPara)
                    {
                        if (item.StartsWith('@'))
                        {
                            command.Parameters.AddWithValue(item, parameters[i]);
                            i++;
                        }
                    }
                }

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(dataTable);
                connection.Close();
            }

            return dataTable;
        }
        #endregion

        #region check account
        /// <summary>
        /// Kiểm tra pass có đúng với username
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool CheckAccount(string username, string password)
        {
            //khởi tạo check=false
            bool Check = false;
            //Lấy ra pass trong bảng LOGIN_ACCOUNT với username
            string query = @"SELECT Pass FROM LOGIN_ACCOUNT WHERE Username= @username ";
            DataTable data= this.ExcureQuery(query, new object[] { username });
            
            if (data.Rows.Count == 0) return false;
            string n = "";
            foreach (DataRow row in data.Rows)
            {
               n = row["Pass"].ToString();
            }
              
            
            if (n==password)
            {
                Check = true;
                isAuth = true;
                CreatClassUser(username, password);
            }    
            return Check;
        }
        #endregion

        public bool CheckUsername(string username)
        {
            //khởi tạo check=false
            bool Check = false;
            //Lấy ra pass trong bảng LOGIN_ACCOUNT với username
            string query = @"SELECT Username FROM LOGIN_ACCOUNT WHERE Username= @username ";    
            DataTable data = this.ExcureQuery(query, new object[] { username });

            if (data.Rows.Count == 0) return false;
            string n = "";
            foreach (DataRow row in data.Rows)
            {
                n = row["Username"].ToString();
            }
            if (n == username)
            {
                Check = true;
                isAuth = true;
            }
            return Check;
        }


        /// <summary>
        /// thêm vào ListUser một ClassUser chứa username,pass và list email của user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pass"></param>
        public void CreatClassUser(string username,string pass)
        {
            ClassUser User = new ClassUser();
            User.Username = username;
            User.Password = pass;
            string query = @"Select * from EMAIL where _To= @username ";
            DataTable data = this.ExcureQuery(query, new object[] { username });
            foreach (DataRow row in data.Rows)
            {
                ListMail temp = new ListMail();
                temp.stt = row["STT"].ToString();
                temp.Date = row["_Date"].ToString();
                temp.From = row["_From"].ToString();
                temp.To = row["_To"].ToString();
                temp.Subject = row["_Subject"].ToString();
                temp.Body = row["_Body"].ToString();
                User.Mail.Add(temp);
            }
            ListUser.Add(User);
        }
        //Kiểm tra Username có trong ListUser chưa
        public bool CheckClassUser(string Username)
        {
            bool result = false;
            foreach(var user in ListUser )
            {
                if (user.Username==Username)
                {
                    result = true;
                    break;
                }    
            }
            return result;
        }
        #region stat
        /// <summary>
        /// STAT  
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string[] GetStat(string username)
        {  
            string[] result = new string[2];
            byte[] a ;
            //lấy ra email với gửi nhận là username
            string query = @"select * from EMAIL where _To= @username ";
            DataTable data = this.ExcureQuery(query, new object[] { username });

            //stt= số hàng trong table email với _To = username
            int number = data.Rows.Count;
            double obtec = 0;
            // get bytes
            foreach(DataRow row in data.Rows)
            {
                a = System.Text.Encoding.ASCII.GetBytes(row.ToString());
                for(int i =0; i<a.Length-1; i++)
                {
                    obtec += a[i];
                }
            }
            //phần tử đầu là tổng số mail
            result[0] = number.ToString();
            result[1] = obtec.ToString();
            return result;
        }
        #endregion

        #region list
        //Nhận lại 1 list các email của username trong ListUser
        /// <summary>
        /// LIST trả về listmail trong class 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public List<ListMail> GetListEmail(string username)
        {
            var result = new ClassUser();
            foreach(var user in ListUser)
            {
                if (user.Username==username)
                {
                    result = user;
                }    
            }
            return result.Mail;
        }
        #endregion

        #region add user
        /// <summary>
        /// Tạo 1 tài khoản mới
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pass"></param>
        public void CreatNewUser(string username, string pass)
        {
            string query = @"INSERT INTO LOGIN_ACCOUNT VALUES ( @username , @pass )";
            this.ExcureQuery(query, new object[] { username, pass });
            CreatNewMessage(username);
            CreatClassUser(username, pass);
        }
        /// <summary>
        /// mail mặc định  Welcome to ....
        /// </summary>
        /// <param name="username"></param>
        public void CreatNewMessage(string username)
        {
            string time = DateTime.Now.ToString();
            string query = @"INSERT INTO EMAIL VALUES( @time , 'POP3server@Team11Server.nt106', @username , 'Welcome to the MyPOP3server', 'Welcome to MyPOP3server!  Your email administrator has created this POP3server email account for you')";
            this.ExcureQuery(query, new object[] { time , username });
        }
        #endregion

        public List<string> STTmailDele = new List<string>();
        /// <summary>
        /// DELE 1 mail trên mảng 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="user"></param>
        public bool Dele(int n,string user)
        {
            foreach (var item in STTmailDele)
            {
                if (item == n.ToString())
                {
                    return false;
                }
            }
            //Trong ListUser, tìm ClassUser tương ứng với user
            ClassUser C_User=new ClassUser();
            foreach(var i in ListUser)
            {
                if (i.Username==user)
                {
                    C_User = i;
                    break;
                }    
            }
            //stt bằng vị trí của mail trong sql
            string stt = C_User.Mail[n - 1].stt;


            STTmailDele.Add(stt.ToString());
            C_User.Mail.RemoveAt(n - 1);
            return true;
        }
        /// <summary>
        /// QUIT and dele mail server 
        /// </summary>
        public void Quit(string username , bool isReset = false)
        {
            //Trong ListUser, tìm ClassUser tương ứng với user
            ClassUser C_User = new ClassUser();
            foreach (var i in ListUser)
            {
                if (i.Username == username)
                {
                    C_User = i;
                    break;
                }
            }
            if (isReset == false)
            {
                foreach (var item in STTmailDele)
                {
                    string stt = item;
                    string query = @"Delete from EMAIL where STT= @stt ";  
                    this.ExcureQuery(query, new object[] { stt });
                }
            }  
        }
        /// <summary>
        /// reset
        /// </summary>
        /// <param name="username"></param>
        public void reset(string user , string pass)
        {
            int k = 0;
            foreach (var i in ListUser)
            {
                if (i.Username == user)
                {
                    k = ListUser.IndexOf(i);
                    //C_User = i;
                    break;
                }
            }

            ClassUser C_User2 = new ClassUser();
            C_User2.Username = user;
            C_User2.Password = pass;

            string query = @"Select * from EMAIL where _To= @username ";
            DataTable data = this.ExcureQuery(query, new object[] { user });
            foreach (DataRow row in data.Rows)
            {
                ListMail temp = new ListMail();
                temp.stt = row["STT"].ToString();
                temp.Date = row["_Date"].ToString();
                temp.From = row["_From"].ToString();
                temp.To = row["_To"].ToString();
                temp.Subject = row["_Subject"].ToString();
                temp.Body = row["_Body"].ToString();
                C_User2.Mail.Add(temp);
            }
            ListUser[k] = C_User2;
            STTmailDele = new List<string>();
        }

        #region đọc mail
        /// <summary>
        /// Đọc mail thứ n trong list
        /// return : a mail 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public ListMail ReadMail(int n,string user)
        {
            //Trong ListUser, tìm ClassUser tương ứng với user
            ClassUser C_User = new ClassUser();
            foreach (var i in ListUser)
            {
                if (i.Username == user)
                {
                    C_User = i;
                    break;
                }
            }
            return C_User.Mail[n-1];
        }
        /// <summary>
        /// List - n 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public string[] getByteofoneMail(int n , string username )
        {
            string[] result = new string[2];
            byte[] a;
            //lấy ra email với gửi nhận là username
            //Trong ListUser, tìm ClassUser tương ứng với user
            ClassUser C_User = new ClassUser();
            foreach (var i in ListUser)
            {
                if (i.Username == username)
                {
                    C_User = i;
                    break;
                }
            }
            //stt bằng vị trí của mail trong sql
            string stt = C_User.Mail[n - 1].stt;
            string query = @"Select * from EMAIL where STT= @stt ";
            DataTable data = this.ExcureQuery(query, new object[] { stt });
            
            double obtec = 0;
            // get bytes
            a = System.Text.Encoding.ASCII.GetBytes(data.Rows[0].ToString());
            for (int i = 0; i < a.Length - 1; i++)
            {
                obtec += a[i];
            }
            //phần tử đầu là tổng số mail
            result[0] = (n).ToString();
            result[1] = obtec.ToString();
            return result;
        }

        public Dictionary<string,string> LIST(string username)
        {
            // Tổng số mail trong mailbox
            string NumOfMail;
            string NumofByte;
            Dictionary<string,string> result = new Dictionary<string, string>();
            
            //lấy ra email với gửi nhận là username
            string query1 = @"select * from EMAIL where _To= @username ";
            DataTable data1 = this.ExcureQuery(query1, new object[] { username });

            //stt= số hàng trong table email với _To = username
            int number = data1.Rows.Count;
            //phần tử đầu là tổng số mail
            double obtec1 = 0;
            // get bytes
            byte[] a1;
            foreach (DataRow row in data1.Rows)
            {
                a1 = System.Text.Encoding.ASCII.GetBytes(row.ToString());
                for (int i = 0; i < a1.Length - 1; i++)
                {
                    obtec1 += a1[i];
                }
            }
            NumOfMail = number.ToString();
            NumofByte = obtec1.ToString();
            result.Add(NumOfMail + " messages", NumofByte + " bytes");
            
            
            //lấy ra email với gửi nhận là username
            //Trong ListUser, tìm ClassUser tương ứng với user
            ClassUser C_User = new ClassUser();
            foreach (var i in ListUser)
            {
                if (i.Username == username)
                {
                    C_User = i;
                    break;
                }
            }
            for (int i = 0; i < C_User.Mail.Count; i++)
            {
                //stt bằng vị trí của mail trong sql
                string stt = C_User.Mail[i].stt;
                string query = @"Select * from EMAIL where STT= @stt ";
                DataTable data = this.ExcureQuery(query, new object[] { stt });

                // get bytes
                double obtec = 0;
                // get bytes
                byte[] a;
                

                a = System.Text.Encoding.ASCII.GetBytes(data.Rows[0].ToString());
                for (int j = 0; j < a.Length - 1; j++)
                {
                    obtec += a[j];
                }
                result.Add((i+1).ToString(), obtec.ToString());
            }
            return result;
        }

       public int SumOfMail(string username)
        {
            // Tổng số mail trong mailbox
            
            //lấy ra email với gửi nhận là username
            string query1 = @"select * from EMAIL where _To= @username ";
            DataTable data1 = this.ExcureQuery(query1, new object[] { username });

            //stt= số hàng trong table email với _To = username
            int number = data1.Rows.Count;
            //phần tử đầu là tổng số mail

            return number;
        }
        #endregion
        #region MA HOA
        /// <summary>
        /// Mã hóa 3DES
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Encrypt(string source, string key)
        {
            //khai báo buến
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();
            byte[] byteHash;
            byte[] byteBuff;

            //Tính toán băm key
            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            //Sử dụng mode ECB
            desCryptoProvider.Mode = CipherMode.ECB;
            //chuyển source về dạng byte[] và gán vào byteBuff 
            byteBuff = Encoding.UTF8.GetBytes(source);

            //Mã hóa
            string encoded =
                Convert.ToBase64String(desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return encoded;
        }
        /// <summary>
        /// GIẢI MÃ #DÉ
        /// </summary>
        /// <param name="encodedText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Decrypt(string encodedText, string key)
        {
            //khai báo biến
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();
            byte[] byteHash;
            byte[] byteBuff;

            //tính toán băm key
            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            //sử dụng mode ECB
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB

            //mã hóa
            byteBuff = Convert.FromBase64String(encodedText);
            string plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }

        #endregion
    }

}


