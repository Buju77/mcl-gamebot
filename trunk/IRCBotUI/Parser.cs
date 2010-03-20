using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using IRCBotInterfaces;
using System.Text.RegularExpressions;

namespace irc_bot_v2._0
{
    class Parser
    {
        private const string icDefaultIP = "noip";
        private string ivCurrentIP;

        private IRCBot ivParent;
        private List<ICommand> ivPlugins;

        private string ivTopic;

        public Parser(IRCBot parent, List<ICommand> pluginList)
        {
            this.ivCurrentIP = icDefaultIP;

            this.ivParent = parent;

            SetPluginList(pluginList);
        }

        public void SetPluginList(List<ICommand> pluginList)
        {
            this.ivPlugins = pluginList;

            Console.WriteLine();
            foreach (ICommand plugin in this.ivPlugins)
            {
                try
                {
                    plugin.Initialize(this.ivParent);
                    Program.Out(string.Format("#### PLUG-IN: The plug-in '{0}' (Version: {1}) successful loaded.", plugin.ID, plugin.Version));
                }
                catch (Exception exx) {
                    Program.Out(string.Format("#### PLUG-IN: Loading the plug-in '{0}' (Version: {1}) FAILED!\n#### PLUG-IN Error: {2}", plugin.ID, plugin.Version, exx.Message));
                }
            }
            Console.WriteLine();
        }

/*        private void Refresh()
        {
            Dictionary<string, List<string>> pluginDump = new Dictionary<string, List<string>>();

            foreach (ICommand plugin in this.ivPlugins)
            {
                try
                {
                    pluginDump.Add(plugin.ID, plugin.DumpToFile());
                }
                catch { }

            }
            this.ivPlugins = this.ivParent.LoadPlugins();
            foreach (ICommand plugin in this.ivPlugins)
            {
                try
                {
                    plugin.Initialize(this.ivParent);
                    if (pluginDump.ContainsKey(plugin.ID))
                    {
                        plugin.LoadFromFile(pluginDump[plugin.ID]);
                    }
                }
                catch { }
            }

            this.ivParent.SimpleCommands.Refresh();
        }
 */

        public void ThreadStart()
        {
            while (true)
            {
                try
                {
                    if (this.ivParent.IncomingMessageQueueCount > 0)
                    {
                        this.Parse(this.ivParent.FirstIncomingMessage);
                    }//if
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    Program.Out("parser speaking:");
                    Program.Out(e.ToString());
                }
            }//while (true)
        }

        public void LoadState(string filename)
        {
            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(filename, out fileSuccess, out fileErrorMsg);
            if (fileSuccess)
            {
                List<string> dumpContent = file.GetAllValues();

                Dictionary<string, List<string>> dumpPerPlugin = new Dictionary<string, List<string>>();

                int i = 0;
                int plStart = 0;

                bool save = false;
                while (i < dumpContent.Count)
                {
                    if (dumpContent[i].StartsWith("###")) { save = true; }
                    if (i == dumpContent.Count - 1) { i++; save = true; }

                    if (save)
                    {
                        if (plStart + 1 < i)
                        {
                            List<string> tmp = new List<string>();
                            for (int j = plStart + 2; j < i; j++)
                            {
                                tmp.Add(dumpContent[j]);
                            }
                            dumpPerPlugin.Add(dumpContent[plStart + 1], tmp);
                        }
                        plStart = i;
                        save = false;
                    }
                    i++;
                }

                foreach (ICommand plug in this.ivPlugins)
                {
                    if (dumpPerPlugin.ContainsKey(plug.ID))
                    {
                        try
                        {
                            plug.LoadFromFile(dumpPerPlugin[plug.ID]);
                        }
                        catch (NotImplementedException)
                        {
                            Program.Out(string.Format("### Warning in plugin: '{0}' (Version {1}) ###\tLoadFromFile() not implemented!", plug.ID, plug.Version));
                        }
                        catch (Exception exx)
                        {
                            Program.Out(string.Format("### ERROR in plugin: '{0}' (Version {1}) ###\tLoadFromFile()", plug.ID, plug.Version));
                            Program.Out(exx.Message);
                        }
                    }
                }
            }
            else
            {
                Program.Out("PARSER: could not load plugin-states: " + fileErrorMsg);
            }
        }

        public void SaveState(string filename)
        {
            List<string> dumpContent = new List<string>();
            foreach (ICommand plug in this.ivPlugins)
            {
                try
                {
                    dumpContent.Add("### plugin with id: " + plug.ID + " ###");
                    dumpContent.Add(plug.ID);
                    List<string> temp = plug.DumpToFile();
                    if (temp == null)
                    {
                        // 2010-03-20 Buju: to prevent NullReferenceException
                        Program.Out(string.Format("### Warning in plugin: '{0}' (Version {1}) ###\tDumpToFile() is NULL!", plug.ID, plug.Version));
                        temp = new List<string>();
                    }
                    dumpContent.AddRange(temp);
                }
                catch (NotImplementedException)
                {
                    Program.Out(string.Format("### Warning in plugin: '{0}' (Version {1}) ###\tDumpToFile() not implemented!", plug.ID, plug.Version));
                }
                catch (Exception exx)
                {
                    Program.Out(string.Format("### ERROR in plugin: '{0}' (Version {1}) ###\tDumpToFile()", plug.ID, plug.Version));
                    Program.Out(exx.Message);
                }
            }

            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(filename, out fileSuccess, out fileErrorMsg);
            if (!fileSuccess || !file.SetAllValues(dumpContent, out fileErrorMsg))
            {
                Program.Out("PARSER: could not save plugin-states: " + fileErrorMsg);
            }
        }

        private void Parse(string message)
        {
            if (message.StartsWith("PING"))
            {//respond to the ping from the server. avoids ping timeout
                this.ivParent.AddOutgoingMessage("PONG " + message.Substring(6));
                this.ivParent.ResetConnChk();
            }//if (in_msg.StartsWith("PING"))

            else if (message.StartsWith("SAY"))
            {//input from console
                this.ivParent.AddOutgoingMessage("PRIVMSG " + Options.GetInstance().Channel + " :" + message.Substring(3));
            }//else if (in_msg.StartsWith("SAY"))

            else if (message.StartsWith(":") && !message.StartsWith(":" + Options.GetInstance().ServerName))
            {//any input from other users
                ParseMsgs(message);
            }//else if (in_msg.StartsWith(":") && !in_msg.StartsWith(":" + this.opts.ServerName))

            else if (message.StartsWith(":" + Options.GetInstance().ServerName))
            {//any server messages (login, welcome msg, etc.)
                ParseServerMsgs(message.Substring(Options.GetInstance().ServerName.Length + 2));
            }//else if (in_msg.StartsWith(":" + this.opts.ServerName))
        }

        private void ParseMsgs(string in_completeMsg)
        {
            #region seperate message parts
            string in_nick = ""; string in_name = ""; string in_ip = ""; string in_action = "";
            string in_msg = ""; string in_sender = "";
            int nickEnd = in_completeMsg.IndexOf("!");
            int nameEnd = in_completeMsg.IndexOf("@");
            int ipEnd = in_completeMsg.IndexOf(" ");
            int msgStart = in_completeMsg.IndexOf(":", ipEnd);
            if (nickEnd < 0 || nameEnd < 0 || ipEnd < 0)
            {
                return;
            }//if (nickEnd<0 || nameEnd<0 || ipEnd<0)
            in_nick = in_completeMsg.Substring(1, nickEnd - 1);
            in_name = in_completeMsg.Substring(nickEnd + 1, nameEnd - (nickEnd + 1));
            if (in_name.StartsWith("~"))
            {
                in_name = in_name.Substring(1);
            }

            in_ip = in_completeMsg.Substring(nameEnd + 1, ipEnd - (nameEnd + 1));

            if (msgStart >= 0)
            {
                in_action = in_completeMsg.Substring(ipEnd + 1, msgStart - (ipEnd + 2));
                in_msg = in_completeMsg.Substring(msgStart + 1).Trim();
            }//if (msgStart>=0)
            else
            {
                in_action = in_completeMsg.Substring(ipEnd + 1);
                in_msg = "";
            }//else::if (msgStart>=0)
            if (in_action.Substring(in_action.IndexOf(" ") + 1) == Options.GetInstance().Nickname)
            {
                in_sender = in_nick;
            }
            else
            {
                in_sender = Options.GetInstance().Channel;
            }
            #endregion

            #region build seen database

            string mode = string.Empty;
            if (in_action.StartsWith("JOIN"))
            {
                mode = "JOIN";
            }//if (action.StartsWith("JOIN"))
            else if (in_action.StartsWith("QUIT"))
            {
                mode = "QUIT";
            }//else if (action.StartsWith("QUIT"))
            else if (in_action.StartsWith("KICK"))
            {
                mode = "KICK";
            }//else if (action.StartsWith("KICK"))
            else if (in_action.StartsWith("NICK"))
            {
                mode = "NICK";
            }//else if (action.StartsWith("NICK"))

            if (in_sender == Options.GetInstance().Channel)
            {
                this.ivParent.Users.SeenUser(in_nick, in_name, in_ip, DateTime.Now.ToString() + "@" + mode + in_msg);
            }
            #endregion//build database for seen command

            #region PRIVMSG
            if (in_action.StartsWith("PRIVMSG"))
            {
                if (this.ivParent.Users.GetUserIgnored(in_name)) { return; }

                if (in_msg.StartsWith("$") && in_sender != Options.GetInstance().Channel && this.ivParent.Users.GetUserAuth(in_name) >= 1000)
                {
                    ParseOwnerCommands(in_msg, in_sender);
                }//if (in_msg.StartsWith("$") && this.ivParent.Users.GetUserAuth(in_name) >= 1000)

                else if (in_msg.Length >= 7 && in_msg.ToLower().StartsWith("!seen "))
                {
                    string seen_user = in_msg.Substring(6);
                    if (seen_user.Length > 15) { seen_user = seen_user.Substring(0, 15); }

                    if (seen_user.Equals("/me") || seen_user.Equals(in_nick) ||
                        seen_user.Equals("me") || seen_user.Equals("myself"))
                    {
                        this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(in_sender, Translator.Translate("looking for yourself?")));
                        return;
                    }
                    else if (seen_user.Equals(Options.GetInstance().Nickname))
                    {
                        this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(in_sender, Translator.Translate("stop bugging me.")));
                        return;
                    }

                    string lastSeen = this.ivParent.Users.GetUserLastSeen(seen_user);
                    if (String.IsNullOrEmpty(lastSeen))
                    {
                        this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(in_sender, seen_user + " " + Translator.Translate("was not seen yet.")));
                        return;
                    }
                    else
                    {
                        int timeEnd = lastSeen.IndexOf("@");
                        string time = lastSeen.Substring(0, timeEnd);
                        string span = Convert.ToString(DateTime.Now - Convert.ToDateTime(time));
                        string lastMsg = lastSeen.Substring(timeEnd + 1);

                        string answer = string.Empty;
                        if (lastMsg.IndexOf("ACTION") >= 0)//user /me
                        {
                            answer = Translator.Translate("doing") + ": " + lastMsg.Substring(8, lastMsg.Length - 10);
                        }//if (lastMsg.IndexOf("ACTION")>=0)//user /me
                        else if (lastMsg.StartsWith("JOIN"))//user joined
                        {
                            answer = Translator.Translate("joining") + " " + Options.GetInstance().Channel;
                        }//else if (lastMsg.StartsWith("JOIN"))//user joined
                        else if (lastMsg.StartsWith("QUIT"))//user left
                        {
                            answer = Translator.Translate("leaving") + " " + Options.GetInstance().Channel + ": " + lastMsg.Substring(4);
                        }//else if (lastMsg.StartsWith("QUIT"))//user left
                        else if (lastMsg.StartsWith("KICK"))
                        {
                            answer = Translator.Translate("being kicked because of") + ": " + ": " + lastMsg.Substring(4);
                        }//else if (lastMsg.StartsWith("KICK"))
                        else//user said something
                        {
                            answer = Translator.Translate("saying") + ": " + lastMsg;
                        }//else user said something

                        string response = String.Format(
                            Translator.Translate("{0} was last seen at {1} ({2} ago)") + " " + answer,
                            seen_user, time, span.Substring(0, span.LastIndexOf("."))
                            );

                        this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(in_sender, response));
                    }
                }//else if (in_msg.Length >= 7 && in_msg.StartsWith("!seen "))

                else
                {
                    bool responded = false;
                    foreach (ICommand plugin in this.ivPlugins)
                    {
                        try
                        {
                            foreach (string keyword in plugin.Keywords)
                            {
                                if (Regex.IsMatch(in_msg, keyword, RegexOptions.IgnoreCase) && plugin.MinimumAuth <= this.ivParent.Users.GetUserAuth(in_name))
                                {
                                    List<string> responses = plugin.KeywordSaid(Options.GetInstance().Channel.Equals(in_sender), in_nick, in_name, in_ip, in_msg);
                                    foreach (string response in responses)
                                    {
                                        this.ivParent.AddOutgoingMessage(response);
                                    }
                                    responded = true;
                                    break;
                                }
                            }
                        } catch (NotImplementedException) {
                            Program.Out("### Warning in plugin: " + plugin.ID + " ###\tKeywordSaid() not implemented!");
                        } catch (Exception exx) {
                            Program.Out("### ERROR in plugin: " + plugin.ID + " ###");
                            Program.Out(exx.Message);
                        }
                        if (responded) { break; }
                    }

                    if (!responded)
                    {
                        string response = this.ivParent.SimpleCommands.GetResponse(in_nick, in_msg);
                        if (!String.IsNullOrEmpty(response))
                        {
                            this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(in_sender, response));
                        }
                    }
                }//else
            }
            #endregion//PRIVMSG

            #region JOIN
            else if (in_action.StartsWith("JOIN"))
            {
                if (this.ivParent.Users.GetUserAuth(in_name) >= 200)
                {
                    this.ivParent.AddOutgoingMessage("MODE " + Options.GetInstance().Channel + " +o " + in_nick);
                }
                this.ivParent.FireUserAction(Utilities.UserAction.Joined, new EventArgs());
            }
            #endregion//JOIN

            #region KICK
            else if (in_action.StartsWith("KICK"))
            {
                if (in_action.StartsWith("KICK " + Options.GetInstance().Channel + " " + Options.GetInstance().Nickname))
                {//if bot is kicked -> rejoin!
                    this.ivParent.AddOutgoingMessage("JOIN " + Options.GetInstance().Channel);
                }//if (action.StartsWith("KICK "+opts.Channel+" "+opts.Nickname))
                else if (in_action.StartsWith("KICK " + Options.GetInstance().Channel + " " + Options.GetInstance().Owner))
                {//if owner is kicked
                    this.ivParent.AddOutgoingMessage("MODE " + Options.GetInstance().Channel + " -o " + in_nick);
                    this.ivParent.Users.UserIgnoreChanged(in_nick, in_name, in_ip, true);
                }
            }
            #endregion//KICK

            #region MODE
            else if (in_action.StartsWith("MODE"))
            {
                Users.UserMode newmode = Users.UserMode.Normal;

                string[] modeusers = in_action.Substring(Options.GetInstance().Channel.Length + 1).Split(' ');
                for (int i = 0; i < modeusers.Length - 1; i++)
                {
                    if (modeusers[0][0] == '+')
                    {
                        if (modeusers[0][i] == 'o')
                        {
                            newmode = Users.UserMode.OP;
                        }
                        else
                        {
                            newmode = Users.UserMode.Voice;
                        }
                    }
                    else
                    {
                        newmode = Users.UserMode.Normal;
                    }
                    this.ivParent.Users.UserModeChanged(modeusers[i], newmode);
                }

                if (in_action.IndexOf(Options.GetInstance().Nickname) >= 0 && in_action.IndexOf('+') >= 0)
                {
                    this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(Options.GetInstance().Channel, Translator.Translate("thanks") + ", " + in_nick));
                }
            }
            #endregion//MODE

            #region TOPIC
            else if (in_action.StartsWith("TOPIC"))
            {
                int tpcStart = in_msg.IndexOf(":");
                this.ivTopic = in_msg.Substring(tpcStart + 1);
            }
            #endregion//TOPIC

            #region NICK
            else if (in_action.StartsWith("NICK"))
            {
            }
            #endregion//NICK

            #region QUIT
            else if (in_action.StartsWith("QUIT"))
            {
                this.ivParent.FireUserAction(Utilities.UserAction.Quit, new EventArgs());
            }
            #endregion//QUIT

            #region PART
            else if (in_action.StartsWith("PART"))
            {
                this.ivParent.FireUserAction(Utilities.UserAction.Part, new EventArgs());
            }
            #endregion//PART
        }

        private void ParseServerMsgs(string message)
        {
            if (message.StartsWith("020"))
            {//first server msg: Please wait while we process your connection
                this.ivParent.AddOutgoingMessage("NICK " + Options.GetInstance().Nickname +
                    "\nUSER " + Options.GetInstance().Username + " 4 " +
                    Options.GetInstance().ServerName + " :" +
                    Options.GetInstance().Fullname);
            }//if (server_msg.StartsWith("020"))

            else if (message.StartsWith("376"))
            {//
                this.ivParent.AddOutgoingMessage("JOIN " + Options.GetInstance().Channel);
            }//else if (server_msg.StartsWith("376"))

            else if (message.StartsWith("366"))
            {//end of names list
                this.ivParent.StartChannelSpecificStuff();
            }//else if (message.StartsWith("366"))

            else if (message.StartsWith("433"))
            {//nickname already in use
                Options.GetInstance().Nickname += "^";
                this.ivParent.AddOutgoingMessage("NICK " + Options.GetInstance().Nickname +
                    "\nUSER " + Options.GetInstance().Username + " 4 " +
                    Options.GetInstance().ServerName + " :" +
                    Options.GetInstance().Fullname);
            }//else if (server_msg.StartsWith(""))

            else if (message.StartsWith("332"))
            {
                int tpcStart = message.IndexOf(":");
                this.ivTopic = message.Substring(tpcStart + 1);
            }

            else if (message.StartsWith("001"))
            {
                this.ivCurrentIP = message.Substring(message.LastIndexOf("@"));
            }

        }

        private void ParseOwnerCommands(string command, string sender)
        {
            if ("$saveusers".Equals(command))
            {
                this.ivParent.Users.SaveUsersToFile(Options.GetInstance().UserInfoFileName);
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, Translator.Translate("userinfo saved")));
            }
            else if ("$loadusers".Equals(command))
            {
                this.ivParent.Users.LoadUsersFromFile(Options.GetInstance().UserInfoFileName);
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, Translator.Translate("userinfo restored")));
            }

            else if ("$dump".Equals(command))
            {
                this.ivParent.Dump();
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, Translator.Translate("dump successful")));
            }
            else if ("$load".Equals(command))
            {
                this.ivParent.Load();
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, Translator.Translate("load successful")));
            }
            else if ("$reloadcmds".Equals(command))
            {
                this.ivParent.SimpleCommands.Refresh();
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, Translator.Translate("cmd reload successful")));
            }

            else if (command.Length > 8 && command.StartsWith("$nick "))
            {
                Options.GetInstance().Nickname = command.Substring(6);
                this.ivParent.AddOutgoingMessage("NICK " + command.Substring(6));
            }

            else if (command.Length > 10 && command.StartsWith("$ignore "))
            {
                this.ivParent.Users.UserIgnoreChanged(command.Substring(8), true);
            }
            else if (command.Length > 12 && command.StartsWith("$unignore "))
            {
                this.ivParent.Users.UserIgnoreChanged(command.Substring(10), false);
            }
            else if (command.Length > 10 && command.StartsWith("$auth "))
            {
                //"auth <username> <int>"

                command = command.Trim();
                
                string[] parts = command.Split(' ');
                if (parts.Length == 3)
                {
                    int prevAuth = this.ivParent.Users.GetUserAuth(parts[1]);

                    int newAuth = 0;
                    try
                    {
                        newAuth = int.Parse(parts[2]);
                    }
                    catch (Exception)
                    {
                        prevAuth = -1;
                    }

                    if (prevAuth != -1)
                    {
                        this.ivParent.Users.SetUserAuth(parts[1], newAuth);

                        string response = String.Format(
                                Translator.Translate("auth of user {0} was changed to {1} (has been {2})"),
                                parts[1], newAuth, prevAuth
                                );
                        this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, response));
                    }
                }
            }

            else if (command.Length > 8 && command.StartsWith("$quit "))
            {
                this.ivParent.AddOutgoingMessage("QUIT :" + command.Substring(6));
            }

            else if ("$list".Equals(command))
            {
                foreach (ICommand plugin in this.ivPlugins)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string key in plugin.Keywords)
                    {
                        sb.Append(key + ", ");
                    }
                    this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, sb.ToString()));
                }
            }

            else if ("$help".Equals(command))
            {
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$saveusers | " + Translator.Translate("save user data to file")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$loadusers | " + Translator.Translate("load user data from file")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$dump | " + Translator.Translate("dump plugin data to file")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$load | " + Translator.Translate("load plugin data from file")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$reloadcmds | " + Translator.Translate("reload simple commands")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$nick <NICK> | " + Translator.Translate("change nick to <NICK>")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$ignore <USERNAME> | " + Translator.Translate("add <USERNAME> to ignore list")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$unignore <USERNAME> | " + Translator.Translate("remove <USERNAME> from ignore list")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$auth <USERNAME> | " + Translator.Translate("change auth of <USERNAME>")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$quit <QUITMSG> | " + Translator.Translate("quit irc with <QUITMSG>")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$list | "+Translator.Translate("list available plugin commands")));
                this.ivParent.AddOutgoingMessage(Utilities.BuildPrivMsg(sender, "$help | " + Translator.Translate("display this help")));
            }
        }
    }
}
