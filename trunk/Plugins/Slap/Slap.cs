using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IRCBotInterfaces;
using System.Text.RegularExpressions;

namespace Slap
{
    public class Slap : ICommand
    {        
        private IIRCBotHandler m_botCore;

        private const string REGEX_HELP = "^!slap help$";
        private const string REGEX_RANKING = "^!slap ranking$";
        private const string REGEX_DO_IT = "^!slap ([^ ]+)$";

        private Dictionary<string, Dictionary<string, int>> m_slappingDict;  // <slapper, <slapped, count>>

        #region ICommand Members

        public string Description
        {
            get
            {
                return "Slaps someone around a bit with a large trout!";
            }
        }

        public string ID
        {
            get
            {
                return "Slap";
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
                return "0.01.2010-05-19";
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
                    REGEX_RANKING
                };
            }
        }

        public void Initialize(IIRCBotHandler botCore)
        {
            this.m_botCore = botCore;

            this.m_slappingDict = new Dictionary<string, Dictionary<string, int>>();
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
                response.Add(Utilities.BuildNotice(nick, "!slap <NICK>"));
                response.Add(Utilities.BuildNotice(nick, "!slap ranking"));
                response.Add(Utilities.BuildNotice(nick, "!slap help"));
            }
            else if (Regex.Match(parameters, REGEX_RANKING).Success)
            {
                response.Add(Utilities.BuildNotice(nick, "Most active slappers:"));
                response.AddRange(this.m_slappingDict
                    .Where(kv => kv.Value.Count() > 0)
                    .Select(kv => new KeyValuePair<string, int>(kv.Key, kv.Value.Select(kv2 => kv2.Value).Sum()))
                    .OrderBy(kv => kv.Value)
                    .Select(kv => Utilities.BuildNotice(nick, kv.Key + ": " + kv.Value))
                    .ToList());
                
                response.Add(Utilities.BuildNotice(nick, "Most active victims:"));
                response.AddRange(this.m_slappingDict
                    .SelectMany(kv => kv.Value)
                    .GroupBy(kv => kv.Key)
                    .Where(g => g.Count() > 0)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum(x => x.Value)))
                    .OrderBy(kv => kv.Value)
                    .Select(kv => Utilities.BuildNotice(nick, kv.Key + ": " + kv.Value))
                    .ToList());
            }
            else if (Regex.Match(parameters, REGEX_DO_IT).Success)
            {
                string receiver = channel ? m_botCore.Channel : nick;

                if (parameters.Length > 7)
                {
                    var slapped = parameters.Substring(6);

                    if (!this.m_slappingDict.ContainsKey(nick))
                    {
                        this.m_slappingDict.Add(nick, new Dictionary<string, int>());
                    }

                    if (!this.m_slappingDict[nick].ContainsKey(slapped))
                    {
                        this.m_slappingDict[nick].Add(slapped, 0);
                    }
                    this.m_slappingDict[nick][slapped]++;

                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format("{0} slaps {1} around a bit with a large trout!", nick, slapped)));
                }
            }

            return response;
        }

        public List<string> DumpToFile()
        {
            var result = new List<string>();

            foreach (var slapper in this.m_slappingDict)
            {
                foreach (var slapped in slapper.Value)
                {
                    result.Add(slapper.Key + "@" + slapped.Key + "@" + slapped.Value);
                }
            }

            return result;
        }

        public void LoadFromFile(List<string> fileDump)
        {
            this.m_slappingDict.Clear();

            foreach (var item in fileDump)
            {
                var parts = item.Split('@');
                if (parts.Length == 3)
                {
                    if (!this.m_slappingDict.ContainsKey(parts[0]))
                    {
                        this.m_slappingDict.Add(parts[0], new Dictionary<string, int>());
                    }
                    int val;
                    try
                    {
                        val = Int32.Parse(parts[2]);
                    }
                    catch
                    {
                        val = 0;
                    }
                    this.m_slappingDict[parts[0]][parts[1]] = val;
                }
            }
        }

        #endregion
    }
}
