using Microsoft.Extensions.Logging;
using WukongMp.Api.Resources;
using WukongMp.Coop.Resources;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class DetectSoftlockSystem(ILogger logger) : ModSystemBase
{
    private readonly HashSet<int> _waitingSequencesIds = [];
    
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.IsMasterClient)
            return;

        var players = 0;
        _waitingSequencesIds.Clear();

        foreach (var mainCharacter in WukongApi.Sync.AllMainCharacters)
        {
            if (mainCharacter.AreaId != WukongApi.Sync.CurrentAreaId)
                continue;

            players++;

            if (mainCharacter.IsWaitingForCutscene)
            {
                _waitingSequencesIds.Add(mainCharacter.WaitingCutsceneId);
            }
        }

        if (players == 0)
            return;

        var localMainCharacter = WukongApi.Sync.LocalMainCharacter;
        if (!localMainCharacter.HasValue)
        {
            logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        if (players > 0 && _waitingSequencesIds.Count > 1 && !localMainCharacter.Value.IsRespawning)
        {
            logger.LogDebug("Softlock detected");
            WukongApi.Local.ShowInfoMessage(CoopTexts.SoftlockDetected);
        }
    }
}
