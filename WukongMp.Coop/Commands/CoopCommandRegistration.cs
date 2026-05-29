using b1;
using B1UI;
using ReadyM.Api.Command;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Commands;

public static class CoopCommandRegistrations
{
    public static void RegisterCommands(IWukongConsoleApi consoleApi)
    {
        consoleApi.AddCommand("cutscene", ConsoleCommand.Create(PlayCutscene, true));
        consoleApi.AddCommand("teleport", ConsoleCommand.Create(Teleport, true));
        consoleApi.AddCommand("openlevel", ConsoleCommand.Create(OpenLevel, true));
        consoleApi.AddCommand("bosshp", ConsoleCommand.Create(CustomScaling, false));
    }

    private static void PlayCutscene(int seqId)
    {
        GSG.GMSvc.GMTeleportToTargetSequence(seqId);
    }

    private static void Teleport(int birthPointId)
    {
        BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(
            ETeleportTypeV2.RebirthPointTeleportOnly,
            new TeleportParam_RebirthPoint { RebirthPointId = birthPointId },
            EPlayerTeleportReason.RebirthPoint);
    }

    private static void OpenLevel(string name)
    {
        UGameplayStatics.OpenLevel(GameUtils.GetWorld(), new FName(name));
    }

        private static void CustomScaling(int scale = 100)
    {        
        var owner = WukongApi.Sync.IsMasterClient;
        var areaPlayers = WukongApi.Sync.AreaPlayers.Count;
        
        if (!owner)
        {
            WukongApi.Chat.ShowLocalMessage("Only the host can change Boss HP scaling.", FLinearColor.OrangeRed);
            return;
        }

        if (scale <= 0)
        {
            WukongApi.Chat.ShowLocalMessage($"Boss HP scaling modifier {scale} is invalid.", FLinearColor.OrangeRed);
            return;
        }

        WukongApi.Chat.SendServerMessage($"Boss HP scaling changed!");
        WukongApi.Chat.SendServerMessage($"Boss HP is set to {scale}% and multiplied by {areaPlayers} Players.");
        WukongApi.Chat.SendServerMessage($"Boss HP is now {scale + scale * (areaPlayers - 1)}% of base HP.");
        Config.BossHPModifier = scale * 0.01f;
        Config.BossHPChanged = true;
    }
}