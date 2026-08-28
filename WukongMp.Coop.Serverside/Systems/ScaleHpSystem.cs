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

    private readonly Dictionary<int, int> _appliedTargets = [];
    private readonly HashSet<int> _seenEntities = [];

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

        _seenEntities.Clear();

        var seen = _seenEntities;
        var applied = _appliedTargets;

        ecs.QueryWithEntity((ref TamerComponent tamer, ref HpComponent hp, int entityId) =>
        {
            if (!tamer.IsBossOrElite)
                return;

            if (hp is { HpMaxBase: 0, Hp: 0, IsDead: false })
                return; // no need to scale if monster is not active

            if (tamer.Guid == "UGuid.HFS.Niu.Teacher")
                return; // Bullguard's cutscene is a softlock if he has scaled HP

            seen.Add(entityId);

            var hasApplied = applied.TryGetValue(entityId, out var appliedTarget);

            // Re-assert the multiplier whenever the game has drifted away from it, but do NOT touch current HP for
            // that. Only a genuine change of our own target justifies rescaling HP.
            if (hasApplied && appliedTarget == targetScalingPercent)
            {
                if (hp.HpMaxMulPercent != targetScalingPercent)
                    hp.HpMaxMulPercent = targetScalingPercent;

                return;
            }

            // First time we scale this monster, treat whatever it currently reports as the baseline so a boss that
            // spawned already scaled is not scaled twice.
            var previousTarget = hasApplied ? appliedTarget : (hp.HpMaxMulPercent == 0 ? 100 : hp.HpMaxMulPercent);

            if (previousTarget != targetScalingPercent)
            {
                hp.HpMaxMulPercent = targetScalingPercent;
                hp.Hp *= (float)targetScalingPercent / previousTarget;
            }
            else if (hp.HpMaxMulPercent != targetScalingPercent)
            {
                hp.HpMaxMulPercent = targetScalingPercent;
            }

            applied[entityId] = targetScalingPercent;
        });

        PruneAppliedTargets();
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

    private void PruneAppliedTargets()
    {
        if (_appliedTargets.Count == _seenEntities.Count)
            return;

        foreach (var entityId in _appliedTargets.Keys.ToArray())
        {
            if (!_seenEntities.Contains(entityId))
                _appliedTargets.Remove(entityId);
        }
    }
}