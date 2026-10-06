using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Mapping;
using WukongMp.Api;
using WukongMp.Coop.Gamemode;
using WukongMp.Coop.Mapping;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop;

[ModEntry]
public sealed partial class Mod(IDependencyContainer services, ILogger logger)
{
    private void Init()
    {
        // Launcher will set SERVER_ID when playing on hosted ReadyM servers
        if (WukongApi.Configuration.GetLaunchParameter("SERVER_ID", "") != "")
        {
            services.RegisterSingleton<IFileClient, HttpFileClient>();

            // takes over the SDK's WukongSelfHostedSaveApi
            services.RegisterSingleton<IWukongSaveApi, CloudWukongSaveApi>(replace: true);
        }

        // TODO: A more direct API for registering this
        services.RegisterSingleton<IShapeMappings, CoopMappings>();

        services.RegisterSingleton<ColliderDisableData>();
        services.RegisterSingleton<CoopSaveManager>();

        logger.LogInformation("Initializing co-op mod");

        WukongApi.Configuration.IsSupportMultiLockEnabled = true;
        WukongApi.Configuration.IsStrongDamageImmueEnabled = false;
        WukongApi.Configuration.EnableCustomCameraArmLength = false;
        WukongApi.Configuration.DeleteDestroyedTamersFromEcs = false;
        WukongApi.Configuration.SyncTamerTeamFromGameToEcs = true;

        logger.LogInformation("Initialized co-op mod");
    }
}