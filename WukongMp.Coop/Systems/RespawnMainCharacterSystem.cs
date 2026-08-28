using Microsoft.Extensions.Logging;
using WukongMp.Coop.Common;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class RespawnMainCharacterSystem(ILogger logger) : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        var allDead = true;
        var players = 0;

        foreach (var mainCharacter in WukongApi.Sync.AllMainCharacters)
        {
            if (mainCharacter.AreaId != WukongApi.Sync.CurrentAreaId)
                continue;

            players++;

            // count players who are dead and not yet respawning
            allDead &= mainCharacter is
            {
                IsDead: true,
                IsTransformed: false,
                IsRespawning: false,
                WaitingCutsceneId: not Constants.YinTigerChallengeFailedSequenceId,
            };
        }

        if (players == 0)
            return;

        var localMainCharacter = WukongApi.Sync.LocalMainCharacter;
        if (!localMainCharacter.HasValue)
        {
            logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        // if all players are dead, respawn the local player
        if (players > 0 && allDead && !localMainCharacter.Value.IsRespawning)
        {
            logger.LogDebug("All {Players} players are dead, respawning player {Player}", players, WukongApi.Sync.LocalPlayerId);

            var furthestRebirthPoint = WukongApi.Sync.AllMainCharacters
                .Select(mainCharacter => mainCharacter.RebirthPointId)
                .Prepend(0)
                .Max();

            localMainCharacter.Value.RebirthAtShrine(furthestRebirthPoint);
        }
    }
}