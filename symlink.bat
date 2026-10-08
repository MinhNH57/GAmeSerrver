set PROJECT_PATH=E:\git_KV1\kv1-game-server

mklink /E %PROJECT_PATH%\GameServer\bin\Release\Config %PROJECT_PATH%\game-config\Config
mklink /E %PROJECT_PATH%\GameServer\bin\Release\LuaScripts %PROJECT_PATH%\game-config\LuaScripts
		E
mklink /E %PROJECT_PATH%\GameServer\bin\Debug\Config %PROJECT_PATH%\game-config\Config
mklink /E %PROJECT_PATH%\GameServer\bin\Debug\LuaScripts %PROJECT_PATH%\game-config\LuaScripts
		E
mklink /E %PROJECT_PATH%\GameServer\bin\x64\Debug\Config %PROJECT_PATH%\game-config\Config
mklink /E %PROJECT_PATH%\GameServer\bin\x64\Debug\LuaScripts %PROJECT_PATH%\game-config\LuaScripts
		E
mklink /E %PROJECT_PATH%\GameServer\bin\x64\Release\Config %PROJECT_PATH%\game-config\Config
mklink /E %PROJECT_PATH%\GameServer\bin\x64\Release\LuaScripts %PROJECT_PATH%\game-config\LuaScripts