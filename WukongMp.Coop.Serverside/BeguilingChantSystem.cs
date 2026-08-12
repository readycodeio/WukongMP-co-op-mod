using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Coop.Common;

namespace WukongMp.Coop.Serverside;

public class BeguilingChantSystem(EcsApi ecs, RpcHandlers rpc, ILogger logger) : ModSystemBase
{
    private const float ChantDurationSeconds = 90f;
    private const float WarningLeadSeconds = 9f;

    private BeguilingChantState state = BeguilingChantState.Inactive;
    private float phaseTimer = ChantDurationSeconds;
    private int eligibleLastTick;

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

        var previous = eligibleLastTick;
        eligibleLastTick = eligible;

        if (eligible == 0)
        {
            if (previous > 0)
            {
                ResetToInactive();
                SendToAll(state);
            }
            return;
        }

        if (previous == 0)
        {
            // first player entered, start a fresh cycle
            ResetToInactive();
            SendToAll(state);
            return;
        }

        phaseTimer -= tick.deltaTime;

        var next = state;
        if (phaseTimer <= 0f)
        {
            phaseTimer += ChantDurationSeconds;
            next = state == BeguilingChantState.Active
                ? BeguilingChantState.Inactive
                : BeguilingChantState.Active;
        }
        else if (state == BeguilingChantState.Inactive && phaseTimer <= WarningLeadSeconds)
        {
            next = BeguilingChantState.Warning;
        }

        if (next != state)
        {
            state = next;
            SendToAll(state);
        }
        else if (eligible != previous)
        {
            // someone joined or left mid-phase, resync everyone
            SendToAll(state);
        }
    }

    private void ResetToInactive()
    {
        state = BeguilingChantState.Inactive;
        phaseTimer = ChantDurationSeconds;
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