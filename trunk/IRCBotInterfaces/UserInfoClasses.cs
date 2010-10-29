using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace IRCBotInterfaces
{
    public enum UserAction
    {
        Joined,
        Part,
        Quit
    };

    public delegate void UserActionEventHandler(object sender, UserActionEventArgs uaea);

    public class UserActionEventArgs
    {
        public UserAction UserAction
        {
            get;
            private set;
        }

        public string Nickname
        {
            get;
            private set;
        }

        public string Username
        {
            get;
            private set;
        }

        public UserActionEventArgs(UserAction ua, string nick, string user)
        {
            this.UserAction = ua;
            this.Nickname = nick;
            this.Username = user;
        }
    }
}
