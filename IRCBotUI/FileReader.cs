using System;
using System.IO;
using System.Collections.Generic;

namespace irc_bot_v2._0
{
	/// <summary>
	/// provides the lines of a file as methods
	/// info:
	/// + do not leave empty lines in between your data
	///		all data after the empty line will be ignored
	/// + do not use more than 100 lines in a file
	/// </summary>
    
	public class FileReader
	{
		#region class variables
        protected const int MAX_LINES = 1000;
        protected List<string> ivContent;
        protected string ivFilename;
		#endregion

		#region properties
        /// <summary>
        /// returns the number of lines saved in the file.
        /// </summary>
		public int Count
		{
			get	{return ivContent.Count;}
		}
		#endregion

		#region methods
        /// <summary>
        /// creates a new object for reading and writing files
        /// </summary>
        /// <param in_name="filename">path and in_name of file</param>
		public FileReader(string filename, out bool success, out string errorMessage)
		{
            ivFilename = filename;
            success = ReadFile(out errorMessage);
		}//public File(string filename)
		
        /// <summary>
        /// returns the content of line number "idx"
        /// index has to be greater than "0" and less than "contents.Count-1"
        /// </summary>
        /// <param in_name="index">line number</param>
        /// <returns>content of line number "idx"</returns>
		public string GetValueAt(int index)
		{
			if (index>=0 && index<=ivContent.Count-1)
			{
				return System.Convert.ToString(ivContent[index]);
			}//if (index>=0 && index<=contents.Count-1)
			else
			{
				return "";
			}//else::if (index>=0 && index<=contents.Count-1)
		}//public string GetValueAt(int index)

        public List<string> GetAllValues()
        {
            return this.ivContent;
        }
		
        /// <summary>
        /// sets the contents of line number "idx"
        /// </summary>
        /// <param in_name="index">line number</param>
        /// <param in_name="text">new content of line "idx"</param>
        /// <returns>true if successful, false if not</returns>
        public bool SetValueAt(int index, string text, out string errorMessage)
		{
			if (index>=0 && index<=ivContent.Count-1)
			{
				ivContent[index]=text;
                return WriteFile(out errorMessage);
			}//if (index>0 && index<=contents.Count-1)
			else
			{
                errorMessage = "Index out of Range.";
				return false;
			}//else::if (index>0 && index<=contents.Count-1)
		}//public bool SetValueAt(string text)
		
        /// <summary>
        /// adds a new line to the file
        /// </summary>
        /// <param in_name="text">content of the new line</param>
        /// <returns>true if successful, false if not</returns>
        public bool AddValue(string text, out string errorMessage)
		{
			if (ivContent.Count<MAX_LINES-1)
			{
				ivContent.Add(text);
				return WriteFile(out errorMessage);
			}//if (contents.Count<MAX_LINES-1)
			else
			{
                errorMessage = "Too much lines in file.";
				return false;
			}//else::if (contents.Count<MAX_LINES-1)
		}//public bool AddValue(string text)

        public bool SetAllValues(List<string> fileTexts, out string errorMessage)
        {
            this.ivContent = fileTexts;
            if (this.ivContent.Count > MAX_LINES)
            {
                this.ivContent.RemoveRange(MAX_LINES, this.ivContent.Count - MAX_LINES);
                
                errorMessage = "Too much lines in file.";
                return false;
            }
            
            return WriteFile(out errorMessage);
        }

        /// <summary>
        /// refreshes the data without restarting the bot
        /// </summary>
        public bool Refresh(out string errorMessage)
        {
            return ReadFile(out errorMessage);
        }//public void Refresh()
		#endregion

        #region private methods

        private bool WriteFile(out string errorMessage)
        {
            try
            {
                StreamWriter file = new StreamWriter(ivFilename);
                for (int i = 0; i <= ivContent.Count - 1; i++)
                {
                    file.WriteLine(ivContent[i]);
                }//for (int i = 0; i <= contents.Count - 1; i++)
                file.Flush();
                file.Close();

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "An Exception occurred while saving the file. Message: " + e.Message;
                return false;
            }
        }

        private bool ReadFile(out string errorMessage)
        {
            try
            {
                StreamReader file = new StreamReader(ivFilename);//opens the file
                string line = "null";
                int count = 0;
                ivContent = new List<string>(MAX_LINES);
                bool run = true;
                while (run)
                {
                    line = file.ReadLine();
                    if (line == null || line == string.Empty || count >= MAX_LINES - 1)
                    {
                        run = false;
                    }//if (line==null || line.Equals("") || count>=MAX_LINES-1)
                    else
                    {
                        ivContent.Add(line);
                        count++;
                    }//else::if (line==null || line.Equals("") || count>=MAX_LINES-1)
                }//while (run)
                file.Close();

                errorMessage = string.Empty;
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "An Exception occurred while reading the file. Message: " + e.Message;
                return false;
            }
        }

        #endregion
    }
}
