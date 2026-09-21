using ArchiveB1;
using b1;
using B1UI.GSUI;
using CommB1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Coop.Gamemode;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Patches;

/// Replace Steam save folder with ours.
[HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchWindowsSaveGame
{
    public static bool Prefix(ref string __result, string SlotName)
    {
        if (!SlotName.StartsWith("ArchiveSaveFile"))
            return true;
        
        __result = FPaths.Combine(WukongApi.Files.GetModDirectory<Mod>(), $"{SlotName}.sav");
        return false;
    }
}

/// Load our custom save on new game.
[HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.StartNewGame))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchStartNewGame
{
    public static bool Prefix(UObject WorldContext)
    {
        WukongApi.Services.Resolve<CoopSaveManager>().OnNewGameLoad(WorldContext);
        return false;
    }
}

/// Read the world save and character save data, clear spells and set the birth point.
[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LoadArchive))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchGameArchive
{
    public static void Postfix(BGW_GameArchiveMgr __instance, ref ReadArchiveResult __result, int ArchiveId, ref FUStBEDArchivesData? OutArchiveData)
    {
        if (__result != ReadArchiveResult.Success)
        {
            Logging.LogError("Original readArchiveData Failed, Result: {Result}", __result);
            return;
        }

        if (OutArchiveData == null)
        {
            Logging.LogError("Original OutArchiveData is null");
            return;
        }

        WukongApi.Services.Resolve<CoopSaveManager>().OnLoadArchive(__instance, ref __result, ArchiveId, ref OutArchiveData);
    }
}

[HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.SaveDataToSlot))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchGSWindowsPlatformSaveGame
{
    private static bool Prefix(List<byte> InSaveData, string SlotName, string UserId, ref bool __result)
    {
        if (!WukongApi.Entities.InArea)
            return true;

        if (!SlotName.StartsWith("ArchiveSaveFile"))
            return true; // only handle game save, not settings etc.

        WukongApi.Services.Resolve<CoopSaveManager>().OnSaveData(InSaveData, SlotName);

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.GetLatestArchive))]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchGetLatestArchive
{
    public static bool Prefix(BGW_GameArchiveMgr __instance, ref ArchiveSummaryData? __result)
    {
        ArchiveSummaryData? archiveSummaryData = null;
        List<ArchiveSummaryData> archiveInfoList = (List<ArchiveSummaryData>)AccessTools.Method(typeof(BGW_GameArchiveMgr), "_GetArchiveInfoList").Invoke(__instance, []);
        for (int index = 0; index < archiveInfoList.Count; ++index)
        {
            if (archiveSummaryData == null || archiveInfoList[index].ArchiveId > archiveSummaryData.ArchiveId)
                archiveSummaryData = archiveInfoList[index];
        }

        __result = archiveSummaryData?.Clone();
        return false;
    }
}