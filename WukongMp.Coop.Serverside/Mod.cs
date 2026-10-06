using Microsoft.Extensions.Logging;
using ReadyM.SDK.Attributes;

namespace WukongMp.Coop.Serverside;

[ModEntry]
public sealed partial class Mod(ILogger logger)
{
    private void Init()
    {
        logger.LogInformation("Serverside SDK mod initialized");
    }
}