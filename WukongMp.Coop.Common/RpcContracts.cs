using ReadyM.Api.Multiplayer;

namespace WukongMp.Coop.Common;

[ServerRpcContracts]
public static partial class RpcContracts
{
    [ClientToServer, ServerToClient] public static partial void BeguilingChant(byte state);
}