using Microsoft.Extensions.Logging;
using ReadyM.SDK.Client.Entities;
using WukongMp.Coop.Common;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class RespawnMainCharacterSystem(IEntities entities, ILogger logger) : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        var allDead = true;
        var players = 0;

        if (WukongApi.Entities.CurrentArea is not { } currentArea)
            return;

        foreach (var mainCharacter in entities.Query<MainCharacter>().InScope(currentArea))
        {
            players++;

            // count players who are dead and not yet respawning
            allDead &= mainCharacter is
            {
                IsDead: true,
                IsTransformed: false,
                IsRespawning: false,
                WaitingSequenceId: not Constants.YinTigerChallengeFailedSequenceId,
            };
        }

        if (players == 0)
            return;

        if (WukongApi.Entities.LocalMainCharacter is not { } main)
            return;

        // if all players are dead, respawn the local player
        if (players > 0 && allDead && !main.IsRespawning)
        {
            logger.LogDebug("All {Players} players are dead, respawning player {Player}", players, main.PlayerId);

            var furthestRebirthPoint = 0;
            foreach (var mainCharacter in entities.Query<MainCharacter>())
            {
                if (mainCharacter.RebirthPointId > furthestRebirthPoint)
                {
                    furthestRebirthPoint = mainCharacter.RebirthPointId;
                }
            }

            main.RebirthAtShrine(furthestRebirthPoint);
        }
    }
}