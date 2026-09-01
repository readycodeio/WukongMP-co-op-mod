#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod output.
#
# MakeModFolder.ps1 produces two things:
#   Output/mods/<mod>/client         client DLLs, sent to players
#   Output/mods/<mod>/server         server DLLs, never sent to players
#   Output/mods/<mod>/manifest.json  shared by both sides

# Project folder names. The client mod folder in Output takes its name from $clientProject.
$clientProject = "WukongMp.Coop"
$serverProject = "WukongMp.Coop.Serverside"

# Copied from the client build folder (WukongMp.Coop/bin/<Configuration>/netstandard2.0)
# into the client folder
$clientBuildFiles = @(
    "WukongMp.Coop.dll",
    "WukongMp.Coop.Common.dll"
)

# Copied from the "Content" folder into the mod folder root, next to the manifest
$contentFiles = @(
    "manifest.json",
    "ArchiveSaveFile.1.sav" # Prologue save files for starting a new game
)

# Copied from the server build folder (WukongMp.Coop.Serverside/bin/<Configuration>/net10.0)
# into the server folder.
$serverBuildFiles = @(
    "WukongMp.Coop.Serverside.dll",
    "WukongMp.Coop.Common.dll"
)

# Copied only in Debug builds
$clientDebugBuildFiles = @(
    "WukongMp.Coop.pdb",
    "WukongMp.Coop.Common.pdb"
)

$serverDebugBuildFiles = @(
    "WukongMp.Coop.Serverside.pdb",
    "WukongMp.Coop.Common.pdb"
)
