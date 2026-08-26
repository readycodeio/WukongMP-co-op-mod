using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Coop.Common;
using WukongMp.Coop.Serverside.Systems;

namespace WukongMp.Coop.Serverside;

[ServerRpcFor(typeof(CoopRpcContracts))]
public partial class RpcHandlers(ScaleHpSystem hpScaling, EcsApi ecs) : ServerRpcHandlersBase
{
    partial void OnScaleBossHp(RpcContext context, int scalingPercent)
    {
        hpScaling.ScalingPercent = scalingPercent;

        var players = 0;
        ecs.Query<MainCharacterComponent>((ref _) =>
        {
            players++;
        });

        ecs.Query<MainCharacterComponent>((ref player) =>
        {
            SendBossHpScaleConfirm(player.PlayerId, scalingPercent, players);
        });
    }
}