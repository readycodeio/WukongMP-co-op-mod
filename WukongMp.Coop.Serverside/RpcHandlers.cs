using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using WukongMp.Coop.Common;
using WukongMp.Coop.Serverside.Services;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Serverside;

[RpcHandlersFor(typeof(CoopRpcContracts))]
public partial class RpcHandlers(BossHpScaling hpScaling, IEntities entities)
{
    partial void OnScaleBossHp(RpcContext context, int scalingPercent)
    {
        hpScaling.ScalingPercent = scalingPercent;

        var players = 0;

        foreach (var _ in entities.Query<MainCharacter>())
            players++;

        foreach (var main in entities.Query<MainCharacter>())
            SendBossHpScaleConfirm(main.PlayerId, scalingPercent, players);
    }
}