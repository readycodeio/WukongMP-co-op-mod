using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Https;
using WukongMp.Coop.Configuration;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using FileInfo = WukongMp.Api.Https.FileInfo;

namespace WukongMp.Coop.Gamemode;

public class HttpFileClient : IFileClient
{
    private class DownloadServerFileResponse
    {
        public string DownloadUrl { get; set; } = null!;
    }

    private readonly int serverId;
    private readonly string apiBaseUrl;
    private readonly string jwtToken;
    private readonly ILogger logger;

    public HttpFileClient(ILogger logger)
    {
        this.logger = logger;

        var serverIdParam = WukongApi.Configuration.GetLaunchParameter("SERVER_ID", "");
        if (!int.TryParse(serverIdParam, out serverId))
        {
            logger.LogError("Invalid or missing SERVER_ID launch parameter");
        }

        apiBaseUrl = WukongApi.Configuration.GetLaunchParameter("API_BASE_URL", "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            logger.LogError("Invalid or missing API_BASE_URL launch parameter");
        }

        jwtToken = WukongApi.Configuration.GetLaunchParameter("JWT_TOKEN", "");
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            logger.LogError("Invalid or missing JWT_TOKEN launch parameter");
        }
    }


    public async Task<bool> UploadFileAsync(FileInfo file, CancellationToken ct = default)
    {
        var client = new BouncyCastleHttpsClient(logger);
        var kind = file.Name == Constants.CoopWorldArchiveName ? SaveFileType.WorldSave : SaveFileType.PlayerSave;

        Guid? userGuid = null;
        if (kind == SaveFileType.PlayerSave)
        {
            // name is like "player_<userGuid>.sav"
            var parts = file.Name.Split('_', '.');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var parsedGuid))
            {
                userGuid = parsedGuid;
            }
        }

        var query = $"?kind={kind}&userGuid={userGuid}&serverId={serverId}";
        var url = new Uri($"{apiBaseUrl}/api/server/{serverId}/files/upload-sas{query}");
        var uploadUrl = await client.GetAsync<string>(url, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {jwtToken}" }
        }, ct);

        if (uploadUrl is not null)
        {
            // compress the blob with GZIP
            using var stream = new MemoryStream();
            using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, true))
            {
                await gzip.WriteAsync(file.Content, 0, file.Content.Length, ct);
            }

            var gzippedContent = stream.ToArray();
            var md5Checksum = MD5.Create().ComputeHash(gzippedContent);

            // this is a SAS URL for Azure Blob Storage
            var uploadUri = new Uri(uploadUrl);

            // https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob?tabs=microsoft-entra-id#request-headers-all-blob-types
            var headers = new Dictionary<string, string>
            {
                { "x-ms-blob-type", "BlockBlob" },
                { "x-ms-version", "2025-07-05" },
                { "x-ms-blob-content-encoding", "gzip" },
                { "Content-MD5", Convert.ToBase64String(md5Checksum) }
            };
            var status = await client.PutBytesAsync(uploadUri, headers, gzippedContent, ct);
            return status is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;
        }

        logger.LogError("Failed to get upload URL for blob '{BlobName}' for server {ServerId}", file.Name, serverId);
        return false;
    }

    public async Task<FileInfo?> DownloadFileAsync(string name, CancellationToken ct = default)
    {
        var nameEscaped = Uri.EscapeDataString(name);

        var client = new BouncyCastleHttpsClient(logger);

        // Download
        var linkUrl = new Uri($"{apiBaseUrl}/api/server/{serverId}/files/{nameEscaped}");
        var downloadResponse = await client.GetAsync<DownloadServerFileResponse>(linkUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {jwtToken}" }
        }, ct);

        if (string.IsNullOrWhiteSpace(downloadResponse?.DownloadUrl))
        {
            logger.LogWarning("Failed to get download URL for blob '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        var downloadUrl = new Uri(downloadResponse!.DownloadUrl);
        var response = await client.GetBytesAsync(downloadUrl, ct: ct);

        if (response == null)
        {
            logger.LogError("Failed to download blob content '{BlobName}' for server {ServerId}", name, serverId);
            return null;
        }

        return new FileInfo(name, response);
    }
}