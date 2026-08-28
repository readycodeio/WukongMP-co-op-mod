using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public class FixYellowbrowSystem : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.InArea || !WukongApi.Sync.LocalMainCharacter.HasValue)
            return;

        foreach (var tamer in WukongApi.Sync.AllTamers)
        {
            if (tamer is { IsMonsterActive: true, Hp: < 1f, Guid: "UGuid.LYS.HuangMei.Big" })
            {
                if (WukongApi.Sync.LocalMainCharacter.Value.IsDead)
                {
                    // rebirth player
                    WukongApi.Sync.LocalMainCharacter.Value.RebirthInPlace();
                }
            }
        }
    }
}