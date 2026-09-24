using Microsoft.Extensions.Logging;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Systems;
using WukongMp.Coop.Common;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Serverside.Systems;

[System]
public partial class BeguilingChantSystem(IEntities entities, RpcHandlers rpc, ILogger logger)
{
    private const float ChantDurationSeconds = 90f;
    private const float WarningLeadSeconds = 9f;

    private BeguilingChantState _state = BeguilingChantState.Inactive;
    private float _phaseTimer = ChantDurationSeconds;
    private int _eligibleLastTick;

    private void Update(Tick tick)
    {
        var eligible = 0;
        foreach (var main in entities.Query<MainCharacter>())
        {
            if (main.BeguilingChantEligible)
            {
                eligible++;
            }
        }

        var previous = _eligibleLastTick;
        _eligibleLastTick = eligible;

        if (eligible == 0)
        {
            if (previous > 0)
            {
                ResetToInactive();
                SendToAll(_state);
            }

            return;
        }

        if (previous == 0)
        {
            // first player entered, start a fresh cycle
            ResetToInactive();
            SendToAll(_state);
            return;
        }

        _phaseTimer -= tick.DeltaTime;

        var next = _state;
        if (_phaseTimer <= 0f)
        {
            _phaseTimer += ChantDurationSeconds;
            next = _state == BeguilingChantState.Active
                ? BeguilingChantState.Inactive
                : BeguilingChantState.Active;
        }
        else if (_state == BeguilingChantState.Inactive && _phaseTimer <= WarningLeadSeconds)
        {
            next = BeguilingChantState.Warning;
        }

        if (next != _state)
        {
            _state = next;
            SendToAll(_state);
        }
        else if (eligible != previous)
        {
            // someone joined or left mid-phase, resync everyone
            SendToAll(_state);
        }
    }

    private void ResetToInactive()
    {
        _state = BeguilingChantState.Inactive;
        _phaseTimer = ChantDurationSeconds;
    }

    private void SendToAll(BeguilingChantState state)
    {
        logger.LogDebug("Sending beguling chant state: {State}", state);

        foreach (var main in entities.Query<MainCharacter>())
        {
            rpc.SendBeguilingChant(main.PlayerId, (byte)state);
        }
    }
}