using System;
using System.Collections.Generic;
using System.Text;

namespace IRCBotInterfaces
{
    public interface IIRCBotHandler
    {
        event EventHandler TimerSecondOccured;

        event UserActionEventHandler UserActionOccured;

        void SendMessage(string message);

        List<string> GetPluginConfig(ICommand plugin);
        void SetPluginConfig(ICommand plugin, List<string> content);

        void Print(ICommand sender, string message);

        string Channel { get; }
        string Nickname { get; }

        string GetUsername(string nickname);
    }
}
