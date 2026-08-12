using CSharpModBase.Input;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using WukongMp.Api;
using WukongMp.Coop.Commands;
using WukongMp.Coop.Configuration;
using WukongMp.Coop.Gamemode;
using WukongMp.Coop.UI;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Mod : ModBase
{
    public override string Name => "WukongMp.Coop";

    protected override void Initialize(IDependencyContainer services)
    {
        // Launcher will set SERVER_ID when playing on hosted ReadyM servers
        if (WukongApi.Configuration.GetLaunchParameter("SERVER_ID", "") != "")
        {
            services.RegisterSingleton<IFileClient, HttpFileClient>();
            // takes over the SDK's WukongSelfHostedSaveApi
            services.RegisterSingleton<IWukongSaveApi, CloudWukongSaveApi>(replace: true);
        }

        services.RegisterSingleton<ColliderDisableData>();
        services.RegisterSingleton<CoopSaveManager>();
        services.RegisterSingleton<CoopWidgetManager>();
        services.RegisterSingleton<CoopEventCallbacks>();
        services.RegisterSingleton<CoopServerRpc>();

        Logger.LogInformation("Initializing {ModName}", Name);

        CoopCommandRegistrations.RegisterCommands(WukongApi.Console);

        WukongApi.Configuration.IsSupportMultiLockEnabled = true;
        WukongApi.Configuration.IsStrongDamageImmueEnabled = false;
        WukongApi.Configuration.EnableCustomCameraArmLength = false;
        WukongApi.Configuration.DeleteDestroyedTamersFromEcs = false;
        WukongApi.Configuration.SyncTamerTeamFromGameToEcs = true;

        Logger.LogInformation("Initialized {PluginName}", Name);
    }

    public override void LateInit()
    {
        base.LateInit();

        WukongApi.Input.RegisterKeyBind(Key.F6, () =>
        {
            Logging.LogDebug("F6: Toggle HP scaling");
            Config.ScaleMonsterHpToHalf = !Config.ScaleMonsterHpToHalf;
        });
    }
}