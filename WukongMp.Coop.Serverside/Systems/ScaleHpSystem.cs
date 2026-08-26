using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Coop.Serverside.Systems;

public class ScaleHpSystem(EcsApi ecs) : ModSystemBase
{
    public int ScalingPercent
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    } = 100;

    protected override void OnUpdate(UpdateTick tick)
    {
        // count all players in game, not just the area
        var players = 0;
        ecs.Query<MainCharacterComponent, int>(ref players, static (ref _, ref p) => { p++; });

        if (players == 0)
            return;

        var targetScalingPercent = ScalingPercent * players;
        ecs.Query<TamerComponent, HpComponent, int>(ref targetScalingPercent, static (ref tamer, ref hp, ref targetScalingPercent) =>
        {
            if (!tamer.IsBossOrElite)
                return;

            if (hp is { HpMaxBase: 0, Hp: 0 })
                return; // no need to scale if monster is not active

            if (tamer.Guid == "UGuid.HFS.Niu.Teacher")
                return; // Bullguard's cutscene is a softlock if he has scaled HP

            var currentMultPercent = hp.HpMaxMulPercent == 0 ? 100 : hp.HpMaxMulPercent;
            if (targetScalingPercent != currentMultPercent)
            {
                var newHp = hp.Hp * ((float)targetScalingPercent / currentMultPercent);

                hp.HpMaxMulPercent = targetScalingPercent;
                hp.Hp = newHp;
            }
        });
    }
}