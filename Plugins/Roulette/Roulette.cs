/**
 * Changelog:
 * 
 * Version 1.00: 
        Initial Release
  
 * Version 1.10:
        New Command: !roulette ranking
  
 * Version 1.11:
        highscore list limited to top10
 
 * Version 1.12:
        calculation for roulette ranking changed
        ranking and stats are sent as notice not to channel
 */

using System;
using System.Collections.Generic;
using System.Text;
using IRCBotInterfaces;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Threading;

namespace Roulette
{
    public class Roulette : ICommand
    {
        private static string id = "roulette";
        private static string description = "Roulette, what else.";
        private static int minAuth = 0;
        private static List<string> keywords = new List<string>();
        private static IIRCBotHandler bot;

        private static bool running = false;
        private static int chamberCount = 6;
        private static int currentChamber = 1;
        private static int badChamber = 0;
        private static int highscoreCount = 10;

        private static string bangWord = "*BANG*";
        private static string clickWord = "*click*";

        private static List<string> participants = new List<string>();

        private static List<string> returnValues = new List<string>();

        private static Thread timerThread;


        static Roulette()
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
                return "1.12.2007-10-22";
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

        private void SpinCylinder()
        {
            Random r = new Random();
            badChamber = r.Next(1, chamberCount + 1);
            currentChamber = 1;
            returnValues.Add(Utilities.BuildActionMessage(bot.Channel, "spins the cylinder..."));
        }

        private void StartRoulette(string username)
        {
            participants.Clear();
            running = true;
            timerThread = new Thread(TimeThread);

            SpinCylinder();

            returnValues.Add(Utilities.BuildPrivMsg(bot.Channel, string.Format("Roulette game started by {0}, type \"{1}\" to play!", username, keywords[0])));
        }

        private void PlayRoulette(string nick)
        {
            if (!participants.Contains(nick))
            {
                participants.Add(nick);
            }

            if (running)
            {
                StringBuilder sb = new StringBuilder(string.Format("{0}, Chamber {1} of {2}: ", nick, currentChamber, chamberCount));
                if (currentChamber++ >= badChamber)
                {
                    sb.Append(bangWord);
                    EndRoulette(nick);
                }
                else
                {
                    sb.Append(clickWord);
                }

                string message = Utilities.BuildPrivMsg(bot.Channel, sb.ToString());

                returnValues.Add(message);

                if (!running)
                {
                    returnValues.Add(Utilities.BuildPrivMsg(bot.Channel, "You lost!"));
                }
                else
                {
                    if (currentChamber == chamberCount)
                    {
                        SpinCylinder();
                    }
                }
            }
        }

        private void TimeThread()
        {
            try
            {
                Thread.Sleep(30 * 60000);
            }
            catch (ThreadAbortException)
            {
            }
            finally
            {
                if (running)
                {
                    bot.SendMessage(Utilities.BuildActionMessage(bot.Channel, "stops the roulette game since nobody seems to be interested in playing anymore."));
                    running = false;
                }
            }
        }

        private void EndRoulette(string username)
        {
            running = false;
            timerThread.Abort();


            if (participants.Contains(username))
            {
                participants.Remove(username);
            }

            foreach (string s in participants)
            {
                RouletteStats.GameWon(s);
                RouletteStats.GamePlayed(s);
            }

            RouletteStats.GameLost(username);
            RouletteStats.GamePlayed(username);

            RouletteStats.RecalculateStats();
        }

        private void PrintHelp(string username)
        {
            string message = Utilities.BuildNotice(username, String.Format("No, {0}. You know how this works...", username));

            returnValues.Add(message);
        }

        private void PrintStats(string nick, string username)
        {
            RouletteUser player = RouletteStats.GetPlayer(username);

            if (player.GamesPlayed > 0)
            {
                RouletteStats.RecalculateStats();
                returnValues.Add(Utilities.BuildNotice(nick, String.Format("{0}, you were killed {1} times and you have survived {2} games.", username, player.GamesLost, player.GamesWon)));
                returnValues.Add(Utilities.BuildNotice(nick, String.Format("This currently ranks you in place {0} of the highscore list.", RouletteStats.GetHighscorePlace(username))));
            }
            else
            {
                returnValues.Add(Utilities.BuildNotice(nick, String.Format("{0}, you don't have any stats yet.", username)));
            }
        }

        private void PrintRanking(string nick)
        {
            List<RouletteUser> ranking = RouletteStats.GetHighscoreList();
            RouletteUser player = null;
            // show only top 10
            int playerCount = ranking.Count > highscoreCount ? highscoreCount : ranking.Count;
            for (int i = 0; i < playerCount; i++)
            //int i = 0;
            //int j = 0;
            //while (i < playerCount && j < ranking.Count)
            {
                player = ranking[i];
                //player = ranking[j];
                //j++;
                //if (!player.IsRanked)
                //{
                //    continue;
                //}
                returnValues.Add(Utilities.BuildNotice(nick, String.Format("{0}. {1} ({2} games played, {3:00.00}% won)", i + 1, player.Username, player.GamesPlayed, player.SurvivalRatio * 100)));
                //i++;
            }
        }


        public void Initialize(IIRCBotHandler botCore)
        {
            bot = botCore;
            keywords.Clear();
            keywords.Add("!roulette");
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            returnValues.Clear();

            switch (parameters)
            {
                case "!roulette":
                    if (!running)
                    {
                        StartRoulette(user);
                    }
                    else
                    {
                        PlayRoulette(user);
                    }
                    break;
                case "!roulette help":
                    PrintHelp(user);
                    break;
                case "!roulette stats":
                    PrintStats(nick, user);
                    break;
                case "!roulette ranking":
                    PrintRanking(nick);
                    break;
            }

            return returnValues;
        }
        public void Refresh()
        {

        }

        public List<string> DumpToFile()
        {
            return RouletteStats.Dump();
        }

        public void LoadFromFile(List<string> fileDump)
        {
            RouletteStats.Load(fileDump);
        }

        #endregion
    }

    [Serializable]
    public class RouletteUser : IComparable<RouletteUser>
    {
        private string username;

        public string Username
        {
            get { return username; }
            set { username = value; }
        }

        private int gamesLost;

        public int GamesLost
        {
            get { return gamesLost; }
            set { gamesLost = value; }
        }

        private int gamesWon;

        public int GamesWon
        {
            get { return gamesWon; }
            set { gamesWon = value; }
        }


        private int gamesPlayed;

        public int GamesPlayed
        {
            get { return gamesPlayed; }
            set { gamesPlayed = value; }
        }


        private int gamesStarted;

        public int GamesStarted
        {
            get { return gamesStarted; }
            set { gamesStarted = value; }
        }

        public double SurvivalRatio 
        {
            get
            {
                return (double)this.GamesWon / (double)this.GamesPlayed;
            }
        }

        public double RankingValue
        {
            get
            {
#warning wir brauchen hier ne vernuenftige berechnung ;)
                //return (this.GamesWon - this.GamesLost) * this.GamesPlayed;
                //return SurvivalRatio * this.GamesPlayed;
                //return ((double)this.GamesWon + 10) / ((double)this.GamesPlayed + 20);
                return SurvivalRatio;
            }
        }

        public bool IsRanked
        {
            get
            {
                return this.GamesPlayed > 15;
            }
        }

        public RouletteUser()
        {
        }

        public RouletteUser(string username)
        {
            this.username = username;
        }

        public void IncPlayCount()
        {
            gamesPlayed++;
        }


        public void IncLostCount()
        {
            gamesLost++;
        }

        public void IncWonCount()
        {
            gamesWon++;
        }

        public void IncStartedCount()
        {
            gamesStarted++;
        }

        public string GetStats()
        {
            return string.Format("{0} started {1} games, was killed {2} times and survived {3} times.", username, gamesStarted, gamesLost, gamesWon);
        }

        #region IComparable<RouletteUser> Members

        public int CompareTo(RouletteUser other)
        {
            //double otherSurvivalRatio = (double)other.GamesWon / (double)other.GamesPlayed;
            //return otherSurvivalRatio.CompareTo(SurvivalRatio);
            return other.RankingValue.CompareTo(RankingValue);
        }

        #endregion
    }

    public static class RouletteStats
    {
        private static Dictionary<string, RouletteUser> players;
        private static List<RouletteUser> playerList;

        static RouletteStats()
        {
            players = new Dictionary<string, RouletteUser>();
            playerList = new List<RouletteUser>();
        }

        private static string filename;

        public static RouletteUser GetPlayer(string username)
        {
            if (players.ContainsKey(username))
            {
                return players[username];
            }
            else
            {
                RouletteUser player = new RouletteUser(username);
                players.Add(username, player);
                return player;
            }
        }

        public static void RecalculateStats()
        {
            try
            {
                playerList.Clear();
                foreach (RouletteUser p in players.Values)
                {
                    if (p.GamesPlayed > 0 && p.IsRanked)
                    {
                        playerList.Add(p);
                    }
                }
                playerList.Sort();
            }
            catch (Exception e)
            {
            }
        }

        public static List<RouletteUser> GetHighscoreList() {
            RecalculateStats();
            return playerList;
        }

        public static int GetHighscorePlace(string username)
        {
            RouletteUser player = GetPlayer(username);
            if (player != null)
            {
                return playerList.IndexOf(player) + 1;
            }
            else
            {
                return -1;
            }
        }

        public static void GamePlayed(string username)
        {
            GetPlayer(username).IncPlayCount();
        }

        public static void GameLost(string username)
        {
            GetPlayer(username).IncLostCount();
        }

        public static void GameWon(string username)
        {
            GetPlayer(username).IncWonCount();
        }

        public static void GameStarted(string username)
        {
            GetPlayer(username).IncStartedCount();
        }

        internal static List<string> Dump()
        {
            List<string> returnList = new List<string>();

            foreach (RouletteUser player in players.Values)
            {
                returnList.Add(String.Format("{0};{1};{2};{3};{4}", player.Username, player.GamesLost, player.GamesWon, player.GamesPlayed, player.GamesStarted));
            }

            return returnList;
        }

        internal static void Load(List<string> list)
        {
            players.Clear();

            foreach (string s in list)
            {
                string[] playerData = s.Split(';');
                RouletteUser player = new RouletteUser(playerData[0]);
                player.GamesLost = int.Parse(playerData[1]);
                player.GamesWon = int.Parse(playerData[2]);
                player.GamesPlayed = int.Parse(playerData[3]);
                player.GamesStarted = int.Parse(playerData[4]);

                players.Add(player.Username, player);
            }
        }
    }
}
