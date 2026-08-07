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

        if (players == 0)
            return;

        var targetScalingPercent = scalingPercent * players;

        ecs.Query<TamerComponent, HpComponent>((ref tamer, ref hp) =>
        {
            if (!tamer.IsBossOrElite)
                return;

            if (hp is { HpMaxBase: 0, Hp: 0 })
                return; // no need to scale if monster is not active

            var currentMultPercent = hp.HpScalingPercent == 0 ? 100 : hp.HpScalingPercent;
            if (targetScalingPercent != currentMultPercent)
            {
                var currentHp = hp.Hp;
                var maxHp = hp.HpMaxBase;

                var scaledMaxHp = maxHp * targetScalingPercent / currentMultPercent;
                var scaledCurrentHp = currentHp * targetScalingPercent / currentMultPercent;

                if (scaledCurrentHp > scaledMaxHp)
                {
                    scaledCurrentHp = scaledMaxHp;
                    logger.LogWarning("Scaled current HP exceeded scaled max HP for {Tamer}. Adjusting current HP to max HP.", tamer.Guid);
                }

                hp.HpMaxBase = scaledMaxHp;
                hp.Hp = scaledCurrentHp;
                hp.HpScalingPercent = targetScalingPercent;

                logger.LogDebug("Scaled {Tamer} HP from {Hp}/{HpMaxBase} to {ScaledHp}/{ScaledHpMaxBase} ({Multiplier}%) for {Players} players", tamer.Guid, currentHp, maxHp, hp.Hp, hp.HpMaxBase, hp.HpScalingPercent, players);
            }
        });
    }
}