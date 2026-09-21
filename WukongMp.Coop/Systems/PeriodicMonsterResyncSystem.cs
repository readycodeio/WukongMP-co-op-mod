using WukongMp.Api;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
/// <summary>
/// Periodically rescans monsters in the area to ensure proper synchronization between master and non-master clients.
/// <remarks>Credit: https://github.com/IOAFukua/GameICU.Tools.Plus</remarks>
/// </summary>
public sealed class PeriodicMonsterResyncSystem : ModSystemBase
{
    private const float RescanIntervalSeconds = 60f;

    private bool _wasMaster;
    private float _timer = RescanIntervalSeconds;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Entities.InArea)
        {
            _wasMaster = false;
            return;
        }

        var isMaster = WukongApi.Entities.IsMasterClient;

        if (isMaster && !_wasMaster)
        {
            Logging.LogInformation("Became master client, rescanning monsters in area immediately");
            WukongApi.Entities.SyncMonstersInArea();
            _timer = RescanIntervalSeconds;
        }
        else if (isMaster)
        {
            _timer -= tick.deltaTime;
            if (_timer <= 0f)
            {
                _timer = RescanIntervalSeconds;
                WukongApi.Entities.SyncMonstersInArea();
            }
        }

        _wasMaster = isMaster;
    }
}