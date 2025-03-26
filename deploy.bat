echo deploy script begin

set SteamDir=C:/Program Files (x86)/Steam/steamapps/common
set ServerDir=%SteamDir%/Valheim dedicated server/BepInEx/plugins
set ClientDir=%SteamDir%/Valheim/BepInEx/plugins
set ArtifactsSpec=./%1%2.*

echo build artifacts %ArtifactsSpec%

echo copy outputs to client directory: %ClientDir%
copy /y "%ArtifactsSpec%" "%ClientDir%/"

echo copy outputs to server directory: %ServerDir%
copy /y "%ArtifactsSpec%" "%ServerDir%/"

echo deploy script complete