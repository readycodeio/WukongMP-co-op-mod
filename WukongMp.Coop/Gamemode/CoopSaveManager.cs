using System.Diagnostics;
using ArchiveB1;
using b1;
using B1UI.GSSvc;
using BtlB1;
using Microsoft.Extensions.Logging;
using PreludeLib.Compat;
using UnrealEngine.Runtime;
using WukongMp.Coop.Common;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Gamemode;

public sealed class CoopSaveManager(ILogger logger)
{
    public void OnNewGameLoad(UObject worldContext)
    {
        GSGMSvc.ClearAllAutoRunTag();
        if (BGW_GameLifeTimeMgr.Get(worldContext).IsInFSMState(SGI_Global.MainMenu))
        {
            BGW_EventCollection.Get(worldContext).Evt_ResetGameInstanceData(EGameInstanceResetType.StartNewGame);
        }

        BGW_EventCollection.Get(worldContext).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
        {
            ArchiveId = Constants.NewCharacterArchiveId
        });
    }

    public void OnLoadArchive(BGW_GameArchiveMgr __instance, ref ReadArchiveResult __result, int ArchiveId, ref FUStBEDArchivesData? OutArchiveData)
    {
        // Read archive with our co-op save.
        bool startNewGame = false;
        byte[] worldData = [];
        byte[] playerData;

        try
        {
            var timer = Stopwatch.StartNew();
            var worldDownloadTask = WukongApi.Saves.DownloadWorldSaveAsync();
            var playerDownloadTask = WukongApi.Saves.DownloadPlayerSaveAsync();

            var task = Task.WhenAll(worldDownloadTask, playerDownloadTask);
            WukongApi.Local.Wait(task);

            timer.Stop();
            logger.LogInformation("Downloaded world and player save files in {Time} ms", timer.ElapsedMilliseconds);

            if (!worldDownloadTask.Result.HasValue)
            {
                logger.LogInformation("Failed to download world save file from the cloud, will start new game");
                startNewGame = true;
            }
            else
            {
                worldData = worldDownloadTask.Result.Value.Content;
            }

            if (!playerDownloadTask.Result.HasValue)
            {
                logger.LogInformation("Player has no save file in the cloud, using default world save");
                playerData = worldData;
            }
            else
            {
                playerData = playerDownloadTask.Result.Value.Content;
            }
        }
        // NOTE: This is typically going to be AggregateException because we download two blobs in parallel
        catch (Exception ex)
        {
            __result = ReadArchiveResult.FileNotExist;
            OutArchiveData = null;
            return;
        }

        ArchiveFileUnpacked? worldArchiveData;
        ArchiveFileUnpacked? playerArchiveData;

        if (startNewGame)
        {
            var readWorldResult = __instance.ReadArchiveData(Constants.NewCharacterArchiveId, out worldArchiveData, out var archiveCanBeRepaired);
            if (readWorldResult != ReadArchiveResult.Success)
            {
                logger.LogError("ReadArchiveData Failed, Result: {Result}", readWorldResult);
                return;
            }

            playerArchiveData = worldArchiveData;
        }
        else
        {
            // we need to write the data as file to read it
            var worldSaveName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CoopWorldArchiveId);
            var worldSavePath = GSWindowsPlatformSaveGame.GetFileFullName(worldSaveName, __instance.ArchiveWorker.UserId);
            File.WriteAllBytes(worldSavePath, worldData);

            var playerSaveName = GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CoopPlayerArchiveId);
            var playerSavePath = GSWindowsPlatformSaveGame.GetFileFullName(playerSaveName, __instance.ArchiveWorker.UserId);
            File.WriteAllBytes(playerSavePath, playerData);

            var readWorldResult = __instance.ReadArchiveData(Constants.CoopWorldArchiveId, out worldArchiveData, out _);
            if (readWorldResult != ReadArchiveResult.Success)
            {
                logger.LogError("ReadArchiveData Failed, Result: {Result}", readWorldResult);
                return;
            }

            var readPlayerResult = __instance.ReadArchiveData(Constants.CoopPlayerArchiveId, out playerArchiveData, out _);
            if (readPlayerResult != ReadArchiveResult.Success)
            {
                logger.LogError("ReadArchiveData Failed, Result: {Result}", readPlayerResult);
                return;
            }
        }

        OutArchiveData = playerArchiveData.GameArchiveData;

        // Keep only RoleData with player state

        // World data:
        OutArchiveData.LevelArchiveData = worldArchiveData.GameArchiveData.LevelArchiveData;
        OutArchiveData.PersistentECSData = worldArchiveData.GameArchiveData.PersistentECSData;
        OutArchiveData.StateMachineArchiveData = worldArchiveData.GameArchiveData.StateMachineArchiveData;
        OutArchiveData.TaskArchiveData = worldArchiveData.GameArchiveData.TaskArchiveData;
        // Add spells received during player absence
        foreach (var spell in worldArchiveData.GameArchiveData.RoleData.RoleCs.Actor.Progress.SpellList)
        {
            if (!OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Contains(spell))
            {
                OutArchiveData.RoleData.RoleCs.Actor.Progress.SpellList.Add(spell);
            }
        }

        // Set spells from world archive if they are not set in player archive
        var worldSpellItemDict = new Dictionary<SpellType, int>(worldArchiveData.GameArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.ToDictionary(spell => spell.Type, spell => spell.SpellId));
        var spellItemDict = new Dictionary<SpellType, int>(OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.ToDictionary(spell => spell.Type, spell => spell.SpellId));
        foreach (var (worldSpellType, worldSpellId) in worldSpellItemDict)
        {
            if (worldSpellId == 0)
            {
                continue;
            }

            if (spellItemDict.TryGetValue(worldSpellType, out var existingSpellId) && existingSpellId == 0)
            {
                logger.LogDebug("Assigning spell ID {SpellId} to type {SpellType}", worldSpellId, worldSpellType);
                spellItemDict[worldSpellType] = worldSpellId;
            }
            else if (!spellItemDict.ContainsKey(worldSpellType))
            {
                logger.LogDebug("Adding spell ID {SpellId} to type {SpellType}", worldSpellId, worldSpellType);
                spellItemDict.Add(worldSpellType, worldSpellId);
            }
        }

        OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.Clear();
        OutArchiveData.RoleData.RoleCs.Actor.Wear.SpellList.AddRange([.. spellItemDict.Select(kvp => new SpellItem { SpellId = kvp.Value, Type = kvp.Key })]);
        // Add interactions received during player absence
        foreach (var interaction in worldArchiveData.GameArchiveData.RoleData.RoleCs.Interaction.InteractionFuncList)
        {
            if (!OutArchiveData.RoleData.RoleCs.Interaction.InteractionFuncList.Contains(interaction))
            {
                OutArchiveData.RoleData.RoleCs.Interaction.InteractionFuncList.Add(interaction);
            }
        }
    }

    public void OnSaveData(List<byte> inSaveData, string slotName)
    {
        logger.LogInformation("Will upload save to the cloud, Slot: {SlotName}, Size: {Size} Mb", slotName, (inSaveData.Count / (1024.0 * 1024.0)).ToString("F2"));

        var data = inSaveData.ToArray();

        Task.Run(async () =>
        {
            if (WukongApi.Sync.IsMasterClient)
            {
                var worldTimer = Stopwatch.StartNew();
                var uploadedWorld = await WukongApi.Saves.UploadWorldSaveAsync(data);
                LogSuccess(worldTimer, uploadedWorld, "world save");
            }

            var playerTimer = Stopwatch.StartNew();
            var uploadedPlayer = await WukongApi.Saves.UploadPlayerSaveAsync(data);
            LogSuccess(playerTimer, uploadedPlayer, "player save");
        });
    }

    private void LogSuccess(Stopwatch stopwatch, bool success, string name)
    {
        stopwatch.Stop();

        if (success)
        {
            logger.LogInformation("Blob uploaded successfully: {Name} in {Time} ms", name, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogError("Failed to upload blob: {Name} in {Time} ms", name, stopwatch.ElapsedMilliseconds);
        }
    }
}