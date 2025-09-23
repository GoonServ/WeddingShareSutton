namespace WeddingShare.Helpers.Notifications
{
    public class NotificationBroker : INotificationHelper
    {
        private readonly ISettingsHelper _settings;
        private readonly ISmtpClientWrapper _smtp;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILoggerFactory _logger;

        public NotificationBroker(ISettingsHelper settings, ISmtpClientWrapper smtp, IHttpClientFactory clientFactory, ILoggerFactory logger)
        {
            _settings = settings;
            _smtp = smtp;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<bool> Send(string title, string message, string? actionLink = null)
        {
            var emailSent = true;
            var ntfySent = true;
            var gotifySent = true;

            if (await _settings.GetOrDefault(Constants.Notifications.Smtp.Enabled, false))
            {
                emailSent = await new EmailHelper(_settings, _smtp, _logger.CreateLogger<EmailHelper>()).Send(title, message, actionLink);
            }

            if (await _settings.GetOrDefault(Constants.Notifications.Ntfy.Enabled, false))
            {
                ntfySent = await new NtfyHelper(_settings, _clientFactory, _logger.CreateLogger<NtfyHelper>()).Send(title, message, actionLink);
            }

            if (await _settings.GetOrDefault(Constants.Notifications.Gotify.Enabled, false))
            {
                gotifySent = await new GotifyHelper(_settings, _clientFactory, _logger.CreateLogger<GotifyHelper>()).Send(title, message, actionLink);
            }

            return emailSent && ntfySent && gotifySent;
        }
    }
}