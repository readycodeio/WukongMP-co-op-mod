using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Coop.Serverside;

public partial class RpcHandlers(ScaleHpSystem hpScaling, EcsApi ecs) : ServerRpcHandlersBase
{
    partial void OnScaleBossHp(RpcContext context, float scaling)
    {
        hpScaling.scalingFactor = scaling;

        var players = 0;
        ecs.Query<MainCharacterComponent>((ref _) =>
        {
            players++;
        });

        ecs.Query<MainCharacterComponent>((ref player) =>
        {
            SendBossHpScaleConfirm(player.PlayerId, scaling, players);
        });
    }
}