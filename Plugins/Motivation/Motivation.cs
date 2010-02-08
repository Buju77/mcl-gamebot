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
        private Random m_rand;

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
                return "1.00.2009-08-26";
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

            LoadFromFile(null);

            m_keyWords.Add(REGEX_ADD_ITEM);
            m_keyWords.Add(REGEX_GETMOTIVATED);
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            List<string> response = new List<string>();
            string receiver = channel ? m_Bot.Channel : nick;

            if (Regex.Match(parameters, REGEX_GETMOTIVATED).Success)
            {
                string link = "";
                if (parameters.StartsWith("!m")) // Motivation for men
                {
                    if (parameters.EndsWith("n")) // either sfw or nsfw
                    {
                        if (m_mmLinks.Count == 0 && m_mpLinks.Count == 0)
                        {
                            link = ".";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, (m_mpLinks.Count + m_mmLinks.Count) - 1);
                            if (randIdx >= m_mpLinks.Count)
                            {
                                link = m_mmLinks[randIdx - m_mpLinks.Count];
                            }
                            else
                            {
                                link = m_mpLinks[randIdx];
                            }
                        }
                    }
                    else if (parameters.EndsWith("+")) // only sfw
                    {
                        if (m_mpLinks.Count == 0)
                        {
                            link = ".";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, m_mpLinks.Count - 1);
                            link = m_mpLinks[randIdx];
                        }
                    }
                    else if (parameters.EndsWith("-")) // only nsfw
                    {
                        if (m_mmLinks.Count == 0)
                        {
                            link = ".";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, m_mmLinks.Count - 1);
                            link = m_mmLinks[randIdx];
                        }
                    }
                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format("{0} ({1})({1})", link.Substring(1), link.Substring(0, 1))));
                }
                else // motivation for women
                {
                    if (parameters.EndsWith("n")) // either sfw or nsfw
                    {
                        if (m_wmLinks.Count == 0 && m_wpLinks.Count == 0)
                        {
                            link = "|";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, (m_wpLinks.Count + m_wmLinks.Count) - 1);
                            if (randIdx >= m_wpLinks.Count)
                            {
                                link = m_wmLinks[randIdx - m_wpLinks.Count];
                            }
                            else
                            {
                                link = m_wpLinks[randIdx];
                            }
                        }
                    }
                    else if (parameters.EndsWith("+")) // only sfw
                    {
                        if (m_wpLinks.Count == 0)
                        {
                            link = "|";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, m_wpLinks.Count - 1);
                            link = m_wpLinks[randIdx];
                        }
                    }
                    else if (parameters.EndsWith("-")) // only nsfw
                    {
                        if (m_wmLinks.Count == 0)
                        {
                            link = "|";
                        }
                        else
                        {
                            int randIdx = m_rand.Next(0, m_wmLinks.Count - 1);
                            link = m_wmLinks[randIdx];
                        }
                    }
                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format("{0} .{1}.", link.Substring(1), link.Substring(0, 1))));
                }
            }
            else if (Regex.Match(parameters, REGEX_ADD_ITEM).Success)
            {//"^!add_[mw]otivation ([+-]) ([^ ]+)$";
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
                    response.Add(Utilities.BuildNotice(nick, "link added."));
                }
                else
                {
                    response.Add(Utilities.BuildNotice(nick, "invalid parameters."));
                }
            }

            return response;
        }

        public void Refresh()
        {
            LoadFromFile(null);
        }

        public List<string> DumpToFile()
        {
            List<string> allLinks = new List<string>();
            foreach (string link in m_mpLinks)
            {
                allLinks.Add("m" + link);
            }
            foreach (string link in m_mmLinks)
            {
                allLinks.Add("m" + link);
            }
            foreach (string link in m_wpLinks)
            {
                allLinks.Add("w" + link);
            }
            foreach (string link in m_wmLinks)
            {
                allLinks.Add("w" + link);
            }
            return allLinks;
        }

        public void LoadFromFile(List<string> fileDump)
        {
            try
            {
                m_mpLinks = new List<string>();
                m_mmLinks = new List<string>();
                m_wpLinks = new List<string>();
                m_wmLinks = new List<string>();
                List<string> allLinks = m_Bot.GetPluginConfig(this);
                foreach (string link in allLinks)
                {
                    if (link.StartsWith("w+"))
                    {
                        m_wpLinks.Add(link.Substring(1));
                    }
                    else if (link.StartsWith("w-"))
                    {
                        m_wmLinks.Add(link.Substring(1));
                    }
                    else if (link.StartsWith("m+"))
                    {
                        m_mpLinks.Add(link.Substring(1));
                    }
                    else if (link.StartsWith("m-"))
                    {
                        m_mmLinks.Add(link.Substring(1));
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
