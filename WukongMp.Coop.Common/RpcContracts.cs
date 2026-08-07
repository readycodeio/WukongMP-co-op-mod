using ReadyM.Api.Multiplayer;

namespace WukongMp.Coop.Common;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ServerToClient] public static partial void BeguilingChant(byte state);
    [ClientToServer] public static partial void ScaleBossHp(float scaling);
    [ServerToClient] public static partial void BossHpScaleConfirm(float scaling, int players);
}