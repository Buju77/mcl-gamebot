using System;
using System.Collections.Generic;
using System.Text;

#if PocketPC
using System.Windows.Forms;
#endif

namespace irc_bot_v2._0
{
    class Program
    {
#if PocketPC
        private static MainForm form;
#endif

        static void Main(string[] args)
        {
#if PocketPC
            form = new MainForm();
#endif
            IRCBot ircBot = new IRCBot();
            ircBot.StartBot();
#if !PocketPC
            Console.WriteLine("Press any key to continue ...");
            Console.Read();
#else
            form.IrcBot = ircBot;
            Application.Run(form);
#endif
        }

        public static void Out(string message)
        {
#if PocketPC
            form.AddOutput(message);
#else
            Console.WriteLine(DateTime.Now.ToLongTimeString() + ": " + Translator.Translate(message));
#endif
        }
    }
}
