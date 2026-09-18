using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Server.Entity;
using WukongMp.Coop.Common;
using WukongMp.Coop.Serverside.Systems;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Serverside;

[ServerRpcFor(typeof(CoopRpcContracts))]
public partial class RpcHandlers(ScaleHpSystem hpScaling, IEntities entities) : ServerRpcHandlersBase
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