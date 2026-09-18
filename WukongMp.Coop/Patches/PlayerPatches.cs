using b1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Archetypes.Mixins;

namespace WukongMp.Coop.Patches;

[HarmonyPatch(typeof(BUS_QuestDynamicObstacleComp), "DisableCollision")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PatchDisableCollision
{
    public static void Postfix(BUS_QuestDynamicObstacleComp __instance)
    {
        if (!WukongApi.Sync.InArea)
            return;

        var obstacle = __instance.GetOwner();
        WukongApi.Services.Resolve<ColliderDisableData>().PermanentlyDisableCollider(obstacle);
    }
}

[HarmonyPatch(typeof(BUS_TouchWallFeedbackComp), "CheckCanTrigger_HitDynamicObstacleWall")]
[HarmonyPatchCategory(PatchCategory.Connected)]
public class PatchCheckCanTrigger_HitDynamicObstacleWall
{
    public static bool Prefix(BUS_TouchWallFeedbackComp __instance, AActor HitActor)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        var questActor = HitActor as BGU_QuestActor;
        if (questActor == null)
            return true;

        if (questActor.QuestActorType != EQuestActorType.DynamicObstacle)
            return true;

        var player = __instance.GetOwner() as BGUPlayerCharacterCS;
        if (player == null)
            return true;

        if (player != WukongApi.Entities.LocalMainCharacter?.Pawn)
            return true;

        var bossActor = GetClosestBossActor(player, player.GetActorLocation());
        if (bossActor == null)
            return true;

        var bossLocation = bossActor.GetActorLocation();
        if (UBGUSelectUtil.MultiSphereTraceForObjects(player, player.GetActorLocation(), bossLocation, 10, [EObjectTypeQuery.ObjectTypeQuery15], false, out var HitResult) > 0 && HitResult.Any(x => x.HitActor == HitActor))
        {
            Logging.LogDebug("Hit dynamic obstacle wall is between boss and player, disabling collision temporarily");
            WukongApi.Services.Resolve<ColliderDisableData>().DisableCollider(questActor, Config.ColliderDisableTime);
            return false;
        }

        return true;
    }

    private static AActor? GetClosestBossActor(UObject context, FVector position)
    {
        AActor? closestBoss = null;
        var closestDistanceSquared = double.MaxValue;
        var monsters = UGameplayStatics.GetAllActorsOfClass<BGU_CharacterAI?>(context);
        foreach (var monster in monsters)
        {
            if (USharpExtensions.IsNullOrDestroyed(monster))
                continue;

            var info = BGW_GameDB.GetUnitBattleInfoExtendDesc(monster.GetFinalBattleInfoExtendID());

            if (info == null)
                continue;

            if (!(monster.bBossRoomMonster || info.QualityType is EUnitQualityType.NormalBoss or EUnitQualityType.FinalBoss || info.BloodBarType == EBGUBloodBarType.BossBar))
                continue;

            var distanceSquared = FVector.DistSquared2D(monster.GetActorLocation(), position);
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestBoss = monster;
            }
        }

        return closestBoss;
    }
}