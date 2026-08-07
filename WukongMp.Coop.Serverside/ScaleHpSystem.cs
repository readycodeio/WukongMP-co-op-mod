using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Coop.Serverside;

public class ScaleHpSystem(EcsApi ecs, ILogger logger) : ModSystemBase
{
    public int scalingPercent = 100;

    protected override void OnUpdate(UpdateTick tick)
    {
        // count all players in game, not just the area
        var players = 0;
        ecs.Query<MainCharacterComponent>((ref _) => { players++; });

        var targetScalingPercent = scalingPercent * players;
        var targetScaling = targetScalingPercent / 100f;

        ecs.Query<TamerComponent, HpComponent>((ref tamer, ref hp) =>
        {
            if (!tamer.IsBossOrElite)
                return;
            
            if (hp is { HpMaxBase: 0, Hp: 0 })
                return; // no need to scale if monster is not active

            if (targetScalingPercent != hp.HpMultiplier)
            {
                var currentHp = hp.Hp;
                var maxHp = hp.HpMaxBase;
                var currentMultiplier = hp.HpMultiplier;

                var scaledMaxHp = maxHp / currentMultiplier * targetScaling;
                var scaledCurrentHp = currentHp / currentMultiplier * targetScaling;

                if (scaledCurrentHp > scaledMaxHp)
                {
                    scaledCurrentHp = scaledMaxHp;
                    logger.LogWarning("Scaled current HP exceeded scaled max HP for {Tamer}. Adjusting current HP to max HP.", tamer.Guid);
                }

                hp.HpMaxBase = scaledMaxHp;
                hp.Hp = scaledCurrentHp;
                hp.HpMultiplier = targetScalingPercent;

                logger.LogDebug("Scaled {Tamer} boss HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", tamer.Guid, hp.Hp, hp.HpMaxBase, targetScaling, players);
            }
        });
    }
}