using System;
using System.IO;

namespace irc_bot_v2._0
{
	/// <summary>
	/// loads the options for the client
	/// the values are provided as properties
	/// </summary>
	
	public class Options
	{
		#region class variables
        protected string ivApplicationPath;        
		//each line of the file is a value here:
		//sorted by line number
		protected string ivServername;
		protected int ivPort;
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


		public string ServerName
		{
            get { return this.ivServername; }
		}
		public int Port
		{
            get { return this.ivPort; }
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
            this.ivServername = myfile.ReadLine();//reads the first line of the file
            this.ivPort = System.Convert.ToInt32(myfile.ReadLine());//second line ...
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
}
