using b1;
using HarmonyLib;
using ReadyM.Api.Multiplayer.RPC;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;
using WukongMp.Coop.Common;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop;

public partial class CoopServerRpc : ServerRpcClient
{
    partial void OnBeguilingChant(byte state)
    {
        var evState = (BeguilingChantState)state;

        RunOnGameThread(() =>
        {
            var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());
            foreach (var area in areaActors)
            {
                var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
                if (comp != null)
                {
                    var isActive = evState == BeguilingChantState.Active;
                    var isWarning = evState == BeguilingChantState.Warning;
                    AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "SetIsActive").Invoke(comp, [isActive]);

                    if (isWarning)
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "CheckIsWarning").Invoke(comp, [0f]);
                    }
                    else
                    {
                        AccessTools.Method(typeof(BUS_IntervalTriggerImpl), "ResetNotiedWarning").Invoke(comp, []);
                    }
                }
            }
        });
    }

    partial void OnBossHpScaleConfirm(float scaling, int players)
    {
        RunOnGameThread(() =>
        {
            var percent = (int)(scaling * 100);
            WukongApi.Chat.ShowLocalMessage("Boss HP scaling changed!", FLinearColor.Gray);
            WukongApi.Chat.ShowLocalMessage($"Boss HP is set to {percent}% and multiplied by {players} Players.", FLinearColor.Gray);
            WukongApi.Chat.ShowLocalMessage($"Boss HP is now {percent + percent * (players - 1)}% of base HP.", FLinearColor.Gray);
        });
    }
}