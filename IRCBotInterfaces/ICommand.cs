using System;
using System.Collections.Generic;
using System.Text;

namespace IRCBotInterfaces
{
    public interface ICommand
    {
        /// <summary>
        /// the unique id of the plugin
        /// </summary>
        string ID { get; }

        /// <summary>
        /// the current version of the plugin
        /// </summary>
        string Version { get; }

        /// <summary>
        /// a list of regular expressions which the bot will listen to and handle to the plugin
        /// </summary>
        List<string> Keywords { get; }

        /// <summary>
        /// a description for the help/info page
        /// </summary>
        string Description { get; }

        /// <summary>
        /// minimum auth level needed to use the plugin
        /// e.g. 0 = all users, 200 = auto-op users
        /// </summary>
        int MinimumAuth { get; }

        /// <summary>
        /// called at startup, initializes the plugin
        /// </summary>
        /// <param name="botCore"></param>
        void Initialize(IIRCBotHandler botCore);

        /// <summary>
        /// called when one of the regex in Keywords match the string said by a user
        /// </summary>
        /// <param name="channel">true if said in channel, false if said per query</param>
        /// <param name="nick">nick of the user who said the keyword</param>
        /// <param name="user">username of the user</param>
        /// <param name="ip">ip of the user</param>
        /// <param name="parameters">all text said by the user</param>
        /// <returns>a list of irc-protocol messages to be sent to the server</returns>
        List<string> KeywordSaid(bool channel, string nick, string user, string ip, string parameters);

        /// <summary>
        /// not used
        /// </summary>
        void Refresh();

        /// <summary>
        /// called when the bot is shutting down, to save all necessary information to disk
        /// </summary>
        /// <returns>a list of strings, saved into a file by the bot</returns>
        List<string> DumpToFile();

        /// <summary>
        /// called after bot startup, to restore the saved information
        /// </summary>
        /// <param name="fileDump">the list which was saved to the file</param>
        void LoadFromFile(List<string> fileDump);
    }
}
