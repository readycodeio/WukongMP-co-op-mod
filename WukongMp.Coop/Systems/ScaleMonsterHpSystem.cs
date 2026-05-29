using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public sealed class ScaleMonsterHpSystem(ILogger logger) : ModSystemBase
{
    
    protected override void OnUpdate(UpdateTick tick)
    {
        var scaling = Config.BossHPModifier;
        var areaPlayers = WukongApi.Sync.AreaPlayers.Count;
        var targetScaling = 1 + scaling * (areaPlayers - 1); // Original formula

        if (Config.BossHPChanged)
        {
            targetScaling = scaling + scaling * (areaPlayers - 1); // Scaling formula
        }

#if DEBUG
        if (Config.ScaleMonsterHpToHalf)
        {
            targetScaling = .5f;
        }
#endif

        foreach (var tamer in WukongApi.Sync.AllTamers)
        {
            if (!tamer.IsMonsterActive)
                continue;

            if (tamer.Owner != WukongApi.Sync.LocalPlayerId)
                continue;

            if (tamer.HpMaxBase.Equals(0f, Constants.FloatComparisonTolerance) && tamer.Hp.Equals(0, Constants.FloatComparisonTolerance))
                continue; // no need to scale if monster is not active

            if (Math.Abs(targetScaling - tamer.HpMultiplier) > Constants.FloatComparisonTolerance)
            {
                if (!tamer.IsBossOrElite)
                    continue;

                var currentHp = tamer.Hp;
                var maxHp = tamer.HpMaxBase;

                ReadyCharacterExtensions.set_HpMaxBase(tamer, maxHp / tamer.HpMultiplier * targetScaling);
                ReadyCharacterExtensions.set_Hp(tamer, currentHp / tamer.HpMultiplier * targetScaling);

                tamer.HpMultiplier = targetScaling;
                logger.LogDebug("Scaled boss HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", tamer.Hp, tamer.HpMaxBase, targetScaling, areaPlayers);
            }
        }
    }
}