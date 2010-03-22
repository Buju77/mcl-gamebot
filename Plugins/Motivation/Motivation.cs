using System;
using System.Collections.Generic;
using System.Text;
using IRCBotInterfaces;
using System.Text.RegularExpressions;
using System.IO;

namespace Motivation
{
    public class Motivation : ICommand
    {
        #region Members

        private List<string> m_keyWords;
        private IIRCBotHandler m_Bot;
        private List<string> m_mpLinks;//men plus
        private List<string> m_mmLinks;//men minus
        private List<string> m_wpLinks;//women plus
        private List<string> m_wmLinks;//women minus
        private Dictionary<string, int> m_linkHistory;  // <link, count>
        private Dictionary<string, int> m_rankingDict;  // <user, count>
        private string m_lastLink;                      // this is to prevent that the same link doesn't diplay twice in a row after a turn increase
                                                        // this is the second check for that. first check is using Dictionary m_linkHistory
        private Random m_rand;
        private int m_wpTurn, m_wmTurn, m_mpTurn, m_mmTurn;

        private const string REGEX_HELP = "^![mw]otivation help$";
        private const string REGEX_RANKING = "^![mw]otivation ranking$";
        private const string REGEX_COUNT = "^![mw]otivation count$";
        private const string REGEX_ADD_ITEM = "^!add_[mw]otivation ([+-]) ([^ ]+)$";
        private const string REGEX_GETMOTIVATED = "^![mw]otivation[+-]?$";
        private const string REGEX_URL = "^((http(s?))\\://)((([a-zA-Z0-9_\\-]{2,}\\.)+[a-zA-Z]{2,})|((?:(?:25[0-5]|2[0-4]\\d|[01]\\d\\d|\\d?\\d)(?(\\.?\\d)\\.)){4}))(:[a-zA-Z0-9]+)?(/[a-zA-Z0-9\\-\\._\\?\\,\\'/\\\\\\+&amp;%\\$#\\=~]*)?$";

        #endregion

        #region Properties
        public string ID
        {
            get
            {
                return "Motivation";
            }
        }

        public string Version
        {
            get
            {
                return "1.30.2010-03-20";
            }
        }

        public List<string> Keywords
        {
            get
            {
                return m_keyWords;
            }
        }

        public string Description
        {
            get
            {
                return "Displayes a link to a random motivation picture.";
            }
        }

        public int MinimumAuth
        {
            get
            {
                return 0;
            }
        } 
        #endregion

        #region Methods

        public void Initialize(IIRCBotHandler botCore)
        {
            m_Bot = botCore;

            m_keyWords = new List<string>();
            m_rand = new Random((int)DateTime.Now.Ticks);
            m_lastLink = string.Empty;

            // init turn number with 1
            m_wpTurn = m_mpTurn = m_wmTurn = m_mmTurn = 1;

            LoadFromFile(null);

            m_keyWords.Add(REGEX_GETMOTIVATED);
            m_keyWords.Add(REGEX_RANKING);
            m_keyWords.Add(REGEX_COUNT);
            m_keyWords.Add(REGEX_ADD_ITEM);
            m_keyWords.Add(REGEX_HELP);
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            List<string> response = new List<string>();
            string receiver = channel ? m_Bot.Channel : nick;

            if (Regex.Match(parameters, REGEX_HELP).Success)
            {
                // display motivation help
                PrintHelp(nick, response);
            }
            else if (Regex.Match(parameters, REGEX_RANKING).Success)
            {
                // display rankings
                PrintRankings(nick, response);
            }
            else if (Regex.Match(parameters, REGEX_COUNT).Success)
            {
                // ![m|w]otivation count
                // display turn count
                PrintLinksTurnCount(nick, response);
            } 
            else if (Regex.Match(parameters, REGEX_GETMOTIVATED).Success)
            {
                // ![m|w]otivation[+|-]
                MotivateUser(user, parameters, response, receiver);
            }
            else if (Regex.Match(parameters, REGEX_ADD_ITEM).Success)
            {
                // !add_[m|w]otivation [+|-] <link>
                AddLink(nick, parameters, response);
            }

            return response;
        }

        private void MotivateUser(string user, string parameters, List<string> response, string receiver)
        {
            // ![m|w]otivation[+|-]
            string link = string.Empty;
            if (parameters.StartsWith("!m")) // Motivation for men
            {
                if (parameters.EndsWith("n")) // either sfw or nsfw
                {
                    // use both m+ and m- links
                    int plusOrMinus = this.m_rand.Next(2);
                    if (plusOrMinus == 0)
                    {
                        link = NextLink(m_mpLinks, ref m_mpTurn, false);
                    }
                    else
                    {
                        link = NextLink(m_mmLinks, ref m_mmTurn, false);
                    }
                }
                else if (parameters.EndsWith("+")) // only sfw
                {
                    // use just m+ links
                    link = NextLink(m_mpLinks, ref m_mpTurn, false);
                }
                else if (parameters.EndsWith("-")) // only nsfw
                {
                    // just m- links
                    link = NextLink(m_mmLinks, ref m_mmTurn, false);
                }
                response.Add(Utilities.BuildPrivMsg(receiver, string.Format("{0} ({1})({1})", link.Substring(1), link.Substring(0, 1))));
            }
            else // motivation for women
            {
                if (parameters.EndsWith("n")) // either sfw or nsfw
                {
                    // use both w+ and w- links
                    int plusOrMinus = this.m_rand.Next(2);
                    if (plusOrMinus == 0)
                    {
                        link = NextLink(m_wpLinks, ref m_wpTurn, true);
                    }
                    else
                    {
                        link = NextLink(m_wmLinks, ref m_wmTurn, true);
                    }
                }
                else if (parameters.EndsWith("+")) // only sfw
                {
                    // just w+
                    link = NextLink(m_wpLinks, ref m_mpTurn, true);
                }
                else if (parameters.EndsWith("-")) // only nsfw
                {
                    // just w-
                    link = NextLink(m_wmLinks, ref m_wmTurn, true);
                }
                response.Add(Utilities.BuildPrivMsg(receiver, string.Format("{0} .{1}.", link.Substring(1), link.Substring(0, 1))));
            }

            // save last link
            m_lastLink = link;

            // increase users !motivation count
            if (m_rankingDict.ContainsKey(user))
            {
                m_rankingDict[user]++;
            }
            else
            {
                m_rankingDict[user] = 1;
            }
        }

        private void AddLink(string nick, string parameters, List<string> response)
        {
            //"^!add_[mw]otivation ([+-]) ([^ ]+)$";
            string cmd = parameters.Substring(0, 15);
            string sfw = parameters.Substring(16, 1);
            string url = parameters.Substring(18);

            if (Regex.Match(url, REGEX_URL).Success && Regex.Match(sfw, "[+-]").Success)
            {
                if (cmd.Contains("w"))
                {
                    if (sfw.Equals("+"))
                    {
                        m_wpLinks.Add(sfw + url);
                    }
                    else
                    {
                        m_wmLinks.Add(sfw + url);
                    }
                }
                else
                {
                    if (sfw.Equals("+"))
                    {
                        m_mpLinks.Add(sfw + url);
                    }
                    else
                    {
                        m_mmLinks.Add(sfw + url);
                    }
                }
                // also add it to link history with count = 0
                m_linkHistory[sfw + url] = 0;
                response.Add(Utilities.BuildNotice(nick, "link added."));
            }
            else
            {
                response.Add(Utilities.BuildNotice(nick, "invalid parameters."));
            }
        }

        private void PrintHelp(string nick, List<string> response)
        {
            response.Add(Utilities.BuildNotice(nick, string.Format("{0} (Version: {1})", this.ID, this.Version)));
            response.Add(Utilities.BuildNotice(nick, "![m|w]otivation"));
            response.Add(Utilities.BuildNotice(nick, "![m|w]otivation[+|-]"));
            response.Add(Utilities.BuildNotice(nick, "![m|w]otivation count"));            
            response.Add(Utilities.BuildNotice(nick, "![m|w]otivation ranking"));
            response.Add(Utilities.BuildNotice(nick, "!add_[m|w]otivation [+|-] <link>"));
            response.Add(Utilities.BuildNotice(nick, "![m|w]otivation help"));
        }

        private void PrintLinksTurnCount(string nick, List<string> response)
        {
            response.Add(Utilities.BuildNotice(nick, "Displays how often all links in a list have been motivated once. (How many rounds)"));
            response.Add(Utilities.BuildNotice(nick, string.Format("m+ > links count: {0}; round: {1}", m_mpLinks.Count, m_mpTurn)));
            response.Add(Utilities.BuildNotice(nick, string.Format("m- > links count: {0}; round: {1}", m_mmLinks.Count, m_mmTurn)));
            response.Add(Utilities.BuildNotice(nick, string.Format("w+ > links count: {0}; round: {1}", m_wpLinks.Count, m_wpTurn)));
            response.Add(Utilities.BuildNotice(nick, string.Format("w- > links count: {0}; round: {1}", m_wmLinks.Count, m_wmTurn)));
        }

        private void PrintRankings(string nick, List<string> response)
        {
            List<string> list = new List<string>(m_rankingDict.Keys);

            // der herkömmliche weg (.Net 2.0/3.5)
            list.Sort(delegate(string x, string y)
            {
                return m_rankingDict[x].CompareTo(m_rankingDict[y]);
            });
            // mithilfe von lambda expressions.
            //list.Sort((x, y) => x.CompareTo(y));

            for (int i = 0; i < list.Count; i++)
            {
                response.Add(Utilities.BuildNotice(nick, string.Format("{0}. {1}: {2}", i + 1, list[i], m_rankingDict[list[i]])));
            }
        }

        /// <summary>
        /// This method is for generating next link for either men or women. 
        /// It takes care of increasing turn if every link was already displayed turn-times.
        /// </summary>
        /// <param name="links">Collection containing links to display.</param>
        /// <param name="turn">Number of turns this collection has.</param>
        /// <param name="forWomen">Determine if the link is generated for women or men.</param>
        /// <returns>A random link from the collection.</returns>
        private string NextLink(List<string> links, ref int turn, bool forWomen)
        {
            if (links.Count == 0)
            {
                return forWomen ? "|" : ".";
            }

            // if there is only 1 link in the collection, just return that
            if (links.Count == 1)
            {
                ++turn;
                return links[0];
            }

            //
            // links.count has more than 1 link
            //
            Dictionary<string, int> tempDict = new Dictionary<string, int>(links.Count);
            int tryCount = 0;
            while (tryCount < links.Count)
            {
                // generate next random idx
                int randIdx = m_rand.Next(0, links.Count);

                // get random link
                string link = links[randIdx];

                // get link count
                int linkCount = m_linkHistory[link];

                // if link isn't already displayed turn-times --> OK

                if (linkCount < turn)
                {
                    if (m_lastLink == link)
                    {
                        // this is to prevent that the same link doesn't diplay twice in a row after a turn increase
                        // because after a turn increase, all links are possible (also that link, that was the last one in the previous turn)
                        continue;
                    }
                    ++m_linkHistory[link];
                    return link;
                }

                // if count is already at turn --> next link
                if (!tempDict.ContainsKey(link))
                {
                    tryCount++;
                    tempDict[link] = linkCount;
                }
            }

            // EVERY link was already displayed turn-times --> increase turn by 1 and get a new link
            ++turn;
            return NextLink(links, ref turn, forWomen);
        }

        public void Refresh()
        {
            LoadFromFile(null);
        }

        public List<string> DumpToFile()
        {
            List<string> allLinks = new List<string>();

            // rankings lachi 100;buju 3;alph 1
            string rankings = "rankings ";
            foreach (KeyValuePair<string, int> var in m_rankingDict)
            {
                rankings += string.Format("{0} {1};", var.Key, var.Value);
            }
            allLinks.Add(rankings);

            // add turns
            allLinks.Add(string.Format("turns {0} {1} {2} {3}", m_wpTurn, m_wmTurn, m_mpTurn, m_mmTurn));
            foreach (string link in m_mpLinks)
            {
                allLinks.Add("m" + link + " " + m_linkHistory[link]);
            }
            foreach (string link in m_mmLinks)
            {
                allLinks.Add("m" + link + " " + m_linkHistory[link]);
            }
            foreach (string link in m_wpLinks)
            {
                allLinks.Add("w" + link + " " + m_linkHistory[link]);
            }
            foreach (string link in m_wmLinks)
            {
                allLinks.Add("w" + link + " " + m_linkHistory[link]);
            }
            return allLinks;
        }

        public void LoadFromFile(List<string> fileDump)
        {
            try
            {
                // TODO: always clear? or add Motivation.conf to fileDump?!?
                m_mpLinks = new List<string>();
                m_mmLinks = new List<string>();
                m_wpLinks = new List<string>();
                m_wmLinks = new List<string>();
                m_linkHistory = new Dictionary<string, int>();
                m_rankingDict = new Dictionary<string, int>();

                // 2010-03-20 Buju: 
                // new dump format to support link count and links turn (so the same link won't be displayed until all other links have been displayed once)
                // syntax:
                // Motivation
                // turn <wp turn> <wm turn> <mp turn> <mm turn>
                // [m|w][+|-]<link> <count>

                // Example:
                // Motivation
                // turn 7 3 3 1
                // m+http://www.google.at/ 4

                // use fileDump or Motivation.conf if fileDump is null
                List<string> allLinks = fileDump == null ? m_Bot.GetPluginConfig(this) : fileDump;
                
                foreach (string line in allLinks)
                {
                    if (line.StartsWith("rankings"))
                    {
                        // rankings lachi 100;buju 3;alph 1
                        int blank = line.IndexOf(' ');
                        string rankings = line.Substring(blank + 1);
                        string[] values = rankings.Split(';');
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i] == string.Empty)
                            {
                                continue;
                            }
                            string[] userValues = values[i].Split(' ');
                            m_rankingDict[userValues[0]] = Int32.Parse(userValues[1]);
                        }
                    } 
                    else if (line.StartsWith("turns"))
                    {
                        int blank = line.IndexOf(' ');
                        string turns = line.Substring(blank + 1);
                        string[] turnValues = turns.Split(' ');

                        // parse turn numbers                  
                        if (turnValues.Length == 4)
                        {
                            Int32.TryParse(turnValues[0], out m_wpTurn);
                            Int32.TryParse(turnValues[1], out m_wmTurn);
                            Int32.TryParse(turnValues[2], out m_mpTurn);
                            Int32.TryParse(turnValues[3], out m_mmTurn);
                        }
                    }
                    else
                    {
                        if (line.StartsWith(this.ID))
                        {
                            continue;
                        }

                        string[] linkAndCount = line.Substring(1).Split(' ');
                        string link = linkAndCount[0];
                        int count;
                        if (linkAndCount.Length != 2 || !Int32.TryParse(linkAndCount[1], out count))
                        {
                            count = 0;
                        }

                        // add link and count to dictionary
                        m_linkHistory[link] = count;

                        if (line.StartsWith("w+"))
                        {
                            m_wpLinks.Add(link);
                        }
                        else if (line.StartsWith("w-"))
                        {
                            m_wmLinks.Add(link);
                        }
                        else if (line.StartsWith("m+"))
                        {
                            m_mpLinks.Add(link);
                        }
                        else if (line.StartsWith("m-"))
                        {
                            m_mmLinks.Add(link);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("#### PLUG-IN: (Motivation) The file 'Motivation.conf' could not be found.");
            }
            catch (Exception excc)
            {
                Console.WriteLine("#### PLUG-IN: (Motivation) An error occured!!!\n\n" + excc.Message);
            }

        }

        #endregion
    }
}
