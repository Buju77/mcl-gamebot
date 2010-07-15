using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Policy;

using IRCBotInterfaces;

namespace irc_bot_v2._0
{
    class PluginLoader
    {
        private string ivPluginDir;
        private const string icPluginFilter = "*.dll";
        //private AppDomain ivDllDomain;

        public PluginLoader(string pluginDir)
        {
#if !PocketPC
            AppDomain.CurrentDomain.SetShadowCopyFiles();
#endif

            this.ivPluginDir = pluginDir;
        }

        public List<ICommand> LoadPlugins()
        {
            List<ICommand> result = new List<ICommand>();

            DirectoryInfo dirInfo;
            FileInfo[] files;

            try
            {
                if (!Directory.Exists(this.ivPluginDir))
                {
                    Directory.CreateDirectory(this.ivPluginDir);
                }

                dirInfo = new DirectoryInfo(this.ivPluginDir);
                files = dirInfo.GetFiles(icPluginFilter);
            }//try
            catch (DirectoryNotFoundException dnfE)
            {
                Program.Out("Plugin Directory not found. Message: " + dnfE.Message);
                return result;
            }//catch

            foreach (FileInfo file in files)
            {
                Type[] typesInAssembly = null;
                Assembly assembly = Assembly.LoadFrom(file.FullName);
                try
                {
                    typesInAssembly = assembly.GetTypes();
                }
                catch (Exception e)
                {
                    Program.Out("Error loading assembly '" + assembly.FullName + "'. Message: " + e.Message);
                    continue;
                }

                foreach (Type type in typesInAssembly)
                {
                    List<Type> interfaceList = new List<Type>(type.GetInterfaces());

                    if (interfaceList.Contains(typeof(ICommand)))
                    {
                        object assemblyObj = Activator.CreateInstance(type);
                        result.Add((ICommand)assemblyObj);
                    }
                }//foreach type
            }//foreach file
            
            return result;
        }
    }
}
