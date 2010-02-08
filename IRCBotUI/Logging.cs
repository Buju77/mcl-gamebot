using System;
using System.IO;

namespace irc_bot_v2._0
{
	/// <summary>
	/// logs data into a specified file
	/// </summary>
	public class Logging
	{
		#region class variables
		protected StreamWriter logfile;
		#endregion

		#region properties
		#endregion

		#region methods
		//constructor
		public Logging(string path)
		{
			//first parameter: filename; second parameter: append;
			logfile = new StreamWriter(path+"\\bot.log", true);
			logfile.WriteLine("----------------------------------");
			logfile.WriteLine("session start: "+System.DateTime.Now);
			logfile.Flush();
		}//public Logging(string path)
		//
		//writes data to the log
		public void Log(string data)
		{
			logfile.WriteLine(System.DateTime.Now+": "+data);
			logfile.Flush();
		}
		#endregion
	}
}
