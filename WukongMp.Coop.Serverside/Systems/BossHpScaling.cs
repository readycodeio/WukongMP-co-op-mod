using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Coop.Serverside.Systems;

[Service]
public sealed partial class BossHpScaling(IEntities entities)
{
    private const int TickInterval = 250; // ECS ticks every 2ms, so ~twice a second

    /// A reconnect deletes and recreates the player's main character entity, so the count drops for seconds.
    /// Apply a drop only once it outlasts that; an increase applies immediately.
    private const float PlayerLossGraceSeconds = 20f;

    private int _appliedPlayerCount;
    private float? _lowerCountSince;

    public int ScalingPercent
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    } = 100;

    private void Update()
    {
        if (Time.Ticks % TickInterval != 0)
            return;

        // count all players in game, not just the area
        var players = 0;

        foreach (var _ in entities.Query<MainCharacter>())
        {
            players++;
        }
        
        if (players == 0)
            return;

        var targetScalingPercent = ScalingPercent * ResolvePlayerCount(players, Time.Elapsed);

        foreach (var tamer in entities.Query<Tamer>())
        {
            if (!tamer.IsBossOrElite)
                continue;

            // HpMaxBase is 0 in ECS until the owner has reported it.
            if (tamer.IsDead || tamer.HpMaxBase <= 0)
                continue;

            if (tamer.Guid == "UGuid.HFS.Niu.Teacher")
                continue; // Bullguard's cutscene is a softlock if he has scaled HP

            tamer.HpMaxMulPercent = targetScalingPercent;
        }
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