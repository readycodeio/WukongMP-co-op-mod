using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Patches;

[HarmonyPatch(typeof(CharacterAttrDataInitTemplate), nameof(CharacterAttrDataInitTemplate.InitDataPreBeginPlay))]
[HarmonyPatchCategory(PatchCategory.Connected)]
public static class PatchTamerStatResetOnBeginPlay
{
    public static void Postfix(AActor ___Owner)
    {
        if (___Owner is not BGU_CharacterAI ai)
            return;

        var tamer = ai.GetTamerOwner();

        if (tamer.IsNullOrDestroyed())
            return; // no tamer
        
        if (WukongApi.Sync.GetTamerEntityByActor(tamer) is not {} tamerEntity)
            return; // not found

        if (tamerEntity.Owner != WukongApi.Sync.LocalPlayerId)
            return; // not owned

        tamerEntity.HpScalingPercent = 100;
    }
}