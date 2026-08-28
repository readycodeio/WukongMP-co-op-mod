using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Coop.Common;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk.Api;
using FileInfo = ReadyM.Api.Saves.FileInfo;

namespace WukongMp.Coop.Gamemode;

public sealed class CloudWukongSaveApi : IWukongSaveApi
{
    private readonly IFileClient fileClient;
    private readonly ILogger logger;

    private string PlayerSaveName { get; }

    public CloudWukongSaveApi(IFileClient fileClient, IWukongConfigurationApi configuration, ILogger logger)
    {
        this.fileClient = fileClient;
        this.logger = logger;

        var userGuid = configuration.GetLaunchParameter("PLAYER_ID", "");
        if (string.IsNullOrEmpty(userGuid) || !Guid.TryParse(userGuid, out var guid))
        {
            logger.LogError("PLAYER_ID launch parameter is not set. Player saves will not be uniquely identified.");
        }

        PlayerSaveName = $"player_{guid:N}.sav";
    }

    public Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default)
        => UploadBlobAsync(Constants.CoopWorldArchiveName, content, ct);

    public Task<FileInfo?> DownloadWorldSaveAsync(CancellationToken ct = default)
        => DownloadBlobAsync(Constants.CoopWorldArchiveName, ct);

    public Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default)
        => UploadBlobAsync(PlayerSaveName, content, ct);

    public Task<FileInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default)
        => DownloadBlobAsync(PlayerSaveName, ct);

    private Task<bool> UploadBlobAsync(string name, byte[] content, CancellationToken ct = default)
    {
        try
        {
            return fileClient.UploadFileAsync(new FileInfo(name, content), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload blob: {BlobName}", name);
            throw new OperationCanceledException("Failed to upload blob", ex);
        }
    }

    private Task<FileInfo?> DownloadBlobAsync(string name, CancellationToken ct = default)
    {
        try
        {
            return fileClient.DownloadFileAsync(name, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download blob: {BlobName}", name);
            throw new OperationCanceledException("Failed to download blob", ex);
        }
    }
}