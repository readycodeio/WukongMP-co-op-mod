using Microsoft.Extensions.Logging;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using WukongMp.Coop.Resources;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Systems;

[System]
public partial class DetectSoftlockSystem(IEntities entities, ILogger logger)
{
    private readonly HashSet<int> _waitingSequencesIds = [];

    private void Update()
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