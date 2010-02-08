using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

using IRCBotInterfaces;
using System.Diagnostics;

namespace irc_bot_v2._0
{
    class IRCBot : IIRCBotHandler
    {
        private SimpleCommands ivSimpleCommands;
        public SimpleCommands SimpleCommands { get { return this.ivSimpleCommands; } }

        private Parser ivParser;
        
        private Network ivNetwork;

        private Users ivUsers;
        public Users Users { get { return this.ivUsers; } }

        private Logging ivLogger;

        private Timer ivTimer_Second;
        private int ivDumpCounter;
        private int ivConnChkCounter;

        #region IncomingMessageQueue
        private List<string> ivIncomingMessageQueue;
        public int IncomingMessageQueueCount { get { return this.ivIncomingMessageQueue.Count; } }
        public string FirstIncomingMessage
        {
            get
            {
                if (this.ivIncomingMessageQueue.Count > 0)
                {
                    string result = this.ivIncomingMessageQueue[0];
                    this.ivIncomingMessageQueue.RemoveAt(0);
                    return result;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        #endregion

        #region OutgoingMessageQueue
        private List<string> ivOutgoingMessageQueue;
        public void AddOutgoingMessage(string message)
        {
            this.ivOutgoingMessageQueue.Add(message);
        }
        #endregion

        public IRCBot()
        {
            this.ivIncomingMessageQueue = new List<string>();
            this.ivOutgoingMessageQueue = new List<string>();

            this.ivLogger = new Logging(Options.GetInstance().ApplicationPath);

            this.ivUsers = new Users();
            if (File.Exists(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().UserInfoFileName)))
            {
                this.ivUsers.LoadUsersFromFile(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().UserInfoFileName));
            }
            DateTime beginTime = new DateTime(2004, 10, 01, 08, 50, 00);
            this.ivUsers.SeenUser("motivation", "motivation", "127.0.0.1", beginTime.ToString() + "@QUIT<nelson>ha ha!</nelson>");

            #region Start SimpleCommands
            this.ivSimpleCommands = new SimpleCommands();
            if (File.Exists(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().CommandsFileName)))
            {
                this.ivSimpleCommands.LoadFromFile(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().CommandsFileName));
            }
            #endregion

            #region Start Network Connection
            this.ivNetwork = new Network(Options.GetInstance().ServerName, Options.GetInstance().Port);
            if (!this.ivNetwork.Started)
            {
                Program.Out("Connection failed: " + this.ivNetwork.Message);
                this.Reconnect();
            }
            else
            {
                Program.Out(this.ivNetwork.Message);
            }
            #endregion

            #region Start Listener and Responder
            Thread listener = new Thread(new ThreadStart(this.ListenerStart));
            listener.Start();
            listener.IsBackground = true;//the thread stops automatically if the main thread stops
            Thread responder = new Thread(new ThreadStart(this.ResponderStart));
            responder.Start();
            responder.IsBackground = true;//the thread stops automatically if the main thread stops
            #endregion

            #region Start PluginLoader
            List<ICommand> pluginList = this.LoadPlugins();
            #endregion

            #region Start Parser
            this.ivParser = new Parser(this, pluginList);
            if (File.Exists(Path.Combine(Options.GetInstance().ApplicationPath, "parserdump")))
            {
                this.ivParser.LoadState(Path.Combine(Options.GetInstance().ApplicationPath, "parserdump"));
            }
            Thread parse_thread = new Thread(new ThreadStart(this.ivParser.ThreadStart));
            parse_thread.Start();
            parse_thread.IsBackground = true;//the thread stops automatically if the main thread stops
            #endregion
        }

        public void StartBot()
        {
            Program.Out("entering loop: enter 'quit' to exit");
            bool active = true;
            while (active) {
                string userinput = Console.ReadLine();
                if (userinput.Equals("quit")) {
                    Program.Out("saving state ...");
                    this.Dump();
                    this.ivUsers.SaveUsersToFile(Options.GetInstance().UserInfoFileName);
                    active = false;
                    break;
                } else if (userinput.StartsWith("say")) {
                    this.ivIncomingMessageQueue.Add("SAY" + userinput.Substring(4));
                }
            }
            Program.Out("shutting down. cya");
        }

        public void StartChannelSpecificStuff()
        {
            #region Start Timer
            //AutoResetEvent autoEvent = new AutoResetEvent(true);
            TimerCallback timerDelegate = new TimerCallback(timerEvent);
            this.ivTimer_Second = new Timer(timerDelegate, /*autoEvent*/null, 1000, 1000);
            this.ivDumpCounter = 0;
            this.ivConnChkCounter = 0;
            #endregion
        }

        public void Dump()
        {
            this.ivParser.SaveState(Path.Combine(Options.GetInstance().ApplicationPath, "parserdump"));
        }

        public void Load()
        {
            this.ivParser.SetPluginList(this.LoadPlugins());
            this.ivParser.LoadState(Path.Combine(Options.GetInstance().ApplicationPath, "parserdump"));
        }

        /// <summary>
        /// startup function for the thread which listens to the irc-server
        /// </summary>
        protected void ListenerStart()
        {
            try
            {
                string input;
                while (true)
                {
                    input = "";
                    try
                    {
                        input = this.ivNetwork.Receive();
                    }//try
                    catch
                    {
                        this.Reconnect();
                    }//catch
                    if (input != "")
                    {
                        this.ivLogger.Log(">> " + input);
                        Program.Out(">> " + input);
                        this.ivIncomingMessageQueue.Add(input);
                    }//if (input != "")
                    Thread.Sleep(1);
                }//while (true)
            }
            catch (Exception e)
            {
                Program.Out("listener speaking:");
                Program.Out(e.ToString());
            }
        }//public void ListenerStart()

        protected void ResponderStart()
        {
            while (true)
            {
                int queueCount = this.ivOutgoingMessageQueue.Count;
                try
                {
                    if (this.ivOutgoingMessageQueue.Count > 0)
                    {
                        this.ivLogger.Log("<< " + this.ivOutgoingMessageQueue[0]);
                        Program.Out("<< " + this.ivOutgoingMessageQueue[0]);
                        try
                        {
                            this.ivNetwork.Send(this.ivOutgoingMessageQueue[0]);
                        }
                        catch
                        {
                            this.Reconnect();
                        }
                        this.ivOutgoingMessageQueue.RemoveAt(0);
                        Thread.Sleep(249);//to avoid being kicked from server: "excess flood"!
                    }
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    if (queueCount == this.ivOutgoingMessageQueue.Count)
                    {
                        this.ivOutgoingMessageQueue.RemoveAt(0);
                    }
                    Program.Out("responder speaking:");
                    Program.Out(e.ToString());
                }
            }//while (true)
        }

        protected void Reconnect()
        {
            Program.Out("sleeping one minute before reconnect attempt ...");
            Thread.Sleep(60 * 1000);//one minute
            Program.Out("trying to reconnect ...");
            this.ivNetwork.Connect(Options.GetInstance().ServerName, Convert.ToInt32(Options.GetInstance().Port));
        }

        /// <summary>
        /// called every minute by the timer
        /// </summary>
        /// <param in_name="o"></param>
        protected void timerEvent(object o)
        {
            if (this.TimerSecondOccured != null) { this.TimerSecondOccured(this, new EventArgs()); }

            this.ivDumpCounter++;
            if (this.ivDumpCounter >= 60 * 60)
            {
                this.Dump();
                this.ivUsers.SaveUsersToFile(Options.GetInstance().UserInfoFileName);

                Program.Out("");
                Program.Out("#### Dumped Data and User-Info.");
                Program.Out("");

                this.ivDumpCounter = 0;
            }

            this.ivConnChkCounter++;
            if (this.ivConnChkCounter >= 10 * 60)
            {
                this.ivNetwork.Send(Utilities.BuildPrivMsg(Options.GetInstance().Nickname, "keep alive ping"));
                Program.Out("### SENDING KEEP ALIVE TO '" + Options.GetInstance().Nickname + "' ###");
            }
        }//protected void timerEvent(object o)

        internal void FireUserAction(Utilities.UserAction action, EventArgs e)
        {
            if (this.UserActionOccured != null) { this.UserActionOccured(this, action, e); }
        }

        internal List<ICommand> LoadPlugins()
        {
            PluginLoader pLoader = new PluginLoader(Path.Combine(Options.GetInstance().ApplicationPath, Options.GetInstance().PluginDir));
            return pLoader.LoadPlugins();
        }

        internal void ResetConnChk()
        {
            this.ivConnChkCounter = 0;
        }

        #region IIRCBotHandler Members

        public event EventHandler TimerSecondOccured;

        public event Utilities.UserActionHandler UserActionOccured;

        public void SendMessage(string message)
        {
            this.ivOutgoingMessageQueue.Add(message);
        }

        public List<string> GetPluginConfig(ICommand plugin)
        {
            string pluginConfigFileName = Path.Combine(Options.GetInstance().PluginDir, plugin.ID + ".conf");
            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(pluginConfigFileName, out fileSuccess, out fileErrorMsg);
            if (!fileSuccess)
            {
                Program.Out("IRCBot: could not load plugin-config: " + fileErrorMsg);
                return new List<string>();
            }
            else
            {
                return file.GetAllValues();
            }
        }

        public void SetPluginConfig(ICommand plugin, List<string> content)
        {
            string pluginConfigFileName = Path.Combine(Options.GetInstance().PluginDir, plugin.ID + ".conf");
            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(pluginConfigFileName, out fileSuccess, out fileErrorMsg);
            if (!fileSuccess || !file.SetAllValues(content, out fileErrorMsg))
            {
                Program.Out("IRCBot: could not save plugin-config: " + fileErrorMsg);
            }
        }

        public void Print(ICommand sender, string message)
        {
            Program.Out(string.Format("### PLUGIN OUTPUT: '{0}' (Version: {1}): {2}", sender.ID, sender.Version, message));
        }

        public string Channel
        {
            get
            {
                return Options.GetInstance().Channel;
            }
        }

        public string Nickname
        {
            get
            {
                return Options.GetInstance().Nickname;
            }
        }

        public string GetUsername(string nickname)
        {
            return this.ivUsers.GetUsernameForNick(nickname);
        }

        #endregion
    }
}
