using Microsoft.Extensions.Options;
using NatureRemoEInfluxDbExporter.Extensions;
using NatureRemoEInfluxDbExporter.Options;
using ZLogger;

namespace NatureRemoEInfluxDbExporter;

public class NatureRemoEClient(
    ILogger<NatureRemoEClient> logger,
    IOptions<NatureRemoOption> option,
    IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();

    private readonly NatureRemoOption _option = option.Value;

    private const string BaseUri = "https://api.nature.global/1";

    /// <summary>
    /// デバイス状態取得
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<(bool IsSuccess, string Json, Exception? Error)> GetAppliancesAsync(CancellationToken cancellationToken = default)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseUri}/appliances");

        requestMessage.Headers.Add("Authorization", $"Bearer {_option.AccessToken}");

        try
        {
            var response = await _client.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (true, (await response.Content.ReadAsStringAsync(cancellationToken)).JsonFormatting(), null);
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error occurred while getting device status.");
            return (false, string.Empty, e);
        }
    }

}
