copy .\Plugins\compiled_dlls\*.dll .\IRCBotUI\bin\Debug\plugins /Y
copy .\Plugins\compiled_dlls\*.pdb .\IRCBotUI\bin\Debug\plugins /Y
copy .\Plugins\compiled_dlls\*.conf .\IRCBotUI\bin\Debug\plugins /Y

cd .\IRCBotUI\bin\Debug\
irc_bot_v2.01.exe