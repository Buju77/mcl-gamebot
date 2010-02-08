using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace irc_bot_v2._0
{
    class SimpleCommands
    {
        private string ivFilename;
        private Dictionary<string, string> ivCommands;

        public void LoadFromFile(string filename)
        {
            this.ivFilename = filename;
            Refresh();
        }

        public void Refresh()
        {
            bool fileSuccess;
            string fileErrorMsg;
            FileReader file = new FileReader(this.ivFilename, out fileSuccess, out fileErrorMsg);
            if (fileSuccess)
            {
                List<string> filecontent = file.GetAllValues();

                this.ivCommands = new Dictionary<string, string>();

                foreach (string item in filecontent)
                {
                    int i = item.LastIndexOf("|");
                    if (i < 0 || item.Length <= i + 1) { continue; }
                    string command = item.Substring(0, i);
                    string response = item.Substring(i + 1);
                    this.ivCommands.Add(command, response);
                }
            }
            else
            {
                Program.Out("SIMPLECOMMANDS: could not load commands: " + fileErrorMsg);
            }
        }

        public string GetResponse(string nick, string command)
        {
            foreach (KeyValuePair<string, string> kvPair in this.ivCommands)
            {
                string regex = kvPair.Key.Replace("%nick%", Options.GetInstance().Nickname);
                if (Regex.IsMatch(command, regex, RegexOptions.IgnoreCase))
                {
                    string response = Regex.Replace(command, regex, kvPair.Value, RegexOptions.IgnoreCase);
                    if (response.Contains("%"))
                    {
                        response = response.Replace("%", nick);
                    }
                    return response;
                }
            }
            /*if (this.ivCommands.ContainsKey(command))
            {
                string response = this.ivCommands[command];
                if (response.Contains("%"))
                {
                    response = response.Replace("%", nick);
                }
                return response;
            }*/
            return string.Empty;
        }
    }
}
