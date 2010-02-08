using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using IRCBotInterfaces;

namespace Hangman
{
    public class Hangman : ICommand
    {
        private static string id = "hangman";
        private static string description = "Hangman game.";
        private static int minAuth = 0;
        private static List<string> keywords = new List<string>();
        private static IIRCBotHandler bot;

        private static bool running = false;

        public static bool Running
        {
            get { return Hangman.running; }
            set
            {
                Hangman.running = value;
                if (value == false)
                {
                    try
                    {
                        timeThread.Interrupt();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        private static string solution = null;
        private static string maskedSolution = null;
        private static string guessedChars = "";
        private static int tryCount = 0;
        private static int maxTryCount = 9;
        private static string[] wrongWords = new string[] { "nope", "usuk", "wrong", "no" };

        private static List<string> returnValues = new List<string>();

        private static Thread timeThread;


        static Hangman()
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
            return Utilities.BuildPrivMsg(bot.Channel, msg);
        }

        private void StartRoulette(string nick)
        {

        }

        private void PlayRoulette(string nick)
        {

        }

        private void PrintHelp(string nick)
        {
            string message = BuildMessage("Help goes here");

            returnValues.Add(message);
        }

        private void PrintStats(string nick)
        {
            string message = BuildMessage("Stats go here");

            returnValues.Add(message);
        }

        public void Initialize(IIRCBotHandler botCore)
        {
            bot = botCore;
            keywords.Clear();
            keywords.Add("^!hangman.*$");
            keywords.Add(@"^\?.+$");
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            returnValues.Clear();

            if (Regex.IsMatch(parameters, @"^!hangman.*$"))
            {
                if (!Running)
                {
                    if (!channel)
                    {
                        StartHangman(nick, parameters);
                    }
                }
            }
            else if (Regex.IsMatch(parameters, @"^\?.+$"))
            {
                if (Running)
                {
                    PlayHangman(nick, parameters);
                }
            }

            return returnValues;
        }

        private void PlayHangman(string nick, string parameters)
        {
            Match m = Regex.Match(parameters, @"^\?\s*(?<letter>[\w-[_]])\s*$");
            if (m != Match.Empty)
            {
                TryLetter(nick, m.Groups["letter"].Value.ToLower());
                return;
            }
            m = Regex.Match(parameters, @"^\?\s*(?<solution>.{2,})$");
            if (m != Match.Empty)
            {
                TrySolution(nick, m.Groups["solution"].Value.Trim().ToLower());
                return;
            }
        }

        private void TrySolution(string nick, string triedSolution)
        {
            if (triedSolution.Equals(solution.ToLower()))
            {
                returnValues.Add(BuildMessage(String.Format("{0}, you got it! {1}", nick, solution)));
                Running = false;
            }
            else
            {
                if (++tryCount >= maxTryCount)
                {
                    returnValues.Add(BuildMessage(String.Format("That was the last try. You failed miserably. The word was: {0}", solution)));
                    Running = false;
                }
                else
                {
                    int triesLeft = maxTryCount - tryCount;
                    returnValues.Add(BuildMessage(String.Format("{0}, {1}. try again: {2}", nick, GetWrongWord(), maskedSolution)));
                    if (triesLeft > 1)
                    {
                        returnValues.Add(BuildMessage(String.Format("{0}{1} tries left{2}", (triesLeft <= 3 ? "only " : ""), triesLeft, (triesLeft <= 3 ? "!" : "."))));
                    }
                    else
                    {
                        returnValues.Add(BuildMessage("Last chance!"));
                    }
                }
            }
        }

        private void TryLetter(string nick, string letter)
        {
            if (guessedChars.Contains(letter))
            {
                returnValues.Add(BuildMessage(String.Format("{0}, Letter {1} has already been tried: {2}", nick, letter, maskedSolution)));
            }
            else
            {
                guessedChars += letter;
                if (Regex.IsMatch(solution, letter, RegexOptions.IgnoreCase))
                {
                    string replacementRegex = String.Format(@"[^\W_{0}]", guessedChars);
                    maskedSolution = Regex.Replace(solution, replacementRegex, ".", RegexOptions.IgnoreCase);

                    if (maskedSolution.Equals(solution))
                    {
                        returnValues.Add(BuildMessage(String.Format("{0}, you got it: {2}", nick, letter, solution)));
                        Running = false;
                    }
                    else
                    {
                        returnValues.Add(BuildMessage(String.Format("{0}, '{1}' is in the word: {2}", nick, letter, maskedSolution)));
                    }
                }
                else
                {
                    if (++tryCount >= maxTryCount)
                    {
                        returnValues.Add(BuildMessage(String.Format("That was the last try. You failed miserably. The word was: {0}", solution)));
                        Running = false;
                    }
                    else
                    {
                        int triesLeft = maxTryCount - tryCount;
                        returnValues.Add(BuildMessage(String.Format("{0}, {1}. '{2}' is not in the word: {3}", nick, GetWrongWord(), letter, maskedSolution)));
                        if (triesLeft > 1)
                        {
                            returnValues.Add(BuildMessage(String.Format("{0}{1} tries left{2}", (triesLeft <= 3 ? "only " : ""), triesLeft, (triesLeft <= 3 ? "!" : "."))));
                        }
                        else
                        {
                            returnValues.Add(BuildMessage("Last chance!"));
                        }
                    }
                }
            }
        }

        private string GetWrongWord()
        {
            Random r = new Random();
            int wrongWordIx = r.Next(0, wrongWords.Length);
            return wrongWords[wrongWordIx];
        }

        private void StartHangman(string nick, string parameters)
        {
            Match m = Regex.Match(parameters, @"^!hangman\s+(?<solution>.*)$");
            if (m != Match.Empty)
            {
                Running = true;
                tryCount = 0;
                solution = m.Groups["solution"].Value.Trim();
                maskedSolution = Regex.Replace(solution, @"[\w-[_]]", ".", RegexOptions.None);
                guessedChars = "";
                returnValues.Add(BuildMessage(String.Format("Hangman game started by {0}: {1}", nick, maskedSolution)));
                timeThread = new Thread(TimeThread);
                timeThread.Start();
            }
        }

        private void TimeThread()
        {
            try
            {
                Thread.Sleep(120000);
                if (!Running)
                    return;
                bot.SendMessage(BuildMessage("Faster!"));
                Thread.Sleep(60000);
                if (!Running)
                    return;
                bot.SendMessage(BuildMessage("Time's up! How lame."));
                bot.SendMessage(BuildMessage(String.Format("The word was: {0}", solution)));
                running = false;
            }
            catch (ThreadInterruptedException te)
            {
                return;
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