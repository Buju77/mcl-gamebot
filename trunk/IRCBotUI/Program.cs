using System;
using System.Collections.Generic;
using System.Text;

namespace irc_bot_v2._0
{
    class Program
    {
        static void Main(string[] args)
        {
            IRCBot ircBot = new IRCBot();
            ircBot.StartBot();
            Console.WriteLine("Press any key to continue ...");
            Console.Read();
        }

        public static void Out(string message)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + ": " + Translator.Translate(message));
        }
    }
}
