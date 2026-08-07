using ReadyM.Api.Multiplayer;

namespace WukongMp.Coop.Common;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ServerToClient] public static partial void BeguilingChant(byte state);
    [ClientToServer] public static partial void ScaleBossHp(int scalingPercent);
    [ServerToClient] public static partial void BossHpScaleConfirm(int scalingPercent, int players);
}