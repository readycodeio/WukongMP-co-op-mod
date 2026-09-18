using ReadyM.SDK.Client.Entities;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public class FixYellowbrowSystem : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Sync.InArea || WukongApi.Entities.LocalMainCharacter is not { } main)
            return;

        foreach (var tamer in WukongApi.Entities.AllTamers)
        {
            if (tamer.Guid == "UGuid.LYS.HuangMei.Big" && tamer is { IsMonsterActive: true, Hp: < 1f })
            {
                if (main.IsDead)
                {
                    // rebirth player
                    main.RebirthInPlace();
                }
            }
        }
    }
}