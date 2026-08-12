#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod output.
#
# MakeModFolder.ps1 produces two things:
#   Output/mods/WukongMp.Coop   the client mod folder, dropped into the game's Mods/ folder
#   Output/server_mods          loose files, dropped into the server's server_mods/ folder

# Project folder names. The client mod folder in Output takes its name from $clientProject.
$clientProject = "WukongMp.Coop"
$serverProject = "WukongMp.Coop.Serverside"

# Copied from the client build folder (WukongMp.Coop/bin/<Configuration>/netstandard2.0)
# into the client mod folder
$clientBuildFiles = @(
    "WukongMp.Coop.dll",
    "WukongMp.Coop.Common.dll"
)

# Copied from the "Content" folder into the client mod folder root
$contentFiles = @(
    "manifest.json",
    "ArchiveSaveFile.1.sav" # Prologue save files for starting a new game
)

# Copied from the server build folder (WukongMp.Coop.Serverside/bin/<Configuration>/net10.0)
# into server_mods. Server mods have no folder of their own, every file sits next to
# the SDK's own server mods, so only ship what is yours.
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
