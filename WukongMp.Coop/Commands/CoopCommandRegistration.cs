using b1;
using B1UI;
using ReadyM.Api.Command;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;
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
        if (scale <= 0)
        {
            WukongApi.Chat.ShowLocalMessage("Boss HP scaling must be > 0%", FLinearColor.OrangeRed);
            return;
        }

        var rpc = WukongApi.Services.Resolve<CoopServerRpc>();
        rpc.SendScaleBossHp(scale);
    }
}