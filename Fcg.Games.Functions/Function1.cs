using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Fcg.Games.Functions;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly IHttpClientFactory _httpFactory;

    private const string ExternalApi = "https://fcggamesapi-g2dcb2fafjftgzfy.chilecentral-01.azurewebsites.net/api/games/random";

    public Function1(ILogger<Function1> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    // HTTP GET endpoint available locally at: http://localhost:7071/api/games/random
    [Function("GetRandomGame")]
    public async Task<HttpResponseData> GetRandom([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/random")] HttpRequestData req)
    {
        var client = _httpFactory.CreateClient();

        try
        {
            var externalResp = await client.GetAsync(ExternalApi);

            var response = req.CreateResponse(externalResp.StatusCode);

            if (externalResp.Content.Headers.ContentType?.ToString() is string ct)
            {
                response.Headers.Add("Content-Type", ct);
            }

            await externalResp.Content.CopyToAsync(response.Body);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling external API");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Error calling external API");
            return error;
        }
    }

    // Timer trigger that runs every 1 minute
    [Function("TimerEveryMinute")]
    public async Task RunTimer([TimerTrigger("0 */1 * * * *")] object timer)
    {
        _logger.LogInformation("Timer triggered at: {time}", DateTimeOffset.UtcNow);

        var client = _httpFactory.CreateClient();
        try
        {
            var externalResp = await client.GetAsync(ExternalApi);
            _logger.LogInformation("External API responded with {status}", externalResp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timer error calling external API");
        }
    }
}
