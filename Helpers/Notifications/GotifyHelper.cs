namespace WeddingShare.Helpers.Notifications
{
    public class GotifyHelper : INotificationHelper
    {
        private readonly ISettingsHelper _settings;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger _logger;

        public GotifyHelper(ISettingsHelper settings, IHttpClientFactory clientFactory, ILogger<GotifyHelper> logger)
        {
            _settings = settings;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            if (string.IsNullOrWhiteSpace(await _settings.GetOrDefault(Constants.Notifications.Gotify.Endpoint, string.Empty)))
            {
                _logger.LogWarning($"Invalid Gotify endpoint specified");
                return false;
            }

            if (string.IsNullOrWhiteSpace(await _settings.GetOrDefault(Constants.Notifications.Gotify.Token, string.Empty)))
            {
                _logger.LogWarning($"Invalid Gotify token specified");
                return false;
            }

            if (await _settings.GetOrDefault(Constants.Notifications.Gotify.Enabled, false))
            { 
                try
                {
                    var token = await _settings.GetOrDefault(Constants.Notifications.Gotify.Token, string.Empty);
                    if (!string.IsNullOrWhiteSpace(token))
                    { 
                        var priority = await _settings.GetOrDefault(Constants.Notifications.Gotify.Priority, 4);
                        if (priority > 0)
                        {
                            message = !string.IsNullOrWhiteSpace(actionLink) ? $"{message} - Visit - {actionLink}" : message;

                            var client = _clientFactory.CreateClient("GotifyClient");
                            using (var response = await client.PostAsJsonAsync($"/message?token={token}", new { title, message, priority }))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    return true;
                                }
                                else
                                {
                                    var error = await response.Content.ReadAsStringAsync();
                                    _logger.LogError($"Failed to send Gotify message with title '{title}' - {error}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid Gotify priority specified");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid Gotify token specified");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send Gotify message with title '{title}' - {ex?.Message}");
                }
            }

            return false;
        }
    }
}