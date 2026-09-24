using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Services;

[Service]
public sealed partial class ColliderReenabling(ColliderDisableData data)
{
    private const float TickIntervalSeconds = 1; // Check every second
    private float _elapsedTime;

    private void Update()
    {
        if (!WukongApi.Local.IsGameplayLevel)
            return;

        _elapsedTime += Time.DeltaTime;

        if (_elapsedTime < TickIntervalSeconds)
            return;

        data.TryReEnableColliders(_elapsedTime);
        _elapsedTime = 0f;
    }
}