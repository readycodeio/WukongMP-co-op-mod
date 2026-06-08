using b1;
using CSharpModBase;
using HarmonyLib;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.RPC;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.WukongUtils;
using WukongMp.Coop.Common;

namespace WukongMp.Coop;

public partial class CoopServerRpc(IRpcClient rpc) : ServerRpcClientBase(rpc)
{
    partial void OnBeguilingChant(byte state)
    {
        Utils.TryRunOnGameThread(() =>
        {
            var evState = (BeguilingChantState)state;

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
}