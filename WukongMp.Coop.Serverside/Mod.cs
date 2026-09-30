using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;

namespace WukongMp.Coop.Serverside;

[ModEntry]
public sealed partial class Mod(IDependencyContainer services, ILogger logger)
{
    private void Start()
    {
        services.RegisterSingleton<RpcHandlers>();
        logger.LogInformation("Serverside SDK mod initialized");
    }
}