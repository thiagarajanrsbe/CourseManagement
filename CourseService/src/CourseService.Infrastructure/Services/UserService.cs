using Microsoft.Extensions.Logging;
using Serilog;
using CourseService.Application.Interfaces;
using Polly;

namespace CourseService.Infrastructure.Services;

/// <summary>
/// User service implementation that calls UserService via HTTP.
/// </summary>
public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    private readonly ILogger<UserService> _logger;

    public UserService(HttpClient httpClient, ILogger<UserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retrying UserService request. Attempt: {RetryCount}",
                        retryCount);
                });        

        _policy = retryPolicy;
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _policy.ExecuteAsync(async () =>
                await _httpClient.GetAsync($"users/{userId}", cancellationToken));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists: {UserId}", userId);
            return false;
        }
    }

    public async Task<string?> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _policy.ExecuteAsync(async () =>
                await _httpClient.GetAsync($"users/{userId}", cancellationToken));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user name for userId: {UserId}", userId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            // In real scenario, deserialize JSON and extract username
            _logger.LogInformation("Retrieved user name for userId: {UserId}", userId);
            return null; // Simplified for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user name: {UserId}", userId);
            return null;
        }
    }
}
