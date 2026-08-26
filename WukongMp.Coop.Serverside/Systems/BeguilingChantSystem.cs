using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Coop.Common;

namespace WukongMp.Coop.Serverside.Systems;

public class BeguilingChantSystem(EcsApi ecs, RpcHandlers rpc, ILogger logger) : ModSystemBase
{
    private const float ChantDurationSeconds = 90f;
    private const float WarningLeadSeconds = 9f;

    private BeguilingChantState _state = BeguilingChantState.Inactive;
    private float _phaseTimer = ChantDurationSeconds;
    private int _eligibleLastTick;

    protected override void OnUpdate(UpdateTick tick)
    {
        var eligible = 0;
        ecs.Query<MainCharacterComponent, int>(ref eligible, static (ref main, ref eligible) =>
        {
            if (main.BeguilingChantEligible)
            {
                eligible++;
            }
        });

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
        ecs.Query<MainCharacterComponent>((ref main) =>
        {
            rpc.SendBeguilingChant(main.PlayerId, (byte)state);
        });
    }
}