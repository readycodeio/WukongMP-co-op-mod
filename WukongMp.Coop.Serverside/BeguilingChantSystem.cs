using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Coop.Common;

namespace WukongMp.Coop.Serverside;

internal class BeguilingChantSystem(EcsApi ecs, RpcHandlers rpc) : ModSystemBase
{
    private const float ChantDurationMs = 90_000f;
    private const float WarningTimeMs = 9_000f;
    private bool chantActive;
    private bool warningNotified;
    private float chantTimer = ChantDurationMs;

    private int eligibleLastFrame;

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

        var anyEligible = eligible > 0;
        var newEligible = eligible != eligibleLastFrame;

        // disable chant for everybody if all players left the Pagoda area
        if (!anyEligible && chantActive)
        {
            // disable chant
            chantActive = false;
            chantTimer = ChantDurationMs;
            SendToAll(BeguilingChantState.Inactive);
        }
        else if (anyEligible)
        {
            // tick chant timer
            chantTimer -= tick.deltaTime;
            if (!warningNotified && !chantActive && chantTimer < WarningTimeMs)
            {
                warningNotified = true;
                SendToAll(BeguilingChantState.Warning);
            }
            else if (chantTimer < 0f)
            {
                chantActive = !chantActive;
                warningNotified = false;
                chantTimer = ChantDurationMs;
                SendToAll(chantActive ? BeguilingChantState.Active : BeguilingChantState.Inactive);
            }
            else if (newEligible)
            {
                if (eligibleLastFrame == 0 && eligible == 1)
                {
                    // first player entered the chant area, start the timer
                    chantTimer = ChantDurationMs;
                    chantActive = true;
                }

                // someone joined the chant area, resend to sync up
                SendToAll(chantActive ? BeguilingChantState.Active : BeguilingChantState.Inactive);
            }
        }

        eligibleLastFrame = eligible;
    }

    private void SendToAll(BeguilingChantState state)
    {
        ecs.Query<MainCharacterComponent>((ref main) =>
        {
            rpc.SendBeguilingChant(main.PlayerId, (byte)state);
        });
    }
}