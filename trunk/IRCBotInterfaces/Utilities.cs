using System;
using System.Collections.Generic;
using System.Text;

namespace IRCBotInterfaces
{
    public class Utilities
    {
        public enum UserAction { Joined, Part, Quit };

        public delegate void UserActionHandler(object sender, UserAction action, EventArgs e);

        #region static functions
        public static string BuildPrivMsg(string receiver, string message)
        {
            return "PRIVMSG " + receiver + " :" + message;
        }

        public static string BuildNotice(string receiver, string message)
        {
            return "NOTICE " + receiver + " :" + message;
        }

        public static string BuildActionMessage(string receiver, string message)
        {
            string actionMsg = String.Format("{0}ACTION {1}{0}", (char)1, message);
            return Utilities.BuildPrivMsg(receiver, actionMsg);
        }

        public static void Out(ICommand caller, string message)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + ": " + caller.ID + " > " + message);
        }
        #endregion
    }
}
