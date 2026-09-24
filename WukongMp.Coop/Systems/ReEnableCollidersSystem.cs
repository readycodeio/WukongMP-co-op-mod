using ReadyM.SDK.Attributes;
using ReadyM.SDK.Systems;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

[System]
public partial class ReEnableCollidersSystem(ColliderDisableData data)
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;

    private void Update(Tick tick)
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        _elapsedTime += tick.DeltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        data.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}