using System.Reflection;
using b1;
using b1.BGW;
using b1.ECS;
using b1.UI.Comm;
using B1UI.GSUI;
using BtlShare;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using LiteNetLib;
using PreludeLib.Attributes;
using ResB1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Global)]
public static class PatchStartGameUiCoop
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UIStartGame", "OnUIPageConstructImpl")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UIStartGame:OnUIPageConstructImpl");
    }

    public static void Postfix(GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName, DSStartGame ___DataStore)
    {
        for (var j = ___DataStore.BtnDataList.Count - 1; j >= 0; j--)
        {
            var BtnBase2 = ___DataStore.BtnDataList[j];

            Logging.LogDebug("Button name: {Name}, id: {Id}", BtnBase2.Name.Value, BtnBase2.Id.Value);
            var buttonName = BtnBase2.Name.Value.ToString();

            if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
            {
                var playerMarkerActorClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(Constants.PlayerMarkerPath, ELoadResourceType.SyncLoadAndCache);
                if (playerMarkerActorClass == null)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                    WukongApi.Widgets.ShowTip(BuiltinTexts.MissingPak, false);
                    Logging.LogError("WukongMP.pak is not loaded. Could not continue game.");
                }
                else if (!WukongApi.Sync.IsConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);

                    WukongApi.Sync.GetDisconnectReasonAndInvoke(reason =>
                    {
                        Utils.TryRunOnGameThread(() =>
                        {
                            WukongApi.Local.ShowInfoMessage(reason == DisconnectReason.ConnectionRejected ? BuiltinTexts.ConnectionRejectedByServer : BuiltinTexts.Disconnected);
                        });
                    });
                    Logging.LogError("Disconnected. Could not continue game.");
                }
                else
                {
                    Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                    ___StartGameBtnList[j].SetTxtName(GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
                }
            }
            else if (buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME).ToString()
                     && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.START_GAME_SETTING).ToString()
                     && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME_PLUS).ToString())
            {
                Logging.LogDebug("UI name desc to hide: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME));
                ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                ___StartGameBtnList.RemoveAt(j);
            }
        }

        __instance.GSAnimKeyToState("GSAKBContinueBtn", "CBtnFocus");

        ___TxtMainName.SetText(FText.FromString(""));
        ___TxtSubName.SetText(FText.FromString("Wukong Multiplayer Mod"));
        ___TxtSubName.SetRenderScale(new FVector2D(1.2, 1.2));
    }
}

/// <summary>
/// Hide challenges shrine options in coop mode.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchShrineRegisterFunc
{
    [HarmonyTargetMethodHint(typeof(FMenuHelper<EShrineMenuTag>), "RegisterFunc")]
    public static MethodBase TargetMethod()
    {
        var specializedType = typeof(FMenuHelper<EShrineMenuTag>);
        return specializedType.GetMethod("RegisterFunc")!;
    }

    public static bool Prefix(int FuncId)
    {
        if (!WukongApi.Sync.InArea)
            return true;

        var interactionFuncDesc = GameDBRuntime.GetInteractionFuncDesc(FuncId);
        return interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossIterations
               && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossRechallenge;
    }
}

[HarmonyPatch(typeof(BUI_BattleInfoCS), "InitBloodBarUI")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchInitBloodBarUI
{
    public static bool Prefix(BUI_BattleInfoCS __instance, Dictionary<Entity, BUI_ProjWidget> ___EntityDic, Dictionary<AActor, DSBarInfoBind> ___BloodBarActorBindDict, Entity Entity)
    {
        if (___EntityDic.ContainsKey(Entity))
            return false;
        var actor = Entity.ToActor();
        var ownerUnit = actor as BGUCharacterCS;
        if (ownerUnit == null)
            return false;
        var unitCommDesc = BGW_GameDB.GetUnitCommDesc(ownerUnit.GetResID());
        if (unitCommDesc == null)
            return false;
        var battleInfoExtendDesc = BGW_GameDB.GetUnitBattleInfoExtendDesc(ownerUnit.GetFinalBattleInfoExtendID());
        if (battleInfoExtendDesc == null)
            return false;

        var maybePlayer = WukongApi.Sync.GetPlayerEntityByActor(actor);
        var isPlayer = maybePlayer.HasValue;
        var bloodBarShowType = isPlayer ? EBGUBloodBarShowType.Always : EBGUBloodBarShowType.Change;

        var isInPlayerTeam = !isPlayer && BGU_DataUtil.GetIsInPlayerTeam(actor);

        if (battleInfoExtendDesc.BloodBarType == EBGUBloodBarType.None || isInPlayerTeam)
            return false;

        var bloodBarPoolWidget = __instance.GetTopBarPoolWidget(ownerUnit, true) as BUI_MBarBase;
        bloodBarPoolWidget?.InitBloodBar(battleInfoExtendDesc.BloodBarType, unitCommDesc.HPBarHeightOffset);

        if (bloodBarPoolWidget != null)
        {
            if (bloodBarShowType == EBGUBloodBarShowType.Always)
            {
                bloodBarPoolWidget.SetAlwaysShowSetting(AlwaysShowSetting.Always, true);
            }

            ___EntityDic.Add(Entity, bloodBarPoolWidget);
        }

        if (!___EntityDic.ContainsKey(Entity) || !___BloodBarActorBindDict.TryGetValue(actor, out var dsBarInfoBind))
            return false;

        dsBarInfoBind.ReInit();
        return false;
    }
}

[HarmonyPatch(typeof(BUS_UnitBarInfoComp), "ShowEnemyBar")]
[HarmonyPatchCategory(PatchCategory.Global)]
public class PatchShowEnemyBar
{
    public static bool Prefix(BUS_UnitBarInfoComp __instance, ref bool __result)
    {
        var owner = __instance.GetOwner();
        if (owner is BGUPlayerCharacterCS)
        {
            __result = !BGUFunctionLibraryCS.BGUHasUnitSimpleState(owner, EBGUSimpleState.CantShowBlood);
            return false;
        }

        return true;
    }
}