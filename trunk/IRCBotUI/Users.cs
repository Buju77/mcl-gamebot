using System;
using System.Collections.Generic;
using System.Text;

namespace irc_bot_v2._0
{
    class Users
    {
        public enum UserMode { Normal = 0, Voice = 2, OP = 4 };

        private class UserInfo
        {
            #region string Name
            private string ivName;
            public string Name
            {
                get { return ivName; }
                //set { ivName = value; }
            }
            #endregion

            #region List<string> IPs
            private List<string> ivIPs;
            public List<string> IPs
            {
                get { return ivIPs; }
                //set { ivIP = value; }
            }
            #endregion

            #region List<string> Nicknames
            private List<string> ivNicknames;
            public List<string> Nicknames
            {
                get { return ivNicknames; }
                //set { ivNicknames = value; }
            } 
            #endregion

            #region int Auth
            private int ivAuth;
            public int Auth
            {
                get { return ivAuth; }
                set { ivAuth = value; }
            } 
            #endregion

            #region UserMode Mode
            private UserMode ivMode;
            public UserMode Mode
            {
                get { return ivMode; }
                set { ivMode = value; }
            } 
            #endregion

            #region bool Ignored
            private bool ivIgnored;
            public bool Ignored
            {
                get { return ivIgnored; }
                set { ivIgnored = value; }
            } 
            #endregion

            #region string LastMessage
            private string ivLastMessage;
            public string LastMessage
            {
                get { return ivLastMessage; }
                set { ivLastMessage = value; }
            } 
            #endregion
            
            public UserInfo(string nick, string name, string ip)
            {
                this.ivName = name;
                this.ivIPs = new List<string>();
                this.ivIPs.Add(ip);
                this.ivNicknames = new List<string>();
                this.ivNicknames.Add(nick);
                this.ivAuth = 0;
                this.ivMode = UserMode.Normal;
                this.ivIgnored = false;
            }

            public UserInfo(string name, List<string> values)
            {
                this.ivName = name;
                this.ivAuth = Convert.ToInt32(values[0]);
                this.ivIgnored = Boolean.TrueString.Equals(values[1]);
                this.ivMode = (UserMode)Convert.ToInt32(values[2]);
                this.ivLastMessage = values[3];

                string[] ips = values[4].Split('@');
                this.ivIPs = new List<string>(ips);
                for (int i = 0; i < this.ivIPs.Count; i++)
                {
                    if (String.IsNullOrEmpty(this.ivIPs[i])) { this.ivIPs.RemoveAt(i); }
                }

                string[] nicks = values[5].Split('@');
                this.ivNicknames = new List<string>(nicks);
                for (int i = 0; i < this.ivNicknames.Count; i++)
                {
                    if (String.IsNullOrEmpty(this.ivNicknames[i])) { this.ivNicknames.RemoveAt(i); }
                }
            }

            public void AddNick(string nick)
            {
                if (!this.ivNicknames.Contains(nick)) { this.ivNicknames.Add(nick); }
            }

            public void AddIP(string ip)
            {
                if (!this.ivIPs.Contains(ip)) { this.ivIPs.Add(ip); }
            }

            public List<string> DumpText()
            {
                List<string> result = new List<string>();
                result.Add(this.ivName);
                result.Add(this.ivAuth.ToString());
                result.Add(this.ivIgnored.ToString());
                result.Add(((int)this.ivMode).ToString());
                result.Add(this.ivLastMessage);
                string ips = string.Empty;
                foreach (string ip in this.ivIPs)
                {
                    ips += ip + "@";
                }
                result.Add(ips);
                string nicks = string.Empty;
                foreach (string nick in this.ivNicknames)
                {
                    nicks += nick + "@";
                }
                result.Add(nicks);

                return result;
            }
        }

        private Dictionary<string, UserInfo> ivUsers;

        public Users()
        {
            this.ivUsers = new Dictionary<string, UserInfo>();

            UserInfo ui = new UserInfo("motivation", "motivation", "127.0.0.1");
            this.ivUsers.Add("motivation", ui);
        }

        public void LoadUsersFromFile(string filePath)
        {
            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(filePath, out fileSuccess, out fileErrorMsg);
            if (fileSuccess)
            {
                List<string> fileText = file.GetAllValues();

                this.ivUsers = new Dictionary<string, UserInfo>();
                for (int i = 0; i < fileText.Count; i += 8)
                {
                    List<string> tmp = new List<string>(7);
                    tmp.Add(fileText[i + 2]);
                    tmp.Add(fileText[i + 3]);
                    tmp.Add(fileText[i + 4]);
                    tmp.Add(fileText[i + 5]);
                    tmp.Add(fileText[i + 6]);
                    tmp.Add(fileText[i + 7]);
                    UserInfo ui = new UserInfo(fileText[i + 1], tmp);
                    this.ivUsers.Add(fileText[i + 1], ui);
                }
            }
            else
            {
                Program.Out("USERINFO: loading of file failed: " + fileErrorMsg);
            }
        }

        public void SaveUsersToFile(string filePath)
        {
            List<string> fileText = new List<string>();

            foreach (KeyValuePair<string, UserInfo> kvPair in this.ivUsers)
            {
                fileText.Add("### user: " + kvPair.Key + " ###");
                fileText.AddRange(kvPair.Value.DumpText());
            }

            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(filePath, out fileSuccess, out fileErrorMsg);
            if (!fileSuccess || !file.SetAllValues(fileText, out fileErrorMsg))
            {
                Program.Out("USERINFO: could not save all users: " + fileErrorMsg);
            }
        }

        public void SeenUser(string nick, string username, string ip, string message)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].LastMessage = message;
                if (!this.ivUsers[username].IPs.Contains(ip))
                {
                    this.ivUsers[username].AddIP(ip);
                }
                if (!this.ivUsers[username].Nicknames.Contains(nick))
                {
                    this.ivUsers[username].AddNick(nick);
                }
            }
            else
            {
                UserInfo ui = new UserInfo(nick, username, ip);
                ui.LastMessage = message;
                this.ivUsers.Add(username, ui);
            }
        }

        public int GetUserAuth(string username)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                return this.ivUsers[username].Auth;
            }
            else
            {
                return -1;
            }
        }

        public bool SetUserAuth(string username, int newAuth)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].Auth = newAuth;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetUserIgnored(string username)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                return this.ivUsers[username].Ignored;
            }
            else
            {
                return false;
            }
        }

        public string GetUsernameForNick(string usernick)
        {
            foreach (UserInfo user in this.ivUsers.Values)
            {
                foreach (string nick in user.Nicknames)
                {
                    if (nick.ToLower().Equals(usernick.ToLower()))
                    {
                        return nick;
                    }
                }
            }
            return string.Empty;
        }

        public string GetUserLastSeen(string usernick)
        {
            Dictionary<DateTime, string> lastMsgs = new Dictionary<DateTime, string>();
            foreach (UserInfo user in this.ivUsers.Values)
            {
                foreach (string nick in user.Nicknames)
                {
                    if (nick.ToLower().Equals(usernick.ToLower()))
                    {
                        int timeEnd = user.LastMessage.IndexOf("@");
                        if (timeEnd < 0)
                        {
                            continue;
                        }

                        string time = user.LastMessage.Substring(0, timeEnd);
                        DateTime timeDT;
                        DateTime.TryParse(time, out timeDT);
                        lastMsgs[timeDT] = user.LastMessage;
                    }
                }
            }

            DateTime resultDT = DateTime.MinValue;
            string resultMsg = string.Empty;
            foreach (KeyValuePair<DateTime, string> msg in lastMsgs)
            {
                if (resultDT < msg.Key)
                {
                    resultDT = msg.Key;
                    resultMsg = msg.Value;
                }
            }
            return resultMsg;
        }

        public void UserJoined(string nick, string username, string ip)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                if (!this.ivUsers[username].IPs.Contains(ip))
                {
                    this.ivUsers[username].AddIP(ip);
                }
                if (!this.ivUsers[username].Nicknames.Contains(nick))
                {
                    this.ivUsers[username].AddNick(nick);
                }
            }
            else
            {
                UserInfo ui = new UserInfo(nick, username, ip);
                this.ivUsers.Add(username, ui);
            }
        }

        public void UserModeChanged(string username, UserMode newmode)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].Mode = newmode;
            }
        }

        public void UserAuthChanged(string nick, string username, string ip, int userauth)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].Auth = userauth;
                if (!this.ivUsers[username].IPs.Contains(ip))
                {
                    this.ivUsers[username].AddIP(ip);
                }
                if (!this.ivUsers[username].Nicknames.Contains(nick))
                {
                    this.ivUsers[username].AddNick(nick);
                }
            }
            else
            {
                UserInfo ui = new UserInfo(nick, username, ip);
                ui.Auth = userauth;
                this.ivUsers.Add(username, ui);
            }
        }

        public void UserIgnoreChanged(string username, bool ignored)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].Ignored = ignored;
            }
        }

        public void UserIgnoreChanged(string nick, string username, string ip, bool ignored)
        {
            if (username.StartsWith("~")) { username = username.Substring(1); }

            if (this.ivUsers.ContainsKey(username))
            {
                this.ivUsers[username].Ignored = ignored;
                if (!this.ivUsers[username].IPs.Contains(ip))
                {
                    this.ivUsers[username].AddIP(ip);
                }
                if (!this.ivUsers[username].Nicknames.Contains(nick))
                {
                    this.ivUsers[username].AddNick(nick);
                }
            }
            else
            {
                UserInfo ui = new UserInfo(nick, username, ip);
                ui.Ignored = ignored;
                this.ivUsers.Add(username, ui);
            }
        }
    }
}
