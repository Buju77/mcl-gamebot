using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRCBotInterfaces;
using System.Text.RegularExpressions;

namespace Tell
{
    public class Tell : ICommand
    {        
        private IIRCBotHandler m_botCore;

        private const string REGEX_HELP = "^!tell help$";
        private const string REGEX_DO_IT = "^!tell (?<who>[^ ]+) (?<what>.+)$";

        private Dictionary<string, List<string>> m_tellDict;  // <who to tell, msg to tell>

        #region ICommand Members

        public string Description
        {
            get
            {
                return "Tell someone something!";
            }
        }

        public string ID
        {
            get
            {
                return "Tell";
            }
        }

        public int MinimumAuth
        {
            get
            {
                return 0;
            }
        }

        public string Version
        {
            get
            {
                return "0.11.2010-12-11";
            }
        }

        public List<string> Keywords
        {
            get
            {
                return new List<string>()
                {
                    REGEX_DO_IT,
                    REGEX_HELP,
                };
            }
        }

        public void Initialize(IIRCBotHandler botCore)
        {
            this.m_botCore = botCore;

            this.m_tellDict = new Dictionary<string, List<string>>();
        
            this.m_botCore.UserActionOccured += new UserActionEventHandler(m_botCore_UserActionOccured);
        }

        public void Refresh()
        {
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            var response = new List<string>();

            if (Regex.Match(parameters, REGEX_HELP).Success)
            {
                response.Add(Utilities.BuildNotice(nick, string.Format("{0} (Version: {1})", this.ID, this.Version)));
                response.Add(Utilities.BuildNotice(nick, "!tell <NICK> <msg>"));
                response.Add(Utilities.BuildNotice(nick, "!tell help"));
            }
            else if (Regex.IsMatch(parameters, REGEX_DO_IT, RegexOptions.IgnoreCase))
            {
                Match values = Regex.Match(parameters, REGEX_DO_IT, RegexOptions.IgnoreCase);

                string who = values.Groups["who"].Value;
                string what = values.Groups["what"].Value;

                // using .toLower() to support camelcase nicks
                if (nick.ToLower().Contains(who.ToLower()) || user.ToLower().Contains(who.ToLower()))
                {
                    response.Add(Utilities.BuildPrivMsg(m_botCore.Channel, "Why are you talking to yourself, dumbass?!?"));
                }
                else
                {
                    // build correct msg
                    string msg = string.Format("[{0}] <{1}> {2}, {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), nick, who, what);

                    if (!m_tellDict.ContainsKey(who.ToLower()))
                    {
                        // create new list
                        List<string> newList = new List<string>();
                        m_tellDict[who.ToLower()] = newList;
                    }

                    m_tellDict[who.ToLower()].Add(msg);

                    // response texts
                    string recipient = channel ? m_botCore.Channel : nick;
                    response.Add(Utilities.BuildNotice(recipient, string.Format("Ok, I'll tell {0} about '{1}'", who, msg)));
                }
            }

            return response;
        }

        public List<string> DumpToFile()
        {
            var result = new List<string>();

            foreach (var item in this.m_tellDict)
            {
                foreach (var message in item.Value)
                {
                    // who\tmessage
                    result.Add(item.Key + "\t" + message);
                }
            }

            return result;
        }

        public void LoadFromFile(List<string> fileDump)
        {
            this.m_tellDict.Clear();

            foreach (var item in fileDump)
            {
                var parts = item.Split('\t');
                if (parts.Length == 2)
                {
                    if (!this.m_tellDict.ContainsKey(parts[0]))
                    {
                        // create array
                        this.m_tellDict.Add(parts[0], new List<string>());
                    }

                    // add message to array
                    this.m_tellDict[parts[0]].Add(parts[1]);
                }
            }
        }

        void m_botCore_UserActionOccured(object sender, UserActionEventArgs uaea)
        {
            string nickname = uaea.Nickname.ToLower();
            List<string> nicksToRemove = new List<string>();

            switch (uaea.UserAction)
            {
                case UserAction.Joined:
                    foreach (var who in m_tellDict.Keys)
                    {
                        // check if we have something to tell to the just joined user
                        bool hadSomethingToSay = false;
                        if (nickname.Contains(who))
                        {
                            // go through the list to tell something
                            foreach (string message in m_tellDict[who])
                            {
                                // send what to tell
                                m_botCore.SendMessage(Utilities.BuildNotice(nickname, message));
                                hadSomethingToSay = true;
                            }

                            // remove the messages from memory
                            nicksToRemove.Add(who);
                        }
                        if (hadSomethingToSay)
                        {
                            m_botCore.SendMessage(Utilities.BuildPrivMsg(m_botCore.Channel, string.Format("I just told {0} something.", who)));
                        }
                    }
                    break;
                case UserAction.Part:
                    break;
                case UserAction.Quit:
                    break;
                default:
                    break;
            }

            // remove messages from dict
            foreach (var who in nicksToRemove)
            {
                m_tellDict.Remove(who);
            }
        }

        #endregion
    }
}
