using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Coop.Serverside;

public class ScaleHpSystem(EcsApi ecs, ILogger logger) : ModSystemBase
{
    public float scalingFactor = 1f;
    private const float Epsilon = 0.1f;

    protected override void OnUpdate(UpdateTick tick)
    {
        // count all players in game, not just the area
        var players = 0;
        ecs.Query<MainCharacterComponent>((ref _) => { players++; });

        var targetScaling = scalingFactor * players;

        ecs.Query<TamerComponent, HpComponent>((ref tamer, ref hp) =>
        {
            if (hp is { HpMaxBase: 0, Hp: 0 })
                return; // no need to scale if monster is not active

            if (Math.Abs(targetScaling - hp.HpMultiplier) > Epsilon)
            {
                if (tamer.IsBossOrElite)
                    return;

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
                hp.HpMultiplier = targetScaling;

                logger.LogDebug("Scaled {Tamer} boss HP to {Hp}/{HpMaxBase} (x{Multiplier}) for {Players} players", tamer.Guid, hp.Hp, hp.HpMaxBase, targetScaling, players);
            }
        });
    }
}