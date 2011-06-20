using System;
using System.IO;
using System.Collections.Generic;

namespace irc_bot_v2._0
{
	/// <summary>
	/// loads the options for the client
	/// the values are provided as properties
	/// </summary>
	public class Options
	{
		#region class variables
        protected int ivCurrentServerIdx;
        protected string ivApplicationPath;        
		//each line of the file is a value here:
		//sorted by line number
		protected string ivNickname;
		protected string ivUsername;
		protected string ivFullname;
		protected string ivChannel;
        protected string ivLang;
		protected string ivOwner;
        protected string ivPluginDir;

        private static Options isInstance;
		#endregion

		#region properties
        public string ApplicationPath
        {
            get { return this.ivApplicationPath; }
        }
        public string UserInfoFileName
        {
            get { return "userinfo"; }
        }
        public string CommandsFileName
        {
            get { return "commands"; }
        }

        public ServerConnection ActiveServer
        {
            get
            {
                return this.Servers[this.ivCurrentServerIdx];
            }
        }
        internal ServerConnection MoveToNextServer()
        {
            this.ivCurrentServerIdx = (this.ivCurrentServerIdx + 1) % this.Servers.Count;
            return this.ActiveServer;
        }
        public List<ServerConnection> Servers
        {
            get;
            protected set;
        }
		public string Nickname
		{
            get { return this.ivNickname; }
            set { ivNickname = value; }
		}
		public string Username
		{
            get { return this.ivUsername; }
		}
		public string Fullname
		{
            get { return this.ivFullname; }
		}
		public string Channel
		{
            get { return this.ivChannel; }
		}
        public string Language
        {
            get { return this.ivLang; }
        }
        public string Owner
        {
            get { return this.ivOwner; }
        }
        public string PluginDir
        {
            get { return this.ivPluginDir; }
        }
        #endregion

        #region methods
        //constructor
        public static Options GetInstance()
        {
            if (isInstance == null)
            {
                isInstance= new Options();
            }            
            return isInstance;
        }

		private Options()
        {
#if PocketPC
            this.ivApplicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().ManifestModule.FullyQualifiedName);
#else
            this.ivApplicationPath = Environment.CurrentDirectory;
#endif

            StreamReader myfile = new StreamReader(this.ivApplicationPath + "\\opts.conf");//opens the file

            this.Servers = new List<ServerConnection>();
            var serverString = myfile.ReadLine();
            while (serverString != "---")
            {
                var tmp = serverString.Split(':');
                var serv = new ServerConnection
                {
                    Hostname = tmp[0],
                    Port = Convert.ToInt32(tmp[1])
                };
                this.Servers.Add(serv);
                serverString = myfile.ReadLine();
            }
            this.ivCurrentServerIdx = 0;
            
            this.ivNickname = myfile.ReadLine();//third line ... and so on
            this.ivUsername = myfile.ReadLine();
            this.ivFullname = myfile.ReadLine();
            this.ivChannel = myfile.ReadLine();
            this.ivLang = myfile.ReadLine();
            this.ivOwner = myfile.ReadLine();
            this.ivPluginDir = myfile.ReadLine();
            //add additional options here			
			myfile.Close();//close the file again
		}//public Options(string path)
		#endregion
    }

    public class ServerConnection
    {
        public string Hostname
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }
    }
}
