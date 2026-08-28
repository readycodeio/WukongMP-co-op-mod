using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Coop.Serverside.Systems;

public class ScaleHpSystem(EcsApi ecs) : ModSystemBase
{
    private const int TickInterval = 250; // ECS ticks every 2ms, so ~twice a second

    /// A reconnect deletes and recreates the player's main character entity, so the count drops for seconds.
    /// Apply a drop only once it outlasts that; an increase applies immediately.
    private const float PlayerLossGraceSeconds = 20f;

    private ulong _tick;
    private int _appliedPlayerCount;
    private float? _lowerCountSince;

    public int ScalingPercent
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    } = 100;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (_tick++ % TickInterval != 0)
            return;

        // count all players in game, not just the area
        var players = 0;
        ecs.Query<MainCharacterComponent, int>(ref players, static (ref _, ref p) => { p++; });

        if (players == 0)
            return;

        var targetScalingPercent = ScalingPercent * ResolvePlayerCount(players, tick.Time);

        ecs.Query<TamerComponent, HpComponent, int>(ref targetScalingPercent, static (ref tamer, ref hp, ref target) =>
        {
            if (!tamer.IsBossOrElite)
                return;

            // HpMaxBase is 0 in ECS until the owner has reported it.
            if (hp.IsDead || hp.HpMaxBase <= 0)
                return;

            if (tamer.Guid == "UGuid.HFS.Niu.Teacher")
                return; // Bullguard's cutscene is a softlock if he has scaled HP

            hp.HpMaxMulPercent = target;
        });
    }

    private int ResolvePlayerCount(int players, float now)
    {
        if (players >= _appliedPlayerCount)
        {
            _lowerCountSince = null;
            _appliedPlayerCount = players;
            return players;
        }

        _lowerCountSince ??= now;

        if (now - _lowerCountSince.Value < PlayerLossGraceSeconds)
            return _appliedPlayerCount;

        _lowerCountSince = null;
        _appliedPlayerCount = players;
        return players;
    }
}