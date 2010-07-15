/**
 * Author:  Lin Yen-Chia
 * Created: 2008-03-18
 * 
 * Change Log:
 * 
 * Version 1.00.2008-03-18: 
        Initial Release

 */

using System;
using System.Collections.Generic;
using System.Text;
using IRCBotInterfaces;
using System.Text.RegularExpressions;

namespace BotPlugins
{
    public class MyRandom : ICommand
    {
        #region Static Members
        // Pattern
        private static string cKeyRand = @"^!rand";
        private static string cKeyRandom = @"^!random";
        private static string cKeyNumber = @" (?<max>([\d]{1,10}))$";
        private static string cKeyNumber2 = @" (?<min>([\d]{1,10}))-(?<max>([\d]{1,10}))$";
        private static string cKeyRandNumber = cKeyRand + cKeyNumber;
        private static string cKeyRandomNumber = cKeyRandom + cKeyNumber;
        private static string cKeyRandNumber2 = cKeyRand + cKeyNumber2;
        private static string cKeyRandomNumber2 = cKeyRandom + cKeyNumber2;

        // Output Text
        private static string cNickRollsValue = @"{0} rolls: {1}"; 
        #endregion

        #region Members
        private List<string> ivKeys;
        private Random ivRand;
        private IIRCBotHandler myBot; 
        #endregion

        #region ICommand Members

        public string ID
        {
            get { return "MyRandom"; }
        }

        public string Version
        {
            get { return "1.00.2008-03-18"; }
        }

        public List<string> Keywords
        {
            get { return ivKeys; }
        }

        public string Description
        {
            get { return "Generates a Random number -> Usage: !(rand|random), !(rand|random) 100, !(rand|random) 20-100, !(rand|random) <min>-<max>"; }
        }

        public int MinimumAuth
        {
            get { return 0; }
        }

        public void Initialize(IIRCBotHandler botCore)
        {
            ivKeys = new List<string>(1);
            ivKeys.Add(cKeyRand + "$");
            ivKeys.Add(cKeyRandom + "$");
            ivKeys.Add(cKeyRandNumber);
            ivKeys.Add(cKeyRandomNumber);
            ivKeys.Add(cKeyRandNumber2);
            ivKeys.Add(cKeyRandomNumber2); 
            
            myBot = botCore;

            ivRand = new Random();
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            List<string> response = new List<string>(1);
            string receiver = myBot.Channel;
            string minString = string.Empty;
            string maxString = string.Empty;
            int min = 1, max = 100;

            #region Get Min Max String (get int Min Value, if available)
            // match min-max
            if (Regex.IsMatch(parameters, cKeyNumber2, RegexOptions.IgnoreCase))
            {
                Match values = Regex.Match(parameters, cKeyNumber2, RegexOptions.IgnoreCase);
                minString = values.Groups["min"].Value.Trim();
                maxString = values.Groups["max"].Value.Trim();

                // parse min value
                try
                {
                    min = Int32.Parse(minString);
                }
                catch
                {
                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format("Error: '{0}' is too big.", maxString)));
                    return response;
                }
            }
            else
            {
                // !rand|random 100
                Match values = Regex.Match(parameters, cKeyNumber, RegexOptions.IgnoreCase);

                // get max value
                maxString = values.Groups["max"].Value.Trim();
            } 
            #endregion

            #region Get int Max Value
            if (maxString == string.Empty)
            {
                // standard roll (1 - 100)
                max = 100;
            }
            else
            {
                try
                {
                    max = Int32.Parse(maxString);
                }
                catch
                {
                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format("Error: '{0}' is too big.", maxString)));
                    return response;
                }
            } 
            #endregion

            #region Roll
            // <min> can't be greater than <max>
            if (min > max)
            {
                response.Add(Utilities.BuildPrivMsg(receiver, "Error: min < max!"));
            }
            else
            {
                // rolls
                int randValue = ivRand.Next(min, max + 1);
                response.Add(Utilities.BuildPrivMsg(receiver, string.Format(cNickRollsValue, nick, randValue)));
            } 
            #endregion

            return response;
        }

        public void Refresh()
        {
        }

        public List<string> DumpToFile()
        {
            return new List<string>();
        }

        public void LoadFromFile(List<string> fileDump)
        {
        }

        #endregion
    }
}
