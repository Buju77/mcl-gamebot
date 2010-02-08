using System;
using System.Collections.Generic;
using System.Text;
using IRCBotInterfaces;
using System.Xml.Serialization;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace BotPlugins
{
    public class MyStats:ICommand
    {
        private static string id = "mystats";
        private static string description = "MyStats";
        private static int minAuth = 0;
        private static List<string> keywords = new List<string>();
        private static IIRCBotHandler bot;

        private static string isReceiver;

        private static string statsRegexString = @"<td class=""(hi)?rankc"".*?>(?<rank>\d+)</td>\s*<td[^>]*><span[^>]*>(?<nick>\S+)</span></td>\s*<td[^>]*>.*?&nbsp;(?<lines>\d+)</td>";

        private static List<string> returnValues = new List<string>();

        static MyStats()
        {
        }

        #region ICommand Members

        public string ID
        {
            get
            {
                return id;
            }
        }

        public string Version
        {
            get
            {
                return "1.00.2007-08-24";
            }
        }

        public List<string> Keywords
        {
            get
            {
                return keywords;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public int MinimumAuth
        {
            get
            {
                return minAuth;
            }
        }

        private static string BuildMessage(string msg)
        {
            if (String.IsNullOrEmpty(isReceiver))
            {
                isReceiver = bot.Channel;
            }
            return Utilities.BuildPrivMsg(isReceiver, msg);
        }

        public void Initialize(IIRCBotHandler botCore)
        {
            bot = botCore;
            keywords.Clear();
            keywords.Add("!mystats");
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            isReceiver = bot.Channel;
            if (!channel)
            {
                isReceiver = nick;
            }

            returnValues.Clear();
            
            switch (parameters)
            {
                case "!mystats":
                    GetStats(nick, parameters);
                    break;
            }

            return returnValues;
        }

        private void GetStats(string nick, string parameters)
        {
            try
            {
                List<string> myConfig = bot.GetPluginConfig(this);

                string url = string.Empty;
                if (myConfig != null && myConfig.Count > 0)
                {
                    url = myConfig[0];
                }
                else
                {
                    bot.Print(this, "config file not found or empty. plugin exiting.");
                    return;
                }

                WebClient wc = new WebClient();

                Stream s = wc.OpenRead(url);
                StreamReader sr = new StreamReader(s);

                string stats = sr.ReadToEnd();

                sr.Close();

                string str = String.Format(statsRegexString, Regex.Escape(nick));
                Regex r = new Regex(String.Format(statsRegexString, Regex.Escape(nick)), RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                MatchCollection mc = r.Matches(stats);

                Dictionary<string, int> nickLines = new Dictionary<string, int>();
                List<string> nickList = new List<string>();

                foreach (Match m in mc)
                {
                    if (m.Success)
                    {
                        int curLines = int.Parse(m.Groups["lines"].Value);
                        string curNick = m.Groups["nick"].Value;

                        nickList.Add(curNick);
                        nickLines.Add(curNick, curLines);
                    }
                }

                if (nickList.Contains(nick))
                {
                    int myIndex = nickList.IndexOf(nick);
                    int myPlace = myIndex + 1;
                    int myLines = nickLines[nick];

                    if (myPlace > 1)
                    {
                        string previousNick = nickList[myIndex - 1];
                        int previousLines = nickLines[previousNick];
                        int lineDifference = previousLines - myLines;

                        if (lineDifference == 0)
                        {
                            returnValues.Add(BuildMessage(String.Format("{0}, you're at place {1} in the channel statistics, tied with {3}.", nick, myPlace, previousNick)));
                        }
                        else
                        {
                            returnValues.Add(BuildMessage(String.Format("{0}, you're at place {1} in the channel statistics, {2} behind {3}.", nick, myPlace, lineDifference == 1 ? "1 line" : String.Format("{0} lines", lineDifference), previousNick)));
                        }
                    }
                    else
                    {
                        returnValues.Add(BuildMessage(String.Format("{0}, you're leading the channel statistics!", nick)));
                    }
                }
                else
                {
                    returnValues.Add(BuildMessage(String.Format("Sorry {0}, you are either not in the stats list, or some weird error occurred.", nick)));
                }
            }
            catch (Exception e)
            {
                returnValues.Add(BuildMessage(String.Format("Sorry {0}, a {1} occurred while trying to process your request.", nick, e.Message)));
            }
        }

        public void Refresh()
        {

        }

        public List<string> DumpToFile()
        {
            return null;
        }

        public void LoadFromFile(List<string> fileDump)
        {
        }

        #endregion
    }
}
