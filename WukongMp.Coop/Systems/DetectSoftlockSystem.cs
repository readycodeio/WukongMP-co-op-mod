using Microsoft.Extensions.Logging;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using WukongMp.Api.Resources;
using WukongMp.Coop.Resources;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class DetectSoftlockSystem(IEntities entities, ILogger logger) : ModSystemBase
{
    private readonly HashSet<int> _waitingSequencesIds = [];

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Entities.IsMasterClient)
            return;

        var players = 0;
        _waitingSequencesIds.Clear();

        if (WukongApi.Entities.CurrentArea is not { } currentArea)
            return;

        foreach (var mainCharacter in entities.Query<MainCharacter>().InScope(currentArea))
        {
            players++;

            if (mainCharacter.IsWaitingForSequence)
            {
                _waitingSequencesIds.Add(mainCharacter.WaitingSequenceId);
            }
        }

        if (players == 0)
            return;

        if (WukongApi.Entities.LocalMainCharacter is not { } main)
            return;

        if (players > 0 && _waitingSequencesIds.Count > 1 && !main.IsRespawning)
        {
            logger.LogDebug("Softlock detected");
            WukongApi.Local.ShowInfoMessage(CoopTexts.SoftlockDetected);
        }
    }
}