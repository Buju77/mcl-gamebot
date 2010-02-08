/**
 * Author:  Lin Yen-Chia
 * Created: 2007-06-28
 * Version: 1.0
 * 
 * Change Log:
 * 
 * Version 1.0: Initial Release
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using IRCBotInterfaces;

namespace InsultPlugin {
    public class Insult : ICommand {
        #region ICommand Members

        private List<String> keys;
        private IIRCBotHandler myBot;
        //private string[] myFile;
        private List<string> myFile;
        private Random rand;

        public string ID {
            get { return "Insult"; }
        }

        public string Version
        {
            get
            {
                return "1.00.2007-08-24";
            }
        }

        public List<string> Keywords {
            get { return keys; }
        }

        public string Description {
            get { return "Insults an user in the current channel."; }
        }

        public int MinimumAuth {
            get { return 0; }
        }

        public void Initialize(IIRCBotHandler botCore) {
            myBot = botCore;
            
            keys = new List<string>();
            rand = new Random((int) DateTime.Now.Ticks);

            LoadFromFile(null);

            //keys.Add("^!insult$");
            keys.Add("^!insult ([^ ]+)$");
       }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters) {
            List<string> response = new List<string>();
            string receiver = parameters.Substring("!insult ".Length);
            if (myBot != null)
            {
                if (myBot.Nickname.Equals(receiver))
                {
                    response.Add(Utilities.BuildPrivMsg(myBot.Channel, string.Format(myFile[rand.Next(myFile.Count)], nick)));
                }
                else
                {
                    response.Add(Utilities.BuildPrivMsg(myBot.Channel, string.Format(myFile[rand.Next(myFile.Count)], receiver)));
                }
            }
            return response;
        }

        public void Refresh() {

        }

        public List<string> DumpToFile() {
            return new List<string>();
        }

        public void LoadFromFile(List<string> fileDump)
        {
            try
            {
                myFile = myBot.GetPluginConfig(this);
                //myFile = File.ReadAllLines(QuotesFile);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("#### PLUG-IN: (Insult) The file 'Quotes.txt' could not be found.");
            }
            catch (Exception excc)
            {
                Console.WriteLine("#### PLUG-IN: (Insult) An error occured!!!\n\n" + excc.Message);
            }

        }

        #endregion
    }
}
