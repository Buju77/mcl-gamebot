/**
 * Author:  Lin Yen-Chia
 * Created: 2007-06-28
 * 
 * Change Log:
 * 
 * Version 1.00: 
        Initial Release
  
 * Version 1.60:
        Bugfix:
            ArgumentOutOfRangeException at too much minutes ... !remind me 1999999999 blub

        Features:
            !remindlist
            !remindlist all
            !remindlist <nick>
  
 * Version 1.62:
        minor bugfixes
  
 * Version 1.65: 
        new command: !remind me 2007-09-28 23:59 blub
                     !remindack (not implemented) coming soon ...
  
 * Version 1.66: 
        Version number aus ID entfernt, weil sonst bei einem update die werte nicht mehr geladen werden aus der Datei!
  
 * Version 1.67: 
        The remind list is now sorted against there reminddate.

 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using IRCBotInterfaces;
using System.Collections;

namespace RemindMe {
    
    public class RemindMe : ICommand {
        #region static Members
        private static int cWaitSeconds = 15;
        private static char[] cDelimeter = { ' ' };
        private static char[] cSaveDelimeter = { '\t' };

        private static string cKeyPattern = @"^!remind (?<who>[^ ]+) (?<when>(([01]?\d|2[0-3]):([0-5]\d))|[\d]+|(\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01]) ([01]?\d|2[0-3]):([0-5]\d))) (?<what>.+)$";
        private static string cKeyPatternRemindList = @"^!remindlist(\s+(?<nick>[^ ]+))?$";
        private static string cKeyPatternRemindAck = @"^!remindack$";
        
        /* 
         * Output strings
         */
        private static string cIwillRemind = @"I will remind {0} at {2} about: {1}";
        private static string cTimesUp = @"{0}, time's up: {1}";
        private static string cSaveFormat = "{0}\t{1}\t{2}";
        private static string cRemindListListBegin = @"There are currently > {0:00} < reminders in my queue.";
        private static string cRemindListListBeginWithNick = @"There are currently > {0:00}/{1:00} < reminders in my queue for {2}.";
        private static string cRemindListListEntry = @"#{0:00} Remind {1} at {2} about: {3}";
        private static string cRemindListEmpty = @"There are currently no reminders in my queue.";
        private static string cTooManyMinutes = @"Too many minutes! Try a lesser number.";
#if DEBUG
        private static string cDateFormat = "yyyy-MM-dd HH:mm:ffffff";
#else
        private static string cDateFormat = "yyyy-MM-dd HH:mm";
#endif
        #endregion

        #region private Members
        private IIRCBotHandler myBot;
        private List<string> ivKeys;
        private List<RemindObject> ivRemindList;
        private int ivTimerCount = 0;
        #endregion

        #region ICommand Members

        public string ID {
            get { return "RemindMe"; }
        }

        public string Version
        {
            get
            {
                return "1.67.2007-08-24";
            }
        }

        public List<string> Keywords {
            get { return ivKeys; }
        }

        public string Description {
            get { return "Reminds someone and prints a list of pending reminders."; }
        }

        public int MinimumAuth {
            get { return 0; }
        }

        public void Initialize(IIRCBotHandler botCore) {
            ivKeys = new List<string>(1);
            ivRemindList = new List<RemindObject>();
            myBot = botCore;
            myBot.TimerSecondOccured += new EventHandler(this.CheckRemind);

            ivKeys.Add(cKeyPattern);
            ivKeys.Add(cKeyPatternRemindList);
            ivKeys.Add(cKeyPatternRemindAck);
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters) {
            List<string> response = new List<string>(1);
            
            string receiver = myBot.Channel;
            if (!channel)
            {
                receiver = nick;
            }

            #region RemindList Command
            if (Regex.IsMatch(parameters, cKeyPatternRemindList, RegexOptions.IgnoreCase))
            {
                receiver = nick;

                lock (ivRemindList)
                {
                    if (ivRemindList.Count == 0)
                    {
                        response.Add(Utilities.BuildPrivMsg(receiver, cRemindListEmpty));
                    }
                    else
                    {
                        int i = 1;
                        Match remindPatternValues = Regex.Match(parameters, cKeyPatternRemindList, RegexOptions.IgnoreCase);
                        string val = remindPatternValues.Groups["nick"].Value;
                        if (val.ToLower() == "all")
                        {
                            response.Add(Utilities.BuildPrivMsg(receiver, string.Format(cRemindListListBegin, ivRemindList.Count)));
                            foreach (RemindObject remObj in ivRemindList)
                            {
                                response.Add(Utilities.BuildPrivMsg(receiver, string.Format(cRemindListListEntry, i++, remObj.Nick, remObj.RemindTime.ToString(cDateFormat), remObj.Message)));
                            }
                        }
                        else
                        {
                            // ... !remindlist --> take current nick
                            // else !remindlist B-1B --> take "B-1B"
                            if (val == string.Empty)
                                val = nick;

                            foreach (RemindObject remObj in ivRemindList)
                            {
                                if (val.Equals(remObj.Nick, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    response.Add(Utilities.BuildPrivMsg(receiver, string.Format(cRemindListListEntry, i++, remObj.Nick, remObj.RemindTime.ToString(cDateFormat), remObj.Message)));
                                }
                            }
                            response.Insert(0, Utilities.BuildPrivMsg(receiver, string.Format(cRemindListListBeginWithNick, i - 1, ivRemindList.Count, val)));
                        }
                    }
                }
                return response;
            }
            #endregion

            #region Remind ACK
            if (Regex.IsMatch(parameters, cKeyPatternRemindAck, RegexOptions.IgnoreCase)) {
                // todo: future release
                response.Add(Utilities.BuildPrivMsg(receiver, "Coming soon ..."));
                return response;
            } 
            #endregion

            Match values = Regex.Match(parameters, cKeyPattern, RegexOptions.IgnoreCase);

#if DEBUG
            for (int i = 0; i < values.Groups.Count; i++) {
                Program.Out("#### PLUG-IN: " + values.Groups[i].Value);
            }
#endif

            string who = values.Groups["who"].Value;
            string when = values.Groups["when"].Value;
//            string whendate = values.Groups["whendate"].Value;
            string what = values.Groups["what"].Value;
            long minutes;
            DateTime remindTime;
            DateTime nowTime = DateTime.Now;
            nowTime = nowTime.AddMilliseconds(-nowTime.Millisecond);
            double totalMS = 0;

            if (DateTime.TryParse(when, out remindTime)) {                               
                // input: 2007-01-21 23:59
                // or: 23:59
                totalMS = ((TimeSpan)(remindTime - nowTime)).TotalMilliseconds;
                
                //
                // remindTime: 18:00
                // currentTime: 19:00 
                // ==> remindTime + 24h
                //
                if (nowTime.TimeOfDay > remindTime.TimeOfDay) {
                    totalMS += 24 * 60 * 60 * 1000;
                }
                
                // occurs very often
                // I will remind myself at 2007-06-28 22:44:999625
                totalMS += 10;

            } else if (Int64.TryParse(when, out minutes)) {
                totalMS = (double) minutes * 60 * 1000.0;
            } else {
                response.Add(Utilities.BuildPrivMsg(receiver, "Wrong syntax!!!"));
                return response;
            }

            if (who.ToLower() == "me")
                who = nick;

            try {
                remindTime = nowTime.AddMilliseconds(totalMS);
                RemindObject ro = new RemindObject(remindTime, who, what);
                ivRemindList.Add(ro);
                ivRemindList.Sort(ro);
                response.Add(Utilities.BuildPrivMsg(receiver, string.Format(cIwillRemind, who, what, remindTime.ToString(cDateFormat))));
            } catch (ArgumentOutOfRangeException) {
                response.Add(Utilities.BuildPrivMsg(receiver, cTooManyMinutes));
            }

            return response;
        }

        public void Refresh() {
            
        }

        public List<string> DumpToFile() {
            lock (ivRemindList) {
                List<string> dump = new List<string>(ivRemindList.Count);
                foreach (RemindObject ro in ivRemindList) {
                    dump.Add(ro.ToString());
                }
                return dump;
            }
        }

        public void LoadFromFile(List<string> fileDump) {
            lock (ivRemindList) {
                ivRemindList.Clear();
                foreach (string str in fileDump) {
                    ivRemindList.Add(new RemindObject(str));
                } 
            }
        }

        #endregion

        #region Event Handlers
        private void CheckRemind(object sender, EventArgs ea) {
            if ((++ivTimerCount) < cWaitSeconds) 
                return;

            ivTimerCount = 0;

            lock (ivRemindList) {
                DateTime nowTime = DateTime.Now;

                for (int i = ivRemindList.Count - 1; i >= 0; i--) {
                    RemindObject remindObj = ivRemindList[i];
                    if (nowTime > remindObj.RemindTime) {
                        //myBot.SendMessage(Utilities.BuildPrivMsg(receiver, string.Format("{0}, time's up ({2}): {1}", remindObj.Nick, remindObj.Message, remindObj.RemindTime.ToString())));
                        myBot.SendMessage(Utilities.BuildPrivMsg(myBot.Channel, string.Format(cTimesUp, remindObj.Nick, remindObj.Message)));
                        ivRemindList.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region class RemindObject
        private class RemindObject : IComparer<RemindObject>
        {
            private DateTime remindTime;
            private string nick;
            private string msg;

            public RemindObject(string s)
            {
                string[] values = s.Split(cSaveDelimeter);
                remindTime = new DateTime(long.Parse(values[0]));
                nick = values[1];
                msg = values[2];
            }
            public RemindObject(DateTime rt, string nickName, string message)
            {
                remindTime = new DateTime(rt.Ticks);
                nick = nickName;
                msg = message;
            }

            public override string ToString() {
                return string.Format(cSaveFormat, remindTime.Ticks, nick, msg);
            }

            #region Properties
            public DateTime RemindTime {
                get { return remindTime; }
                set { remindTime = value; }
            }

            public string Nick {
                get { return nick; }
                set { nick = value; }
            }

            public string Message {
                get { return msg; }
                set { msg = value; }
            }
            #endregion

            #region IComparer<RemindObject> Members

            public int Compare(RemindObject x, RemindObject y) {
                return x.remindTime.CompareTo(y.remindTime);
            }

            #endregion
        }
        #endregion        
    }
}