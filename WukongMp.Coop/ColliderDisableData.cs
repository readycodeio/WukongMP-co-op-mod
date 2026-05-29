using b1;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop;

public sealed class ColliderDisableData(ILogger logger)
{
    private readonly Dictionary<AActor, float> _colliderDisableTimes = []; 

    public void PermanentlyDisableCollider(AActor actor)
    {
        if (_colliderDisableTimes.Remove(actor))
        {
            logger.LogDebug("Permanently disabled collider for actor: {Actor}", BGU_DataUtil.GetActorGuid(actor));
        }
    }

    public void DisableCollider(AActor actor, float disableDuration)
    {
        _colliderDisableTimes[actor] = disableDuration;
        actor.SetActorEnableCollision(false);
    }

    public void TryReEnableColliders(float deltaTime)
    {
        var collidersToEnable = new List<AActor>();
        foreach (var collider in _colliderDisableTimes.Keys.ToList())
        {
            var remainingTime = _colliderDisableTimes[collider] - deltaTime;
            if (remainingTime <= 0f)
            {
                collidersToEnable.Add(collider);
            }
            else
            {
                _colliderDisableTimes[collider] = remainingTime;
            }
        }
        foreach (var collider in collidersToEnable)
        {
            collider.SetActorEnableCollision(true);

            if (WukongApi.Sync.LocalMainCharacter != null)
            {
                var player = WukongApi.Sync.LocalMainCharacter.Value.Pawn;
                var traceLength = player.CapsuleComponent.GetScaledCapsuleRadius() + 20f;
                var lineTraceDir = GetLineTraceDir_SafeNormal2D(player);
                var playerLocation = player.BGUGetActorLocation();
                var startTrace = playerLocation - lineTraceDir * traceLength;
                var endTrace = playerLocation + lineTraceDir * traceLength;
                if (UBGUSelectUtil.MultiSphereTraceForObjects(player, startTrace, endTrace, traceLength, [EObjectTypeQuery.ObjectTypeQuery15], false, out var HitResult) > 0)
                {
                    if (HitResult.Any(x => x.HitActor == collider))
                    {
                        logger.LogDebug("Re-disabled collider for actor: {Actor} due to player proximity", BGU_DataUtil.GetActorGuid(collider));
                        DisableCollider(collider, Config.ColliderDisableTime);
                        continue;
                    }
                }
            }

            _colliderDisableTimes.Remove(collider);
            logger.LogDebug("Re-enabled collider for actor: {Actor}", BGU_DataUtil.GetActorGuid(collider));
        }
    }

    private FVector GetLineTraceDir_SafeNormal2D(BGUCharacterCS playerCharacter)
    {
        if (playerCharacter.CharacterMovement.IsFalling())
        {
            return playerCharacter.GetVelocity().GetSafeNormal2D();
        }

        return playerCharacter.CharacterMovement.GetCurrentAcceleration().GetSafeNormal2D();
    }
}