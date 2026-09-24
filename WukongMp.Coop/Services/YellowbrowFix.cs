using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Extensions;
using WukongMp.Sdk.Archetypes.Mixins;

namespace WukongMp.Coop.Services;

[Service]
public sealed partial class YellowbrowFix
{
    private void Update()
    {
        if (!WukongApi.Entities.InArea || WukongApi.Entities.LocalMainCharacter is not { } main)
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