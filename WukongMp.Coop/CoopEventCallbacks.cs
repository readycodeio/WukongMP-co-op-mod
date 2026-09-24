using b1;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.SDK.Client.Mapping;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;
using WukongMp.Sdk.SDK;

namespace WukongMp.Coop;

public sealed class CoopEventCallbacks(IGameEvents gameEvents, ILogger logger) : IHostedService
{
    public void OnScopeStart()
    {
        gameEvents.OnJoinedArea += OnJoinedAreaHandler;
        gameEvents.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        gameEvents.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
    }

    public void Dispose()
    {
        gameEvents.OnJoinedArea -= OnJoinedAreaHandler;
        gameEvents.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        gameEvents.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
    }

    private static void OnPlayerPawnSpawned(MainCharacter player)
    {
        const string whiteColor = "(R=0.9,G=0.9,B=0.9)";
        player.As<Character>().SetMarkerMessage(player.Nickname.ToString(), whiteColor);
    }

    private static void OnMainCharacterEntityInitialized(MainCharacter player)
    {
        // check if we are in the Pagoda
        var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());
        foreach (var area in areaActors)
        {
            var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
            if (comp != null)
            {
                player.Pull(MainCharacterData.Field.BeguilingChantEligible, comp);
                return;
            }
        }
    }

    private void OnJoinedAreaHandler(AreaId areaId)
    {
        var isFirst = WukongApi.Entities.IsMasterClient;
        logger.LogInformation("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

        if (isFirst)
        {
            // it's enough for 1 player to sync the monsters in the area
            WukongApi.Entities.SyncMonstersInArea();
        }
    }
}