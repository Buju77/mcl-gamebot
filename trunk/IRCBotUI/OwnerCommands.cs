using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using IRCBotInterfaces;
using System.IO;
using System.Text.RegularExpressions;

namespace irc_bot_v2._0
{
    internal class OwnerCommands : ICommand
    {
        private class Command
        {
            public string Regex;
            public string Info;
            public Func<bool, string[], Match, List<string>> Execute;
            public int Auth = 1000;
        }

        private List<Command> commands;
        private IRCBot bot;

        private string GetSender(bool channel, string nick)
        {
            return channel ? Options.GetInstance().Channel : nick;
        }

        public OwnerCommands(IRCBot bot)
        {
            this.bot = bot;
            this.commands = new List<Command>();

            commands.Add(new Command
            {
                Regex = "^\\$saveusers$",
                Info = Translator.Translate("save user data to file"),
                Execute = (channel, data, match) =>
                {
                    bot.Users.SaveUsersToFile(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().UserInfoFileName));
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("userinfo saved")) };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$loadusers$",
                Info = Translator.Translate("load user data from file"),
                Execute = (channel, data, match) =>
                {
                    bot.Users.LoadUsersFromFile(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().UserInfoFileName));
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("userinfo restored")) };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$dump$",
                Info = Translator.Translate("dump plugin data to file"),
                Execute = (channel, data, match) =>
                {
                    bot.Dump();
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("dump successful")) };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$load$",
                Info = Translator.Translate("load plugin data from file"),
                Execute = (channel, data, match) =>
                {
                    bot.Load();
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("load successful")) };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$reloadcmds$",
                Info = Translator.Translate("reload simple commands"),
                Execute = (channel, data, match) =>
                {
                    bot.SimpleCommands.Refresh();
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("cmd reload successful")) };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$nick (?<nick>[^ ]+)$",
                Info = Translator.Translate("change nick to <NICK>"),
                Execute = (channel, data, match) =>
                {
                    var newNick = data[3].Substring(6);
                    Options.GetInstance().Nickname = newNick;
                    return new List<string> { "NICK " + newNick };
                },
                Auth = 2000
            });
            commands.Add(new Command
            {
                Regex = "^\\$ignore (?<username>[^ ]+)$",
                Info = Translator.Translate("add <USERNAME> to ignore list"),
                Execute = (channel, data, match) =>
                {
                    bot.Users.UserIgnoreChanged(data[3].Substring(8), true);
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("user '" + data[3].Substring(8) + "' will now be ignored")) };
                },
                Auth = 2000
            });
            commands.Add(new Command
            {
                Regex = "^\\$unignore (?<username>[^ ]+) $",
                Info = Translator.Translate("remove <USERNAME> from ignore list"),
                Execute = (channel, data, match) =>
                {
                    bot.Users.UserIgnoreChanged(data[3].Substring(10), false);
                    return new List<string> { Utilities.BuildPrivMsg(GetSender(channel, data[0]), Translator.Translate("user '" + data[3].Substring(10) + "' will now not be ignored anymore")) };
                },
                Auth = 2000
            });
            commands.Add(new Command
            {
                Regex = "^\\$auth (?<username>[^ ]+) (?<auth>[0-9]+)$",
                Info = Translator.Translate("change auth of <USERNAME>"),
                Execute = (channel, data, match) =>
                {
                    var user = match.Groups["username"].Value;
                    var authStr = match.Groups["auth"].Value;

                    int prevAuth = bot.Users.GetUserAuth(user);

                    int newAuth = 0;
                    try
                    {
                        newAuth = int.Parse(authStr);
                    }
                    catch (Exception)
                    {
                        prevAuth = -1;
                    }

                    if (prevAuth != -1)
                    {
                        bot.Users.SetUserAuth(user, newAuth);

                        string response = String.Format(
                                Translator.Translate("auth of user {0} was changed to {1} (has been {2})"),
                                user, newAuth, prevAuth
                                );
                        return new List<string> { Utilities.BuildPrivMsg(this.GetSender(channel, data[0]), response) };
                    }
                    else
                    {
                        return new List<string>();
                    }
                },
                Auth = 2000
            });
            commands.Add(new Command
            {
                Regex = "^\\$quit (?<quitmsg>.+)$",
                Info = Translator.Translate("quit irc with <QUITMSG>"),
                Execute = (channel, data, match) =>
                {
                    return new List<string> { "QUIT :" + data[3].Substring(6) };
                },
                Auth = 2000
            });
            commands.Add(new Command
            {
                Regex = "^\\$list$",
                Info = Translator.Translate("list available plugin commands"),
                Execute = (channel, data, match) =>
                {
                    var result = new List<string>();
                    foreach (ICommand plugin in bot.Parser.Plugins)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (string key in plugin.Keywords)
                        {
                            sb.Append(key + ", ");
                        }
                        result.Add(Utilities.BuildPrivMsg(data[0], sb.ToString()));
                    }
                    return result;
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$opme$",
                Info = Translator.Translate("ops the user"),
                Execute = (channel, data, match) =>
                {
                    return new List<string> { "MODE " + Options.GetInstance().Channel + " +o " + data[0] };
                }
            });
            commands.Add(new Command
            {
                Regex = "^\\$help$",
                Info = Translator.Translate("display this help"),
                Execute = (channel, data, match) =>
                {
                    var result = new List<string>();
                    foreach (var item in this.commands)
                    {
                        result.Add(Utilities.BuildPrivMsg(data[0], item.Regex + " | " + item.Info));
                    }
                    return result;
                }
            });
        }

        #region ICommand Members

        public string ID
        {
            get
            {
                return "OwnerCommands";
            }
        }

        public string Version
        {
            get
            {
                return "0.0";
            }
        }

        public List<string> Keywords
        {
            get
            {
                return this.commands.Select(c => c.Regex).ToList();
            }
        }

        public string Description
        {
            get
            {
                return "built in commands for bot administration";
            }
        }

        public int MinimumAuth
        {
            get
            {
                return 1000;
            }
        }

        public void Initialize(IIRCBotHandler botCore)
        {
        }

        public List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters)
        {
            foreach (var cmd in this.commands)
            {
                var match = Regex.Match(parameters, cmd.Regex);
                if (match.Success && cmd.Auth <= bot.Users.GetUserAuth(user))
                {
                    return cmd.Execute(channel, new[] { nick, user, ip, parameters }, match);
                }
            }
            return new List<string>();
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
